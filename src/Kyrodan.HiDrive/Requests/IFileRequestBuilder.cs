using System;
using Kyrodan.HiDrive.Models;

namespace Kyrodan.HiDrive.Requests
{
    public interface IFileRequestBuilder
    {
        //IFileRequest Request();
        //IFileRequest Request(IEnumerable<KeyValuePair<string, string>> additionalParameters);

        IReceiveStreamRequest Download(string path = null, string pid = null, string snapshot = null);

        ISendStreamRequest<FileItem> Upload(string name, string dir = null, string dir_id = null, DateTime modificationTime = default(DateTime), DateTime dirModificationTime = default(DateTime), UploadMode mode = UploadMode.CreateOnly);

        IRequest Delete(string path = null, string pid = null, DateTime dirModificationTime = default(DateTime));

        IRequest<FileItem> Copy(string sourcePath = null, string sourceId = null, string destPath = null, string destId = null, string snapshot = null);

        IRequest<FileItem> Rename(string path = null, string pid = null, string name = null);

        IRequest<FileItem> Move(string sourcePath = null, string sourceId = null, string destPath = null, string destId = null);
        IRequest<FileHash> Hash(string path = null, string pid = null, int level = 1, string ranges = "-");

        ISendStreamRequest Patch(string path = null, string pid = null, DateTime modificationTime = default(DateTime),
            long offset = 0);
    }
}