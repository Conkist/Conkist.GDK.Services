using System;
using System.Collections.Generic;
using Conkist.GDK.Services.Conkist;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Conkist.GDK.Services.Tests
{
    /// <summary>
    /// Offline checks of the link contract with the Conkist API (no network).
    /// The JSON samples mirror what conkist-api returns.
    /// </summary>
    public class ConkistLinkContractTests
    {
        [Test]
        public void Initialize_Response_ForNewPlayer_ReadsSecretOnce()
        {
            const string json = "{\"playerId\":\"1001\",\"playerSecret\":\"abc\",\"isNew\":true,\"sessionToken\":\"jwt\",\"expiresIn\":300}";
            var r = JsonConvert.DeserializeObject<InitializeResponse>(json);

            Assert.AreEqual("1001", r.PlayerId);
            Assert.AreEqual("abc", r.PlayerSecret);
            Assert.IsTrue(r.IsNew);
            Assert.AreEqual(300, r.ExpiresIn);
        }

        [Test]
        public void Initialize_Response_ForReturningPlayer_HasNoSecret()
        {
            const string json = "{\"playerId\":\"1001\",\"playerSecret\":null,\"isNew\":false,\"sessionToken\":\"jwt\",\"expiresIn\":300}";
            var r = JsonConvert.DeserializeObject<InitializeResponse>(json);

            Assert.IsNull(r.PlayerSecret);
            Assert.IsFalse(r.IsNew);
        }

        [Test]
        public void CreateLink_Response_Has8DigitCodeAndLinkId()
        {
            const string json = "{\"linkId\":42,\"code\":\"12345678\",\"expiresAt\":\"2026-11-12T12:05:00Z\",\"deeplink\":\"https://app.conkist.me/connect?token=12345678\"}";
            var r = JsonConvert.DeserializeObject<CreateSdkLinkResponse>(json);

            Assert.AreEqual(42, r.LinkId);
            Assert.AreEqual("12345678", r.Code);
            Assert.AreEqual(DateTimeKind.Utc, r.ExpiresAt.ToUniversalTime().Kind);
        }

        [TestCase("pending")]
        [TestCase("confirmed")]
        [TestCase("revoked")]
        [TestCase("expired")]
        public void LinkStatus_Names_MatchTheApi(string status)
        {
            var r = JsonConvert.DeserializeObject<GetLinkStatusResponse>("{\"linkId\":1,\"status\":\"" + status + "\",\"revokedBy\":\"account\"}");
            Assert.AreEqual(status, r.Status);
            Assert.Contains(status, new[]
            {
                LinkStatusNames.Pending, LinkStatusNames.Confirmed, LinkStatusNames.Revoked, LinkStatusNames.Expired
            });
        }

        [Test]
        public void PlayerLinks_List_Parses()
        {
            const string json = "[{\"linkId\":7,\"status\":\"confirmed\",\"alias\":\"Mae\",\"confirmedAt\":\"2026-11-12T12:00:00Z\"}]";
            var list = JsonConvert.DeserializeObject<List<PlayerLink>>(json);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Mae", list[0].Alias);
        }

        [Test]
        public void OnlyPlayerCredentialErrors_DropTheSavedPlayer()
        {
            Assert.IsTrue(ConkistBackendService.IsPlayerCredentialError("{\"message\":\"Invalid player credentials.\"}"));
            // A bad public key is also a 401. It must not wipe the saved player.
            Assert.IsFalse(ConkistBackendService.IsPlayerCredentialError("{\"message\":\"Invalid Public API Key.\"}"));
            Assert.IsFalse(ConkistBackendService.IsPlayerCredentialError(null));
        }

        [Test]
        public void PlayerPrefsStore_SavesLoadsAndClears()
        {
            var store = new PlayerPrefsCredentialStore();
            store.Clear();
            Assert.IsFalse(store.TryLoad(out _, out _));

            store.Save("p1", "s1");
            Assert.IsTrue(store.TryLoad(out var id, out var secret));
            Assert.AreEqual("p1", id);
            Assert.AreEqual("s1", secret);

            store.Clear();
            Assert.IsFalse(store.TryLoad(out _, out _));
        }
    }
}
