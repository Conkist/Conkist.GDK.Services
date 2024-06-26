using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PlayFab.ClientModels;
using PlayFab;
using Conkist.GDK.Services.Backend;

namespace Conkist.GDK.Services.Playfab
{
    /// <summary>
    /// Handles user statistics using PlayFab.
    /// Requires login with PlayFab backend.
    /// </summary>
    [RequireComponent(typeof(PlayfabBackendService))]
    public class PlayfabUserStatistics : BackendUserStatistics
    {
        /// <summary>
        /// Singleton instance of PlayfabUserStatistics.
        /// </summary>
        public static new PlayfabUserStatistics Instance => (PlayfabUserStatistics)_instance;

        /// <summary>
        /// Indicates if the statistics are in sync with the server.
        /// </summary>
        public bool InSync { get; protected set; }

        /// <summary>
        /// Event triggered when statistics loading starts.
        /// </summary>
        public UnityAction onStatisticLoadStart;
        /// <summary>
        /// Event triggered when statistics loading ends.
        /// </summary>
        public UnityAction onStatisticLoadEnd;
        /// <summary>
        /// Event triggered when statistics are successfully sent.
        /// </summary>
        public UnityAction onStatisticSent;

        /// <summary>
        /// Event triggered when leaderboard is loaded.
        /// Returns a list of leaderboard entries.
        /// </summary>
        public UnityAction<List<LeaderboardEntry>> onLeaderboardLoaded;

        /// <summary>
        /// Loads the user statistics for the specified keys.
        /// </summary>
        /// <param name="keys">The keys of the statistics to load.</param>
        public override void Load(params string[] keys)
        {
            InSync = false;
            onStatisticLoadStart?.Invoke();

            var request = new GetPlayerStatisticsRequest
            {
                StatisticNames = keys.ToList()
            };

            PlayFabClientAPI.GetPlayerStatistics(request, OnStatisticLoaded, OnError);
        }

        /// <summary>
        /// Callback method when PlayFab statistics are successfully loaded.
        /// </summary>
        /// <param name="result">The result containing the statistics.</param>
        private void OnStatisticLoaded(GetPlayerStatisticsResult result)
        {
            // Clear and update the statistics
            foreach (var stat in result.Statistics)
            {
                var pair = new KeyValuePair<string, int>(stat.StatisticName, stat.Value);
                _statistics.RemoveAll(p => p.Key == pair.Key);
                _statistics.Add(pair);
            }

            onStatisticLoadEnd?.Invoke();
            InSync = true;
        }

        /// <summary>
        /// Saves the provided statistics to PlayFab.
        /// </summary>
        /// <param name="statistics">List of key-value pairs representing the statistics.</param>
        public override void Save(List<KeyValuePair<string, int>> statistics)
        {
            var stats = statistics.Select(s => new StatisticUpdate
            {
                StatisticName = s.Key,
                Value = s.Value
            }).ToList();

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = stats
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticSent, OnError);
        }

        /// <summary>
        /// Saves a single statistic to PlayFab.
        /// </summary>
        /// <param name="key">The key of the statistic.</param>
        /// <param name="value">The value of the statistic.</param>
        public override void Save(string key, int value)
        {
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = key, Value = value }
                }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticSent, OnError);
        }

        /// <summary>
        /// Callback method when statistics are successfully sent to PlayFab.
        /// </summary>
        /// <param name="result">The result of the update operation.</param>
        private void OnStatisticSent(UpdatePlayerStatisticsResult result)
        {
            Debug.Log($"[{GetType().Name}] Statistic Sent", this);
            onStatisticSent?.Invoke();
        }

        /// <summary>
        /// Fetches the leaderboard around the player for the specified statistic.
        /// </summary>
        /// <param name="key">The key of the statistic.</param>
        public void Leaderboard(string key)
        {
            var request = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = key,
                PlayFabId = PlayfabBackendService.UserProfile.PlayerId
            };

            PlayFabClientAPI.GetLeaderboardAroundPlayer(request, OnLeaderboardLoaded, OnError);
        }

        /// <summary>
        /// Callback method when the leaderboard is successfully loaded.
        /// </summary>
        /// <param name="result">The result containing the leaderboard entries.</param>
        private void OnLeaderboardLoaded(GetLeaderboardAroundPlayerResult result)
        {
            var leaderboard = result.Leaderboard.Select(entry => new LeaderboardEntry(
                playerId: entry.PlayFabId,
                username: entry.DisplayName,
                position: entry.Position + 1,
                value: entry.StatValue)).ToList();

            onLeaderboardLoaded?.Invoke(leaderboard);
        }

        /// <summary>
        /// Callback method for handling PlayFab errors.
        /// </summary>
        /// <param name="error">The error information from PlayFab.</param>
        private void OnError(PlayFabError error)
        {
            Debug.LogWarning(error.GenerateErrorReport());
        }
    }
}
