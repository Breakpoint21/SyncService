using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kyrodan.HiDrive;
using Kyrodan.HiDrive.Models;
using Kyrodan.HiDrive.Requests;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SyncService.Services.Sync
{
    public class HiDriveSyncExecutionInfo
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string LogPath { get; set; }
        public bool SyncContentOnly { get; set; }
        public LogEventLevel LogEventLevel { get; set; } = LogEventLevel.Verbose;
    }

    public class RemoteItem
    {
        public RemoteItem(DirectoryChildItem directoryChild)
        {
            Id = directoryChild?.Id ?? throw new ArgumentNullException(nameof(DirectoryChildItem.Id));
            Name = directoryChild?.Name ?? throw new ArgumentNullException(nameof(DirectoryChildItem.Name));
            Type = directoryChild?.Type ?? throw new ArgumentNullException(nameof(DirectoryChildItem.Type));
            MetaHash = directoryChild?.MetaHash ?? throw new ArgumentNullException(nameof(DirectoryChildItem.MetaHash));
        }

        public string Id { get; }
        public string Name { get; }
        public string Type { get; }
        public string MetaHash { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum SyncState
    {
        None,
        Successful,
        Cancelled,
        Failed
    }

    public enum SyncAction
    {
        None,
        Added,
        Updated,
        Deleted
    }

    public class SyncResult
    {
        public SyncResult(HiDriveSyncExecutionInfo syncExecutionInfo)
        {
            TaskName = syncExecutionInfo.Name;
        }

        public string TaskName { get; set; }
        public Exception Exception { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public SyncState State { get; set; }
        
        public List<ItemSyncResult> Items { get; set; }
    }

    public class ItemSyncResult
    {
        public ItemSyncResult(string name, SyncAction action, SyncState state)
        {
            Name = name;
            Action = action;
            State = state;
        }

        public ItemSyncResult(string name, SyncAction action, SyncState state, Exception exception)
        {
            Name = name;
            Action = action;
            State = state;
            Exception = exception;
        }

        public string Name { get; set; }
        public Exception Exception { get; set; }
        public SyncState State { get; set; }
        public SyncAction Action { get; set; }
    }

    public class HiDriveSyncExecutor
    {
        private readonly IHiDriveClient _hiDriveClient;
        private Logger _logger;
        private static readonly HiDriveSyncHash HiDriveSyncHash = HiDriveSyncHash.Create();

        public HiDriveSyncExecutor(IHiDriveClient hiDriveClient)
        {
            _hiDriveClient = hiDriveClient;
        }

        public async Task<SyncResult> Sync(HiDriveSyncExecutionInfo syncExecutionInfo, CancellationToken token)
        {
            var result = new SyncResult(syncExecutionInfo);
            Directory.CreateDirectory(syncExecutionInfo.LogPath);
            using (_logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.File(Path.Combine(syncExecutionInfo.LogPath, $"{syncExecutionInfo.Name}_Log_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt"), syncExecutionInfo.LogEventLevel).WriteTo.Debug().CreateLogger())
            {
                _logger.Information("Starting Sync of '{LocalDirectory}' to '{RemotePath}'", syncExecutionInfo.SourcePath, syncExecutionInfo.DestinationPath);
                result.Start = DateTime.Now;
                try
                {
                    var destination = await CreateDirectoryPath(syncExecutionInfo.DestinationPath, token);
                    var source = new DirectoryInfo(syncExecutionInfo.SourcePath);
                    result.Items = await SyncDirectory(destination, source, token);
                    result.State = SyncState.Successful;
                }
                catch (OperationCanceledException operationCanceledException)
                {
                    _logger.Warning(operationCanceledException, "Sync Process was cancelled!");
                    result.Items = new List<ItemSyncResult>();
                    result.State = SyncState.Cancelled;
                }
                catch (Exception exception)
                {
                    _logger.Fatal(exception, "");
                    result.Items = new List<ItemSyncResult>();
                    result.State = SyncState.Failed;
                    result.Exception = exception;
                }
                finally
                {
                    result.End = DateTime.Now;
                }
            }

            return result;
        }

        private async Task<List<ItemSyncResult>> SyncDirectory(DirectoryItem destination, DirectoryInfo source, CancellationToken token)
        {
            var syncResults = new List<ItemSyncResult>();
            var contentHash = await FetchContentHash(destination.Path, token);
            if (contentHash != null)
            {
                var localContentHash = HiDriveSyncHash.CalculateContentHash(source, info => false).ToHashString();
                if (string.Equals(contentHash, localContentHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("Local Directory '{source}' is in sync with '{destination}'", source,
                        destination);
                    return syncResults;
                }
            }

            var remoteItems = await FetchRemoteItems(destination, token);

            //var tasks = new List<Task<ItemSyncResult>>(8);

            foreach (var fileSystemInfo in source.EnumerateFileSystemInfos()
                .TakeWhile(info => !token.IsCancellationRequested))
            {
                if (fileSystemInfo is FileInfo fileInfo)
                {
                    //if (tasks.Count >= 8)
                    //{
                    //    var task = await Task.WhenAny(tasks);
                    //    tasks.Remove(task);
                    //    syncResults.Add(await task);
                    //}

                    //tasks.Add(SyncFile(destination, fileInfo, remoteItems, token));
                    var syncResult = await SyncFile(destination, fileInfo, remoteItems, token);
                    syncResults.Add(syncResult);
                }
                else if (fileSystemInfo is DirectoryInfo directoryInfo)
                {
                    var itemSyncResults = await SyncSubDirectory(destination, directoryInfo, remoteItems, token);
                    syncResults.AddRange(itemSyncResults);
                }
            }

            //syncResults.AddRange(await Task.WhenAll(tasks));

            if (remoteItems.Count > 0)
            {
                var itemSyncResults = await DeleteRemoteItems(source, remoteItems, token);
                syncResults.AddRange(itemSyncResults);
            }

            _logger.Information("Local Directory '{source}' was synced with '{destination}'", source, destination);

            return syncResults;
        }

        private async Task<List<ItemSyncResult>> DeleteRemoteItems(DirectoryInfo source, Dictionary<string, RemoteItem> remoteItems, CancellationToken token = default(CancellationToken))
        {
            var syncResults = new List<ItemSyncResult>();
            foreach (var entry in remoteItems)
            {
                try
                {
                    _logger.Information("Deleting Remote Item '{@RemoteItem}'", entry.Value);
                    if (entry.Value.Type.Equals("file", StringComparison.OrdinalIgnoreCase))
                    {
                        await _hiDriveClient.File.Delete(pid: entry.Value.Id, dirModificationTime: source.LastWriteTimeUtc)
                            .ExecuteAsync(token);
                        var syncResult = new ItemSyncResult(entry.Value.Name, SyncAction.Deleted, SyncState.Successful);
                        syncResults.Add(syncResult);
                    }
                    else if (entry.Value.Type.Equals("dir", StringComparison.OrdinalIgnoreCase))
                    {
                        await _hiDriveClient.Directory.Delete(pid: entry.Value.Id, isRecursive: true, dirModificationTime: source.LastWriteTimeUtc)
                            .ExecuteAsync(token);
                        var syncResult = new ItemSyncResult(entry.Value.Name, SyncAction.Deleted, SyncState.Successful);
                        syncResults.Add(syncResult);
                    }
                    else
                    {
                        _logger.Warning("Unable to delete Remote Item {@RemoteItem}", entry.Value);
                        var syncResult = new ItemSyncResult(entry.Value.Name, SyncAction.Deleted, SyncState.Failed);
                        syncResults.Add(syncResult);
                    }
                }
                catch (ServiceException e)
                {
                    _logger.Warning(e, "");
                    var syncResult = new ItemSyncResult(entry.Value.Name, SyncAction.Deleted, SyncState.Failed, e);
                    syncResults.Add(syncResult);
                }
            }

            return syncResults;
        }

        private async Task<List<ItemSyncResult>> SyncSubDirectory(DirectoryItem remoteParentDirectory, DirectoryInfo directoryInfo,
            Dictionary<string, RemoteItem> remoteItems, CancellationToken token)
        {
            var syncResults = new List<ItemSyncResult>();
            var remoteDirectoryPath = CombinePaths(remoteParentDirectory.Path, directoryInfo.Name);
            var remoteMetadata = await FetchDirectoryMetadata(remoteDirectoryPath, token);
            if (remoteMetadata == null)
            {
                _logger.Information("Directory '{Directory}' does not exist in remote Directory {Parent}", directoryInfo, remoteDirectoryPath);
                var directoryItem = await CreateRemoteDirectory(remoteParentDirectory, directoryInfo, token);
                syncResults.Add(new ItemSyncResult(directoryInfo.FullName, SyncAction.Added, SyncState.Successful));
                var itemSyncResults = await SyncDirectory(directoryItem, directoryInfo, token);
                syncResults.AddRange(itemSyncResults);
            }
            else
            {
                var directoryNameHash = HiDriveSyncHash.CalculateNameHash(directoryInfo.Name).ToHashString();
                if(!remoteItems.Remove(directoryNameHash))
                    throw new KeyNotFoundException($"Name Hash for Directory '{directoryInfo.Name}' was not found!");
                var directoryItem = await FetchRemoteDirectoryItem(remoteDirectoryPath, token);

                var localContentHash = HiDriveSyncHash.CalculateContentHash(directoryInfo, info => false).ToHashString();
                if (remoteMetadata.ContentHash != localContentHash)
                {
                    _logger.Information("Directory Content '{Directory}' is out of sync!", directoryInfo);
                    _logger.Verbose("Remotehash: {remote} Localhash: {local}", remoteMetadata.ContentHash, localContentHash);

                    var itemSyncResults = await SyncDirectory(directoryItem, directoryInfo, token);
                    var itemSyncResult = await UpdateDirectoryMetadata(directoryItem, directoryInfo, token);
                    syncResults.AddRange(itemSyncResults);
                    syncResults.Add(itemSyncResult);
                }
                else if (remoteMetadata.MetadataHash != HiDriveSyncHash.CalculateMetadataHash(directoryInfo).ToHashString())
                {
                    _logger.Information("Directory Metadata '{Directory}' is out of sync!", directoryInfo);
                    var syncResult = await UpdateDirectoryMetadata(directoryItem, directoryInfo, token);
                    syncResults.Add(syncResult);
                }
                else
                {
                    _logger.Information("Directory '{Directory}' is in sync!", directoryInfo);
                    syncResults.Add(new ItemSyncResult(directoryInfo.FullName, SyncAction.None, SyncState.Successful));
                }
            }

            return syncResults;
        }

        private async Task<DirectoryItem> FetchRemoteDirectoryItem(string remoteDirectoryPath, CancellationToken token)
        {
            try
            {
                return await _hiDriveClient.Directory.Get(remoteDirectoryPath,
                        members: new[] {DirectoryMember.Directory},
                        fields: new[]
                            {DirectoryBaseItem.Fields.Id, DirectoryBaseItem.Fields.Name, DirectoryBaseItem.Fields.Path})
                    .ExecuteAsync(token);
            }
            catch (ServiceException serviceException)
            {
                _logger.Warning("", serviceException);
                throw;
            }
        }

        private async Task<DirectoryItem> CreateRemoteDirectory(DirectoryItem remoteDirectory, DirectoryInfo directoryInfo, CancellationToken token = default(CancellationToken))
        {
            try
            {
                _logger.Verbose("Creating Sub directory in {remoteDirectory} for local Directory {localDirectory}", remoteDirectory, directoryInfo);
                return await _hiDriveClient.Directory.Create(directoryInfo.Name, remoteDirectory.Id,
                    directoryInfo.LastWriteTimeUtc, directoryInfo.Parent.LastWriteTimeUtc).ExecuteAsync(token);
            }
            catch (ServiceException e)
            {
                _logger.Warning(e, "Creating Remote Directory failed!");
                throw;
            }
        }

        private async Task<ItemSyncResult> UpdateDirectoryMetadata(DirectoryItem remoteDirectoryItem, DirectoryInfo directoryInfo, CancellationToken token = default(CancellationToken))
        {
            try
            {
                _logger.Verbose("Updating Metadata for Directory {RemoteDirectory} with local Metadata", remoteDirectoryItem);
                await _hiDriveClient.Meta
                    .SetModificationDate(pid: remoteDirectoryItem.Id,
                        modificationTime: directoryInfo.LastWriteTimeUtc).ExecuteAsync(token);
                return new ItemSyncResult(directoryInfo.FullName, SyncAction.Updated, SyncState.Successful);
            }
            catch (ServiceException e)
            {
                _logger.Warning("", e);
                return new ItemSyncResult(directoryInfo.FullName, SyncAction.Updated, SyncState.Failed, e);
            }
        }

        private async Task<ItemSyncResult> UpdateFileMetadata(RemoteItem remoteItem, FileInfo file, CancellationToken token = default(CancellationToken))
        {
            try
            {
                _logger.Verbose("Updating Metadata for File {RemoteFile} with local Metadata", remoteItem);
                await _hiDriveClient.Meta
                    .SetModificationDate(pid: remoteItem.Id,
                        modificationTime: file.LastWriteTimeUtc).ExecuteAsync(token);
                return new ItemSyncResult(file.FullName, SyncAction.Updated, SyncState.Successful);
            }
            catch (ServiceException e)
            {
                _logger.Warning("", e);
                return new ItemSyncResult(file.FullName, SyncAction.Updated, SyncState.Failed, e);
            }
        }

        private async Task<ItemSyncResult> SyncFile(DirectoryItem destination, FileInfo fileInfo,
            Dictionary<string, RemoteItem> remoteItems, CancellationToken token)
        {
            var remoteContentHash = await FetchContentHash(CombinePaths(destination.Path, fileInfo.Name), token);
            using (var file = fileInfo.OpenRead())
            {
                if (remoteContentHash == null)
                {
                    _logger.Information("File {file} does not exist on remote", fileInfo);
                    return await UploadFile(destination, fileInfo, file, token);
                }
                var fileNameHash = HiDriveSyncHash.CalculateNameHash(fileInfo.Name).ToHashString();
                var localContentHash = HiDriveSyncHash.CalculateContentHash(file).ToHashString();
                if (remoteContentHash != localContentHash)
                {
                    _logger.Information("File {file} is out of sync with remote", fileInfo);
                    _logger.Verbose("Remotehash: {remote} Localhash: {local}", remoteContentHash, localContentHash);
                    if (!remoteItems.Remove(fileNameHash))
                        throw new KeyNotFoundException($"Filename Hash for '{fileInfo.Name}' was not found!");
                    return await UpdateFile(destination, fileInfo, file, token);
                }

                if (remoteItems.TryGetValue(fileNameHash, out var remoteItem))
                {
                    var metadataHash = HiDriveSyncHash.CalculateMetadataHash(fileInfo);
                    if (remoteItem.MetaHash != metadataHash.ToHashString())
                    {
                        _logger.Information("File Metadata of {file} is out of sync with remote", fileInfo);
                        _logger.Verbose("Remotehash: {remote} Localhash: {local}", remoteItem.MetaHash, metadataHash.ToHashString());
                        remoteItems.Remove(fileNameHash);
                        return await UpdateFileMetadata(remoteItem, fileInfo, token);
                    }

                    _logger.Debug("File {file} is in sync", fileInfo);
                    remoteItems.Remove(fileNameHash);
                    return new ItemSyncResult(fileInfo.FullName, SyncAction.None, SyncState.Successful);
                }
                throw new KeyNotFoundException($"Filename Hash for '{fileInfo.Name}' was not found!");
            }
        }

        private async Task<ItemSyncResult> UpdateFile(DirectoryItem destination, FileInfo fileInfo, FileStream file, CancellationToken token = default(CancellationToken))
        {
            try
            {
                _logger.Verbose("Replacing File {file} on remote with local", fileInfo);
                if (fileInfo.Length < 2147483647)
                {
                    await _hiDriveClient.File
                        .Upload(fileInfo.Name, dir_id: destination.Id, modificationTime: fileInfo.LastWriteTimeUtc,
                            dirModificationTime: fileInfo.Directory.LastWriteTimeUtc, mode: UploadMode.CreateOrUpdate)
                        .ExecuteAsync(file, token);
                }
                else
                {
                    var path = CombinePaths(destination.Path, fileInfo.Name);
                    var batchSize = 0x10000000;
                    var i = (int)Math.Ceiling(fileInfo.Length / (double)batchSize);
                    for (long j = 0; j < i; j++)
                    {
                        var stream = new BatchStream(file, j * batchSize, batchSize);
                        await _hiDriveClient.File
                            .Patch(path, modificationTime: fileInfo.LastWriteTimeUtc,
                                offset: j * batchSize)
                            .ExecuteAsync(stream, token);
                    }
                }
                
                return new ItemSyncResult(fileInfo.FullName, SyncAction.Updated, SyncState.Successful);
            }
            catch (ServiceException e)
            {
                _logger.Warning("Replacing File failed!", e);
                return new ItemSyncResult(fileInfo.FullName, SyncAction.Updated, SyncState.Failed, e);
            }
        }

        private async Task<ItemSyncResult> UploadFile(DirectoryItem destination, FileInfo fileInfo, FileStream file, CancellationToken token = default(CancellationToken))
        {
            try
            {
                _logger.Verbose("Uploading File {file} to remote", fileInfo);
                if (fileInfo.Length < 2147483647)
                {
                    await _hiDriveClient.File
                        .Upload(fileInfo.Name, dir_id: destination.Id, modificationTime: fileInfo.LastWriteTimeUtc,
                            dirModificationTime: fileInfo.Directory.LastWriteTimeUtc)
                        .ExecuteAsync(file, token);
                }
                else
                {
                    //Create empty file, and upload in parts
                    var fileItem = await _hiDriveClient.File
                        .Upload(fileInfo.Name, dir_id: destination.Id, modificationTime: fileInfo.LastWriteTimeUtc,
                            dirModificationTime: fileInfo.Directory.LastWriteTimeUtc)
                        .ExecuteAsync(Stream.Null, token);
                    var batchSize = 0x10000000;
                    var i = (int) Math.Ceiling(fileInfo.Length / (double) batchSize);
                    for (long j = 0; j < i; j++)
                    {
                        var stream = new BatchStream(file, j*batchSize, batchSize);
                        await _hiDriveClient.File
                            .Patch(pid:fileItem.Id, modificationTime: fileInfo.LastWriteTimeUtc,
                                offset: j * batchSize)
                            .ExecuteAsync(stream, token);
                    }
                }
                
                return new ItemSyncResult(fileInfo.FullName, SyncAction.Added, SyncState.Successful);
            }
            catch (ServiceException e)
            {
                _logger.Warning("Uploading File failed!", e);
                return new ItemSyncResult(fileInfo.FullName, SyncAction.Added, SyncState.Failed, e);
            }
            catch (OperationCanceledException e)
            {
                _logger.Warning("Uploading File timed out!");
                return new ItemSyncResult(fileInfo.FullName, SyncAction.Added, SyncState.Failed, e);
            }
        }

        public class BatchStream : Stream
        {
            private readonly Stream _baseStream;
            private readonly long _start;
            private readonly long _length;

            public BatchStream(Stream baseStream, long start, long length)
            {
                _baseStream = baseStream;
                _start = start;
                _length = Math.Min(_baseStream.Length - _baseStream.Position, length);
                _baseStream.Position = start;
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_baseStream.Position + count > _start + _length)
                {
                    return _baseStream.Read(buffer, offset, (int) ((_length + _start) - _baseStream.Position));
                }
                return _baseStream.Read(buffer, offset , count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset + _start, origin) - _start;
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _baseStream.Write(buffer, offset , count);
            }

            public override bool CanRead => _baseStream.CanRead;
            public override bool CanSeek => _baseStream.CanSeek;
            public override bool CanWrite => _baseStream.CanWrite;
            public override long Length => _length;
            public override long Position
            {
                get => _baseStream.Position - _start;
                set => _baseStream.Position = _start + value;
            }
        }

        private async Task<Dictionary<string, RemoteItem>> FetchRemoteItems(DirectoryItem directory, CancellationToken token = default(CancellationToken))
        {
            try
            {
                var directoryItem = await _hiDriveClient.Directory.Get(pid: directory.Id,
                        members: new[] {DirectoryMember.File, DirectoryMember.Directory},
                        fields: new[]
                        {
                            DirectoryBaseItem.Fields.Members, DirectoryBaseItem.Fields.Members_NameHash,
                            DirectoryBaseItem.Fields.Members_Id, DirectoryBaseItem.Fields.Members_Type, DirectoryBaseItem.Fields.Members_MetaHash
                        })
                    .ExecuteAsync(token);

                //new ConcurrentDictionary<string, RemoteItem>()
                return directoryItem.Members.ToDictionary(childItem => childItem.NameHash, childItem => new RemoteItem(childItem));
            }
            catch (ServiceException e) when (e.Error.Code == "404")
            {
                _logger.Verbose("Remote Item with id {Id} does not exist!", directory.Id);
                throw;
            }
        }

        private async Task<string> FetchContentHash(string path, CancellationToken token = default(CancellationToken))
        {
            try
            {
                _logger.Verbose("Fetching remote content Hash for path {path}", path);
                var meta = await _hiDriveClient.Meta.Get(path,
                    fields: new[]
                    {
                        Meta.Fields.ContentHash
                    }).ExecuteAsync(token);
                return meta.ContentHash;
            }
            catch (ServiceException e) when (e.Error.Code == "404")
            {
                _logger.Verbose("Remote path {path} does not exist!", path);
                return null;
            }
        }

        private async Task<Meta> FetchDirectoryMetadata(string path, CancellationToken token = default(CancellationToken))
        {
            try
            {
                _logger.Verbose("Fetching remote Metadata for path {path}", path);
                return await _hiDriveClient.Meta.Get(path,
                    fields: new[]
                    {
                        Meta.Fields.ContentHash, Meta.Fields.MetadataHash, Meta.Fields.NameHash,
                        Meta.Fields.MetadataOnlyHash, Meta.Fields.ModificationTime, Meta.Fields.CreationTime
                    }).ExecuteAsync(token);
            }
            catch (ServiceException e) when(e.Error.Code == "404")
            {
                _logger.Verbose("Remote path {path} does not exist!", path);
                return null;
            }
        }

        private async Task<DirectoryItem> CreateDirectoryPath(string folderDestinationPath, CancellationToken token = default(CancellationToken))
        {
            var strings = folderDestinationPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var home = await _hiDriveClient.Directory.GetHome(fields: new[] { DirectoryBaseItem.Fields.Id }).ExecuteAsync(token);
            DirectoryItem directoryItem = null;
            try
            {
                directoryItem = await _hiDriveClient.Directory.Get(folderDestinationPath, home.Id, new[] { DirectoryMember.Directory }, new[] { DirectoryBaseItem.Fields.Id, DirectoryBaseItem.Fields.Name, DirectoryBaseItem.Fields.Path }).ExecuteAsync(token);
                return directoryItem;
            }
            catch (ServiceException serviceException) when (serviceException.Error.Code == "404")
            {
                directoryItem = home;
                foreach (var s in strings)
                {
                    try
                    {
                        directoryItem = await _hiDriveClient.Directory.Get(s, directoryItem.Id, new[] { DirectoryMember.Directory }, new[] { DirectoryBaseItem.Fields.Id, DirectoryBaseItem.Fields.Name, DirectoryBaseItem.Fields.Path }).ExecuteAsync(token);
                    }
                    catch (ServiceException exception) when (exception.Error.Code == "404")
                    {
                        directoryItem = await _hiDriveClient.Directory.Create(s, directoryItem.Id).ExecuteAsync(token);
                    }
                }
            }
            return directoryItem;
        }

        private static string CombinePaths(string path1, string path2)
        {
            var builder = new StringBuilder(path1.Length + path2.Length + 2);
            builder.Append(path1);
            if (!path1.EndsWith('/'))
            {
                if (!path2.StartsWith('/'))
                {
                    builder.Append('/');
                }
                builder.Append(path2);
            }
            else if (path2.StartsWith('/'))
            {
                builder.Append(path2.AsSpan().Slice(1));
            }
            else
            {
                builder.Append(path2);
            }

            return builder.ToString();
        }
    }
}