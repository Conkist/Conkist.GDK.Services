using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Conkist.GDK.Services.Backend
{
    [RequireComponent(typeof(BaseBackendService))]
    public abstract class BackendUserStatistics : Singleton<BackendUserStatistics>
    {
        protected List<KeyValuePair<string, int>> _statistics = new List<KeyValuePair<string, int>>();

        /// <summary>
        /// Loads the user statistics for the specified keys. This method should be implemented in a derived class.
        /// </summary>
        /// <param name="keys">The keys of the statistics to load.</param>
        [WhenAuthenticated]
        public abstract void Load(params string[] keys);

        /// <summary>
        /// Gets the value of the specified statistic.
        /// </summary>
        /// <param name="key">The key of the statistic.</param>
        /// <param name="value">The value of the statistic.</param>
        /// <returns><c>true</c> if the statistic was found; otherwise, <c>false</c>.</returns>
        public static bool Get(string key, out int value)
        {
            value = 0;
            if(Instance._statistics == null || Instance._statistics.Count == 0) return false;

            var pair = Instance._statistics.Where(p => p.Key == key).FirstOrDefault();
            if(pair.Equals(default(KeyValuePair<string,int>))) return false;

            value = pair.Value;
            return true;
        }

        /// <summary>
        /// Saves the specified user statistics. This method should be implemented in a derived class.
        /// </summary>
        /// <param name="statistics">The statistics to save.</param>
        [WhenAuthenticated]
        public abstract void Save(List<KeyValuePair<string,int>> statistics);

        /// <summary>
        /// Saves the specified key-value pair statistic. This method should be implemented in a derived class.
        /// </summary>
        /// <param name="key">The key of the statistic.</param>
        /// <param name="value">The value of the statistic.</param>
        [WhenAuthenticated]
        public abstract void Save(string key, int value);
    }
}
