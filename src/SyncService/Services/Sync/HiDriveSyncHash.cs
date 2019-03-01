using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace SyncService.Services.Sync
{
    public class HiDriveSyncHash
    {
        private HiDriveSyncHash()
        {
        }

        public static HiDriveSyncHash Create()
        {
            return new HiDriveSyncHash();
        }

        public byte[] CalculateNameHash(string name)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(Encoding.UTF8.GetBytes(name));
            }
        }

        public byte[] CalculateContentHash(FileStream file)
        {
            using (var sha1 = SHA1.Create())
            {
                var hashes = CalculateL0Hashes(file, sha1);

                if (hashes.Length > 1)
                {
                    hashes = CalculateL1Hashes(hashes, sha1);
                    while (hashes.Length > 1)
                    {
                        hashes = CalculateL1Hashes(hashes, sha1);
                    }
                }

                if (hashes.Length == 1)
                {
                    return hashes[0] ?? new byte[20];
                }
            }

                

            throw new InvalidOperationException($"ContentHash for file '{file.Name}' could not be computed!");
        }

        public byte[] CalculateContentHash(DirectoryInfo directory, Predicate<FileSystemInfo> excludePredicate = null)
        {
            var lockObject = new object();
            byte[] hash = new byte[20];
            try
            {
                //Parallel.ForEach(directory.EnumerateFileSystemInfos(), new ParallelOptions {MaxDegreeOfParallelism = 2},
                //    fileSystemInfo =>
                //    {
                //        if (excludePredicate != null && excludePredicate(fileSystemInfo))
                //        {
                //            return;
                //        }

                //        if (fileSystemInfo is FileInfo fileInfo)
                //        {
                //            var metadataHash = CalculateMetadataHash(fileInfo);
                //            using (var file = fileInfo.OpenRead())
                //            {
                //                var contentHash = CalculateContentHash(file);
                //                metadataHash.AddHash(contentHash);
                //            }

                //            lock (metadataHash)
                //            {

                //            }
                //        }
                //        else if (fileSystemInfo is DirectoryInfo directoryInfo)
                //        {
                //            hash.AddHash(CalculateMetadataHash(directoryInfo));
                //            hash.AddHash(CalculateContentHash(directoryInfo, excludePredicate));
                //        }
                //    });

                Parallel.ForEach(directory.EnumerateFileSystemInfos(), new ParallelOptions { MaxDegreeOfParallelism = 8 }, () => new byte[20], (fileSystemInfo, state, arg3) =>
                //fileSystemInfo =>
                {
                    //var h = new byte[20];
                    //h.AddHash(arg3);
                    if (excludePredicate == null || !excludePredicate(fileSystemInfo))
                    {
                        if (fileSystemInfo is FileInfo fileInfo)
                        {
                            var metadataHash = CalculateMetadataHash(fileInfo);
                            arg3.AddHash(metadataHash);
                            //h.AddHash(metadataHash);
                            using (var file = fileInfo.OpenRead())
                            {
                                var contentHash = CalculateContentHash(file);
                                arg3.AddHash(contentHash);
                                //metadataHash.AddHash(contentHash);
                                //h.AddHash(contentHash);
                            }
                            //arg3.Add(metadataHash);
                        }
                        if (fileSystemInfo is DirectoryInfo directoryInfo)
                        {
                            var metadataHash = CalculateMetadataHash(directoryInfo);
                            arg3.AddHash(metadataHash);
                            //h.AddHash(metadataHash);
                            var contentHash = CalculateContentHash(directoryInfo, excludePredicate);
                            //h.AddHash(contentHash);
                            //metadataHash.AddHash(contentHash);
                            arg3.AddHash(contentHash);
                        }
                    }

                    return arg3;
                }, bytes =>
            {
                lock (lockObject)
                {
                    hash.AddHash(bytes);
                }
            });

                //foreach (var fileSystemInfo in directory.EnumerateFileSystemInfos())
                //{
                //    if (excludePredicate != null && excludePredicate(fileSystemInfo))
                //    {
                //        continue;
                //    }

                //    if (fileSystemInfo is FileInfo fileInfo)
                //    {
                //        hash.AddHash(CalculateMetadataHash(fileInfo));
                //        using (var file = fileInfo.OpenRead())
                //        {
                //            hash.AddHash(CalculateContentHash(file));
                //        }
                //    }
                //    else if (fileSystemInfo is DirectoryInfo directoryInfo)
                //    {
                //        hash.AddHash(CalculateMetadataHash(directoryInfo));
                //        hash.AddHash(CalculateContentHash(directoryInfo, excludePredicate));
                //    }
                //}
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error while computing directory content Hash (Dir: {directory})", directory.FullName);
                throw;
            }
            return hash;
        }

        public byte[] CalculateContentHashSingle(DirectoryInfo directory, Predicate<FileSystemInfo> excludePredicate = null)
        {
            byte[] hash = new byte[20];
            try
            {
                foreach (var fileSystemInfo in directory.EnumerateFileSystemInfos())
                {
                    if (excludePredicate != null && excludePredicate(fileSystemInfo))
                    {
                        continue;
                    }

                    if (fileSystemInfo is FileInfo fileInfo)
                    {
                        hash.AddHash(CalculateMetadataHash(fileInfo));
                        using (var file = fileInfo.OpenRead())
                        {
                            hash.AddHash(CalculateContentHash(file));
                        }
                    }
                    else if (fileSystemInfo is DirectoryInfo directoryInfo)
                    {
                        hash.AddHash(CalculateMetadataHash(directoryInfo));
                        hash.AddHash(CalculateContentHash(directoryInfo, excludePredicate));
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error while computing directory content Hash (Dir: {directory})", directory.FullName);
                throw;
            }
            return hash;
        }

        /// <summary>
        /// SHA1(SHA1(Filename as UTF8), Size as 64-bit Integer, Last Writetime UTC (UNIX Timestamp) as 64-bit Integer)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public byte[] CalculateMetadataHash(FileInfo file)
        {
            var nameHash = CalculateNameHash(file.Name);
            var bytes = ArrayPool<byte>.Shared.Rent(20 + 8 + 8);
            Array.Copy(nameHash, bytes, 20);
            var unixTimeSeconds = new DateTimeOffset(file.LastWriteTimeUtc).ToUnixTimeSeconds();
            bytes[20] = (byte)file.Length;
            bytes[21] = (byte)(file.Length >> 8);
            bytes[22] = (byte)(file.Length >> 0x10);
            bytes[23] = (byte)(file.Length >> 0x18);
            bytes[24] = (byte)(file.Length >> 32);
            bytes[25] = (byte)(file.Length >> 40);
            bytes[26] = (byte)(file.Length >> 48);
            bytes[27] = (byte)(file.Length >> 56);
            bytes[28] = (byte)unixTimeSeconds;
            bytes[29] = (byte)(unixTimeSeconds >> 8);
            bytes[30] = (byte)(unixTimeSeconds >> 0x10);
            bytes[31] = (byte)(unixTimeSeconds >> 0x18);
            bytes[32] = (byte)(unixTimeSeconds >> 32);
            bytes[33] = (byte)(unixTimeSeconds >> 40);
            bytes[34] = (byte)(unixTimeSeconds >> 48);
            bytes[35] = (byte)(unixTimeSeconds >> 56);

            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(bytes, 0, 36);
                ArrayPool<byte>.Shared.Return(bytes);
                return hash;
            }
        }

        /// <summary>
        /// SHA1(SHA1(Filename as UTF8), Last Writetime UTC (UNIX Timestamp) as 64-bit Integer)
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public byte[] CalculateMetadataHash(DirectoryInfo directory)
        {
            var nameHash = CalculateNameHash(directory.Name);
            var bytes = ArrayPool<byte>.Shared.Rent(20 + 8);
            Array.Copy(nameHash, bytes, 20);
            var unixTimeSeconds = new DateTimeOffset(directory.LastWriteTimeUtc).ToUnixTimeSeconds();
            bytes[20] = (byte)unixTimeSeconds;
            bytes[21] = (byte)(unixTimeSeconds >> 8);
            bytes[22] = (byte)(unixTimeSeconds >> 0x10);
            bytes[23] = (byte)(unixTimeSeconds >> 0x18);
            bytes[24] = (byte)(unixTimeSeconds >> 32);
            bytes[25] = (byte)(unixTimeSeconds >> 40);
            bytes[26] = (byte)(unixTimeSeconds >> 48);
            bytes[27] = (byte)(unixTimeSeconds >> 56);

            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(bytes, 0, 28);
                ArrayPool<byte>.Shared.Return(bytes);
                return hash;
            }
        }

        public byte[] CalculateMetadataOnlyHash(DirectoryInfo directory, Predicate<FileSystemInfo> excludePredicate = null)
        {
            byte[] hash = new byte[20];
            foreach (var fileSystemInfo in directory.EnumerateFileSystemInfos())
            {
                if (excludePredicate != null && excludePredicate(fileSystemInfo))
                {
                    continue;
                }

                if (fileSystemInfo is FileInfo fileInfo)
                {
                    hash.AddHash(CalculateMetadataHash(fileInfo));
                }
                else if (fileSystemInfo is DirectoryInfo directoryInfo)
                {
                    hash.AddHash(CalculateMetadataHash(directoryInfo));
                }
            }

            return hash;
        }

        private Span<byte[]> CalculateL0Hashes(FileStream stream, SHA1 sha1)
        {
            if (stream.Length == 0) return new[] {new byte[20]};

            var l = (int) Math.Ceiling(stream.Length / 4096D);
            var hashes = new byte[l][];
            var j = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(4096);

            stream.Seek(0, SeekOrigin.Begin);
            while (true)
            {
                var i = stream.Read(buffer, 0, 4096);
                if (i == 0) break;
                if (i < 4096)
                {
                    for (int k = i; k < 4096; k++)
                    {
                        buffer[k] = 0;
                    }
                }

                if (!IsEmpty(buffer, 4096))
                {
                    hashes[j] = sha1.ComputeHash(buffer, 0, 4096);
                }

                j++;
            }

            stream.Seek(0, SeekOrigin.Begin);
            ArrayPool<byte>.Shared.Return(buffer);
            return hashes.AsSpan();
        }

        private Span<byte[]> CalculateL1Hashes(Span<byte[]> l0Hashes, SHA1 sha1)
        {
            var i1 = (int)Math.Ceiling(l0Hashes.Length / 256D);

            var l1Hashes = new byte[i1][];

            for (int i = 0; i < i1; i++)
            {
                if((i*256 + 256) > l0Hashes.Length)
                    l1Hashes[i] = CalculateL1Hash(l0Hashes.Slice(i * 256), sha1);
                else
                    l1Hashes[i] = CalculateL1Hash(l0Hashes.Slice(i * 256, 256), sha1);
            }
            return l1Hashes.AsSpan();
        }

        private byte[] CalculateL1Hash(Span<byte[]> l0Hashes, SHA1 sha1)
        {
            if (!IsEmpty(l0Hashes))
            {
                var hash = new byte[20];
                for (int i = 0; i < l0Hashes.Length; i++)
                {
                    if (l0Hashes[i] != null)
                        hash.AddHash(CalculateIndexedHash(l0Hashes[i], i, sha1));
                }
                return hash;
            }

            return null;
        }

        private byte[] CalculateIndexedHash(byte[] hash, int index, SHA1 sha1)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(21);
            for (int i = 0; i < 20; i++)
            {
                bytes[i] = hash[i];
            }

            bytes[20] = (byte)(index % 256);
            var h = sha1.ComputeHash(bytes, 0, 21);
            ArrayPool<byte>.Shared.Return(bytes);
            return h;
        }

        private bool IsEmpty(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (buffer[i] != 0) return false;
            }

            return true;
        }

        private bool IsEmpty(Span<byte[]> l0Hashes)
        {
            foreach (var l0Hash in l0Hashes)
            {
                if (l0Hash != null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}