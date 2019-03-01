using System;
using System.Collections.Generic;
using System.Globalization;
using Kyrodan.HiDrive.Models;

namespace Kyrodan.HiDrive.Requests
{
    internal class FileRequestBuilder : BaseRequestBuilder, IFileRequestBuilder
    {
        public FileRequestBuilder(IBaseClient client)
            : base("file", client)
        {
        }

        public IReceiveStreamRequest Download(string path = null, string pid = null, string snapshot = null)
        {
            var request = new ReceiveStreamRequest(RequestUrl, Client);

            if (path != null) request.QueryOptions.Add(new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)));
            if (pid != null) request.QueryOptions.Add(new KeyValuePair<string, string>("pid", pid));
            if (snapshot != null) request.QueryOptions.Add(new KeyValuePair<string, string>("snapshot", snapshot));

            return request;
        }

        public ISendStreamRequest<FileItem> Upload(string name, string dir = null, string dir_id = null, DateTime modificationTime = default(DateTime), DateTime dirModificationTime = default(DateTime), UploadMode mode = UploadMode.CreateOnly)
        {
            var request = new SendStreamRequest<FileItem>(RequestUrl, Client);

            switch (mode)
            {
                case UploadMode.CreateOnly:
                    request.Method = "POST";
                    break;
                case UploadMode.CreateOrUpdate:
                    request.Method = "PUT";
                    break;
                default:
                    throw new NotImplementedException();
            }

            request.QueryOptions.Add(new KeyValuePair<string, string>("name", Uri.EscapeDataString(name)));
            if (dir != null) request.QueryOptions.Add(new KeyValuePair<string, string>("dir", Uri.EscapeDataString(dir)));
            if (dir_id != null) request.QueryOptions.Add(new KeyValuePair<string, string>("dir_id", dir_id));
            if (modificationTime != default(DateTime))
            {
                request.QueryOptions.Add(new KeyValuePair<string, string>("mtime", new DateTimeOffset(modificationTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
            }
            if (dirModificationTime != default(DateTime))
            {
                request.QueryOptions.Add(new KeyValuePair<string, string>("parent_mtime", new DateTimeOffset(dirModificationTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
            }

            return request;
        }

        public ISendStreamRequest Patch(string path = null, string pid = null, DateTime modificationTime = default(DateTime),
            long offset = 0)
        {
            var request = new SendStreamRequest(RequestUrl, Client)
            {
                Method = "PATCH"
            };

            if (path != null) request.QueryOptions.Add(new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)));
            if (pid != null) request.QueryOptions.Add(new KeyValuePair<string, string>("pid", pid));
            if (modificationTime != default(DateTime))
            {
                request.QueryOptions.Add(new KeyValuePair<string, string>("mtime", new DateTimeOffset(modificationTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
            }
            request.QueryOptions.Add(new KeyValuePair<string, string>("offset", offset.ToString(CultureInfo.InvariantCulture)));
            return request;
        }

        public IRequest Delete(string path = null, string pid = null, DateTime dirModificationTime = default(DateTime))
        {
            var request = new Request(RequestUrl, Client)
            {
                Method = "DELETE"
            };

            if (path != null) request.QueryOptions.Add(new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)));
            if (pid != null) request.QueryOptions.Add(new KeyValuePair<string, string>("pid", pid));
            if (dirModificationTime != default(DateTime))
            {
                request.QueryOptions.Add(new KeyValuePair<string, string>("parent_mtime", new DateTimeOffset(dirModificationTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
            }

            return request;
        }

        public IRequest<FileItem> Copy(string sourcePath = null, string sourceId = null, string destPath = null, string destId = null,
            string snapshot = null)
        {
            var request = new Request<FileItem>(AppendSegmentToRequestUrl("copy"), Client)
            {
                Method = "POST"
            };

            if (sourcePath != null) request.QueryOptions.Add(new KeyValuePair<string, string>("src", Uri.EscapeDataString(sourcePath)));
            if (sourceId != null) request.QueryOptions.Add(new KeyValuePair<string, string>("src_id", sourceId));
            if (destPath != null) request.QueryOptions.Add(new KeyValuePair<string, string>("dst", Uri.EscapeDataString(destPath)));
            if (destId != null) request.QueryOptions.Add(new KeyValuePair<string, string>("dst_id", destId));
            if (snapshot != null) request.QueryOptions.Add(new KeyValuePair<string, string>("snapshot", snapshot));

            return request;
        }

        public IRequest<FileItem> Rename(string path = null, string pid = null, string name = null)
        {
            var request = new Request<FileItem>(AppendSegmentToRequestUrl("rename"), Client)
            {
                Method = "POST"
            };

            if (path != null) request.QueryOptions.Add(new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)));
            if (pid != null) request.QueryOptions.Add(new KeyValuePair<string, string>("pid", pid));
            if (name != null) request.QueryOptions.Add(new KeyValuePair<string, string>("name", Uri.EscapeDataString(name)));

            return request;
        }

        public IRequest<FileItem> Move(string sourcePath = null, string sourceId = null, string destPath = null, string destId = null)
        {
            var request = new Request<FileItem>(AppendSegmentToRequestUrl("move"), Client)
            {
                Method = "POST"
            };

            if (sourcePath != null) request.QueryOptions.Add(new KeyValuePair<string, string>("src", Uri.EscapeDataString(sourcePath)));
            if (sourceId != null) request.QueryOptions.Add(new KeyValuePair<string, string>("src_id", sourceId));
            if (destPath != null) request.QueryOptions.Add(new KeyValuePair<string, string>("dst", Uri.EscapeDataString(destPath)));
            if (destId != null) request.QueryOptions.Add(new KeyValuePair<string, string>("dst_id", destId));

            return request;

        }

        public IRequest<FileHash> Hash(string path = null, string pid = null, int level = 1, string ranges = null)
        {
            var request = new Request<FileHash>(AppendSegmentToRequestUrl("hash"), Client);

            if (path != null) request.QueryOptions.Add(new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)));
            if (pid != null) request.QueryOptions.Add(new KeyValuePair<string, string>("pid", pid));
            request.QueryOptions.Add(new KeyValuePair<string, string>("level", level.ToString(CultureInfo.InvariantCulture)));
            request.QueryOptions.Add(new KeyValuePair<string, string>("ranges", ranges));

            return request;
        }
    }
}