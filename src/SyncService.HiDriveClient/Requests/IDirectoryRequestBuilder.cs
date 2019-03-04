using System;
using System.Collections.Generic;
using SyncService.HiDriveClient.Models;

namespace SyncService.HiDriveClient.Requests
{
    public interface IDirectoryRequestBuilder
    {
        IRequest<DirectoryItem> Get(string path = null, string pid = null, IEnumerable<DirectoryMember> members = null, IEnumerable<string> fields = null, int? offset = null, int? limit = null,
            string snapshot = null);

        IRequest<DirectoryItem> Create(string path = null, string pid = null, DateTime modificationTime = default(DateTime), DateTime dirModificationTime = default(DateTime));

        IRequest Delete(string path = null, string pid = null, bool? isRecursive = null, DateTime dirModificationTime = default(DateTime));

        IRequest<DirectoryItem> GetHome(IEnumerable<DirectoryMember> members = null, IEnumerable<string> fields = null, int? offset = null, int? limit = null, string snapshot = null);

        IRequest<DirectoryItem> Copy(string sourcePath = null, string sourceId = null, string destPath = null, string destId = null, string snapshot = null);

        IRequest<DirectoryItem> Move(string sourcePath = null, string sourceId = null, string destPath = null, string destId = null);

        IRequest<DirectoryItem> Rename(string path = null, string pid = null, string name = null);
    }
}