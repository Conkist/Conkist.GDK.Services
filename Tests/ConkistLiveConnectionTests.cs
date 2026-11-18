using System.Collections;
using System.IO;
using System.Reflection;
using Conkist.GDK.Services.Conkist;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Conkist.GDK.Services.Tests
{
    /// <summary>
    /// Real connection to a running Conkist API (local dev). Skipped unless run on purpose.
    /// Reads Library/ConkistLiveTest.json (Library is not versioned, so nothing secret is committed):
    ///   { "apiUrl": "http://localhost:5099", "publicKey": "pk_...", "roomCode": "ABCD2345", "accountCode": "12345678" }
    /// roomCode: a code of an open room of the same project. accountCode: a fresh code made by an account in the App (valid 5 min).
    /// </summary>
    [Explicit("Needs a running Conkist API and Library/ConkistLiveTest.json")]
    public class ConkistLiveConnectionTests
    {
        private class MemoryStore : IPlayerCredentialStore
        {
            private string _id, _secret;
            public bool TryLoad(out string playerId, out string playerSecret) { playerId = _id; playerSecret = _secret; return _id != null && _secret != null; }
            public void Save(string playerId, string playerSecret) { _id = playerId; _secret = playerSecret; }
            public void Clear() { _id = _secret = null; }
        }

        [UnityTest]
        public IEnumerator Session_Code_Room_And_Link_Flow() => UniTask.ToCoroutine(async () =>
        {
            var path = Path.Combine(Application.dataPath, "..", "Library", "ConkistLiveTest.json");
            if (!File.Exists(path)) Assert.Inconclusive("Missing Library/ConkistLiveTest.json");
            var cfg = JObject.Parse(File.ReadAllText(path));

            var go = new GameObject("ConkistLive");
            var service = go.AddComponent<ConkistBackendService>();
            try
            {
                // No automatic login and no automatic connection code: the test drives each step.
                var autoLogin = service.GetType().BaseType.GetField("_autoLogin", BindingFlags.Instance | BindingFlags.NonPublic);
                autoLogin?.SetValue(service, false);
                service.AutoGenerateConnectionCode = false;
                service.Setup((string)cfg["publicKey"], null, (string)cfg["apiUrl"]);
                service.SetCredentialStore(new MemoryStore());
                await UniTask.Yield(); // Start() builds the API client

                // 1. Session
                var playerId = await service.LoginAsync();
                Assert.IsFalse(string.IsNullOrEmpty(playerId), "session did not start");

                // 2. A text that is not a code never reaches the server
                var bad = await service.RedeemCodeAsync("abc");
                Assert.AreEqual(400, bad.StatusCode);

                // 3. Room code through the unified door (a link text is also accepted)
                var room = await service.RedeemCodeAsync("https://app.conkist.me/connect?token=" + (string)cfg["roomCode"]);
                Assert.IsTrue(room.IsSuccess, room.ErrorMessage);
                Assert.IsTrue(room.Data.IsRoom);
                Assert.IsFalse(string.IsNullOrEmpty(room.Data.Room.Name));

                // 4. The room is listed
                var rooms = await service.GetRoomsAsync();
                Assert.IsTrue(rooms.IsSuccess, rooms.ErrorMessage);
                Assert.IsTrue(rooms.Data.Exists(r => r.RoomId == room.Data.Room.RoomId));

                // 5. Account code through the same door
                var link = await service.RedeemCodeAsync((string)cfg["accountCode"]);
                Assert.IsTrue(link.IsSuccess, link.ErrorMessage);
                Assert.IsTrue(link.Data.IsLink);
                Assert.AreEqual(LinkStatusNames.Confirmed, link.Data.Status);
                var links = await service.GetLinksAsync();
                Assert.IsTrue(links.IsSuccess && links.Data.Count >= 1);

                // 6. A well-formed code that does not exist: 400, same message for any kind
                var wrong = await service.RedeemCodeAsync("ZZZZ9999");
                Assert.AreEqual(400, wrong.StatusCode);

                // 7. Leave the room
                var left = await service.LeaveRoomAsync(room.Data.Room.RoomId);
                Assert.IsTrue(left.IsSuccess, left.ErrorMessage);
                var after = await service.GetRoomsAsync();
                Assert.IsFalse(after.Data.Exists(r => r.RoomId == room.Data.Room.RoomId));
            }
            finally
            {
                Object.Destroy(go);
            }
        });
    }
}
