using System;

namespace Conkist.Services.Backend
{
    public class BackendUserProfile
    {

        public string PlayerId { get; set; }
        public bool ProfileLoaded { get; set; }
        public string Username { get; set; }

        public DateTime? CreateDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        
        public bool IsNewPlayer() => Username == null;

        public BackendUserProfile(string playerId)
        {
            this.PlayerId = playerId;
        }
    }
}