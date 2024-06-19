using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using Conkist.Services.Backend;
using UnityEngine;

namespace Conkist.Services.Playfab
{
    public class PlayfabDataProvider: BaseDataProvider
    {
        public override void FetchData()
        {
            _cachedData = null;
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnPlayfabDataFetched, OnPlayfabError);
        }

        public override async UniTask FetchDataAsync()
        {
            FetchData();
            await UniTask.WaitUntil(() => _cachedData != null);
        }

        private void OnPlayfabDataFetched(GetTitleDataResult result)
        {
            _cachedData = result.Data;
            _onDataFetched?.Invoke();
        }

        private void OnPlayfabError(PlayFabError error)
        {
            Debug.LogError("PlayfabClient failed to fetch TitleData");
        }
    }
}