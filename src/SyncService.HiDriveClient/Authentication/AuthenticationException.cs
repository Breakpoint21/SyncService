using System;

namespace SyncService.HiDriveClient.Authentication
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(AuthenticationError error)
            : base(error.ToString())
        {
            Error = error;
        }

        public AuthenticationError Error { get; }
    }
}