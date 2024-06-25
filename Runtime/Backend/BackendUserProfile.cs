using System;

namespace Conkist.GDK.Services.Backend
{
    /// <summary>
    /// Represents the profile information of a backend user.
    /// </summary>
    public class BackendUserProfile
    {
        /// <summary>
        /// Gets the unique identifier for the player.
        /// </summary>
        public string PlayerId { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the profile has been loaded.
        /// </summary>
        public bool ProfileLoaded { get; set; }

        /// <summary>
        /// Gets or sets the username of the player.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the profile.
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// Gets or sets the last login date of the profile.
        /// </summary>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Gets or sets the last update date of the profile.
        /// </summary>
        public DateTime? LastUpdateDate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendUserProfile"/> class.
        /// </summary>
        /// <param name="playerId">The unique identifier for the player.</param>
        public BackendUserProfile(string playerId)
        {
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendUserProfile"/> class.
        /// </summary>
        public BackendUserProfile() { }

        /// <summary>
        /// Determines whether the player is new based on the presence of a username.
        /// </summary>
        /// <returns><c>true</c> if the player is new; otherwise, <c>false</c>.</returns>
        public bool IsNewPlayer() => Username == null;
    }
}