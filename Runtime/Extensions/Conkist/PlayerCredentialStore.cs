using UnityEngine;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// Keeps the anonymous player's id and secret on the device.
    /// The secret proves that this device owns the player: without it, knowing the playerId is not enough.
    /// </summary>
    public interface IPlayerCredentialStore
    {
        /// <summary>True when both id and secret were saved.</summary>
        bool TryLoad(out string playerId, out string playerSecret);
        void Save(string playerId, string playerSecret);
        void Clear();
    }

    /// <summary>
    /// Default store. PlayerPrefs is not a safe vault (on WebGL it is plain browser storage).
    /// That is acceptable here: the secret only guards one anonymous player, never an account.
    /// Replace it with a Keychain/Keystore store through <c>ConkistBackendService.SetCredentialStore</c> if needed.
    /// </summary>
    public class PlayerPrefsCredentialStore : IPlayerCredentialStore
    {
        public const string PlayerIdKey = "Conkist_PlayerId";
        public const string PlayerSecretKey = "Conkist_PlayerSecret";

        public bool TryLoad(out string playerId, out string playerSecret)
        {
            playerId = PlayerPrefs.GetString(PlayerIdKey, null);
            playerSecret = PlayerPrefs.GetString(PlayerSecretKey, null);
            return !string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(playerSecret);
        }

        public void Save(string playerId, string playerSecret)
        {
            PlayerPrefs.SetString(PlayerIdKey, playerId);
            PlayerPrefs.SetString(PlayerSecretKey, playerSecret);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(PlayerIdKey);
            PlayerPrefs.DeleteKey(PlayerSecretKey);
            PlayerPrefs.Save();
        }
    }
}
