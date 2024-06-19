using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Conkist.Tools;

namespace Conkist.Services.Backend
{
    [RequireComponent(typeof(BaseBackendService))]
    public abstract class BackendUserStatistics : Singleton<BackendUserStatistics>
    {
        protected List<KeyValuePair<string, int>> _statistics = new List<KeyValuePair<string, int>>();

        [WhenAuthenticated]
        public abstract void Load(params string[] keys);

        public static bool Get(string key, out int value)
        {
            value = 0;
            if(Instance._statistics == null || Instance._statistics.Count == 0) return false;

            var pair = Instance._statistics.Where(p => p.Key == key).FirstOrDefault();
            if(pair.Equals(default(KeyValuePair<string,int>))) return false;

            value = pair.Value;
            return true;
        }

        [WhenAuthenticated]
        public abstract void Save(List<KeyValuePair<string,int>> statistics);

        [WhenAuthenticated]
        public abstract void Save(string key, int value);
    }
}
