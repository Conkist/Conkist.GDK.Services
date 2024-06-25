namespace Conkist.GDK.Services
{
    /// <summary>
    /// Represents an entry in the leaderboard with player information.
    /// </summary>
    public struct LeaderboardEntry
    {
        /// <summary>
        /// Gets the ID of the player.
        /// </summary>
        public string PlayerId { get; }

        /// <summary>
        /// Gets the username of the player.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Gets the position of the player in the leaderboard.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets the score value of the player.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardEntry"/> struct.
        /// </summary>
        /// <param name="playerId">The ID of the player.</param>
        /// <param name="username">The username of the player.</param>
        /// <param name="position">The position of the player in the leaderboard.</param>
        /// <param name="value">The score value of the player.</param>
        public LeaderboardEntry(string playerId, string username, int position, int value)
        {
            PlayerId = playerId;
            Username = username;
            Position = position;
            Value = value;
        }
    }
}