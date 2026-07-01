using System;

namespace Conkist.GDK.Services
{
    /// <summary>
    /// Internal model holding the current Conkist session state.
    /// Tracks the player ID, JWT session token, and expiration time
    /// for proactive token renewal.
    /// </summary>
    internal class ConkistSessionData
    {
        /// <summary>
        /// The Snowflake-generated player ID returned by the API.
        /// </summary>
        public string PlayerId;

        /// <summary>
        /// The JWT session token used for authenticating telemetry requests.
        /// </summary>
        public string SessionToken;

        /// <summary>
        /// The UTC time at which the session token expires (5 minutes after issuance).
        /// </summary>
        public DateTime ExpiresAt;

        /// <summary>
        /// Returns true if the token will expire within 30 seconds.
        /// Used to trigger proactive token renewal before a telemetry flush.
        /// </summary>
        public bool IsExpiringSoon => (ExpiresAt - DateTime.UtcNow).TotalSeconds < 30;

        /// <summary>
        /// Returns true if the token has already expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Returns true if the session has been initialized with valid data.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(SessionToken) && !string.IsNullOrEmpty(PlayerId);
    }
}
