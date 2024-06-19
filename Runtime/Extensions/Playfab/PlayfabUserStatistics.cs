using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PlayFab.ClientModels;
using PlayFab;
using Conkist.Services.Backend;
using System;

namespace Conkist.Services.Playfab
{
    //All of them require login with backend!!
    [RequireComponent(typeof(PlayfabBackendService))]
    public class PlayfabUserStatistics : BackendUserStatistics
    {
        public static new PlayfabUserStatistics Instance => (PlayfabUserStatistics) _instance;
        public bool InSync { get; protected set; }

        public UnityAction onStatisticLoadStart;
        public UnityAction onStatisticLoadEnd;
        public UnityAction onStatisticSent;

        public UnityAction<List<ILeaderboardEntry>> onLeaderboardLoaded;

        public override void Load(params string[] keys)
        {
            InSync = false;
            onStatisticLoadStart?.Invoke();
            var request = new GetPlayerStatisticsRequest
            {
                StatisticNames = keys.ToList()
            };

            PlayFabClientAPI.GetPlayerStatistics(request,
            OnStatisticLoaded, OnError);
        }

        private void OnStatisticLoaded(GetPlayerStatisticsResult result)
        {
            foreach (var stat in result.Statistics)
            {
                var pair = new KeyValuePair<string, int>(stat.StatisticName, stat.Value);
                _statistics.RemoveAll(p => p.Key == pair.Key);
                _statistics.Add(pair);
            }
            onStatisticLoadEnd?.Invoke();
            InSync = true;
        }

        public override void Save(List<KeyValuePair<string, int>> statistics)
        {
            List<StatisticUpdate> stats = new List<StatisticUpdate>();
            foreach(var s in statistics)
            {
                stats.Add(new StatisticUpdate
                {
                    StatisticName = s.Key,
                    Value = s.Value
                });
            }

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = stats
            };
            
            PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticSent, OnError);
        }

        public override void Save(string key, int value)
        {
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = key,
                        Value = value
                    }
                }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticSent, OnError);
        }

        private void OnStatisticSent(UpdatePlayerStatisticsResult result)
        {

            Debug.Log($"[{GetType().Name}] " + "Statistic Sent", this);
            onStatisticSent?.Invoke();
        }

        public void Leaderboard(string key)
        {
            var request = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = key,
                PlayFabId = PlayfabBackendService.UserProfile.PlayerId
            };

            PlayFabClientAPI.GetLeaderboardAroundPlayer(request, OnLeaderboardLoaded, OnError);
        }

        private void OnLeaderboardLoaded(GetLeaderboardAroundPlayerResult result)
        {
            List<ILeaderboardEntry> leaderboard = new List<ILeaderboardEntry>();

            foreach(var entry in result.Leaderboard)
            {
                leaderboard.Add(new ILeaderboardEntry(
                    playerId: entry.PlayFabId,
                    username: entry.DisplayName,
                    position: entry.Position+1,
                    value: entry.StatValue
                ));
            }

            onLeaderboardLoaded?.Invoke(leaderboard);
        }

        private void OnError(PlayFabError error)
        {
            Debug.LogWarning(error.GenerateErrorReport());
        }
    }
}
