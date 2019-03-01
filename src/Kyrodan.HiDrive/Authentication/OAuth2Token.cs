using System;
using System.Runtime.Serialization;

namespace Kyrodan.HiDrive.Authentication
{
    [DataContract]
    public class OAuth2Token
    {
        private int? _expiresIn;

        public OAuth2Token()
        {
            CreatedAt = DateTime.Now;
            ExpiresAt = CreatedAt.AddSeconds(-1);
        }

        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "expires_in")]
        public int? ExpiresIn
        {
            get => _expiresIn;
            set
            {
                _expiresIn = value;
                if (_expiresIn.HasValue)
                {
                    ExpiresAt = CreatedAt.AddSeconds(_expiresIn.Value).AddMinutes(-1);
                }
            }
        }

        [DataMember(Name = "token_type")]
        public string TokenType { get; set; }

        [DataMember(Name = "refresh_token")]
        public string RefreshToken { get; set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }

        public bool IsValid => DateTime.Now < ExpiresAt;
    }
}