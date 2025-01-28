using Unity.Services.RemoteConfig;
using Conkist.GDK.Services.Backend;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.Services.CloudSave;

namespace Conkist.GDK.Services.Unity
{
    public class UnityDataProvider : BaseDataProvider
    {
        public struct userAttributes{
            public string partnerKey;
        }

        public struct appAttributes{
            public string appVersion;

        }

        public bool FetchingPlayerData { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            RemoteConfigService.Instance.FetchCompleted += OnAppDataFetched;
        }

        private void OnDestroy() {
            RemoteConfigService.Instance.FetchCompleted -= OnAppDataFetched;
        }

        public override void FetchAppData()
        {
            _cachedAppData.Clear();

            RemoteConfigService.Instance.FetchConfigs<userAttributes, appAttributes>(new userAttributes(), new appAttributes());
        }

        public override async UniTask FetchAppDataAsync()
        {
            FetchAppData();
            await UniTask.WaitUntil(()=> _cachedAppData.Count > 0);
        }

        public override void FetchPlayerData()
        {
            FetchPlayerDataAsync().Forget();
        }

        public override async UniTask FetchPlayerDataAsync()
        {
            FetchingPlayerData = true;
            var result = await CloudSaveService.Instance.Data.Player.LoadAllAsync();
            foreach (var pair in result)
            {
                _cachedPlayerData[pair.Key] = pair.Value?.ToString() ?? string.Empty;
            }
            FetchingPlayerData = false;
        }

        private void OnAppDataFetched(ConfigResponse response)
        {
            Debug.Log("Data Fetched!");
            //Update Data
            foreach (var pair in RemoteConfigService.Instance.appConfig.config)
            {
                _cachedAppData[pair.Key] = pair.Value?.ToString() ?? string.Empty;
            }

            InvokeAppDataFetched();
        }
    }
}
