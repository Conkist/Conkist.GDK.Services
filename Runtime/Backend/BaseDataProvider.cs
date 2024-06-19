using UnityEngine;
using UnityEngine.Events;
using Conkist.Tools;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Conkist.Services.Backend
{
    public abstract class BaseDataProvider : Singleton<BaseDataProvider>
    {
        protected static Dictionary<string, string> _cachedData;

        [SerializeField] protected UnityEvent _onDataFetched;

        public UnityAction onError;

        public abstract void FetchData();

        public virtual async UniTask FetchDataAsync()
        {
            await UniTask.Yield();
        }
    }
}
