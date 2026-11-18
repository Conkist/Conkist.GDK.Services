using Conkist.GDK.Services.Conkist;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Conkist.GDK.Services.Tests
{
    /// <summary>Offline checks of the unified code door: parser and response contract (no network).</summary>
    public class ConkistCodeParserTests
    {
        [TestCase("ABCD2345", "ABCD2345")]
        [TestCase("abcd-2345", "ABCD2345")]
        [TestCase("1234 5678", "12345678")]
        [TestCase("https://app.conkist.me/connect?token=ABCD2345", "ABCD2345")]
        [TestCase("https://app.conkist.me/connect?x=1&code=abcd2345", "ABCD2345")]
        [TestCase("https://app.conkist.me/connect?token=12345678", "12345678")]
        [TestCase("https://app.conkist.me/join/ABCD2345", "ABCD2345")]
        public void Normalize_AcceptsCodesAndLinks(string input, string expected) =>
            Assert.AreEqual(expected, ConkistCodeParser.Normalize(input));

        [TestCase("ABC")]
        [TestCase("ABCDO345")] // O is not in the room alphabet
        [TestCase("ABCD1345")] // 1 is not in the room alphabet
        [TestCase("ABCD23456")]
        [TestCase("  ")]
        [TestCase(null)]
        [TestCase("https://app.conkist.me/connect")]
        [TestCase("https://app.conkist.me/connect?foo=ABCD2345")]
        public void Normalize_RefusesWhatIsNotACode(string input) =>
            Assert.IsNull(ConkistCodeParser.Normalize(input));

        [Test]
        public void Kinds_AreDisjoint()
        {
            Assert.IsTrue(ConkistCodeParser.IsAccountCode("12345678"));
            Assert.IsFalse(ConkistCodeParser.IsRoomCode("22222222"), "digits only is an account code, never a room code");
            Assert.IsTrue(ConkistCodeParser.IsRoomCode("ABCD2345"));
            Assert.IsFalse(ConkistCodeParser.IsAccountCode("ABCD2345"));
        }

        [Test]
        public void RedeemCodeResponse_Room_ReadsRoom()
        {
            const string json = "{\"kind\":\"room\",\"linkId\":null,\"status\":null,\"room\":{\"roomId\":7,\"name\":\"Turma 5A\",\"organization\":\"Inteceleri\",\"joinedAt\":\"2026-11-18T12:00:00Z\"}}";
            var r = JsonConvert.DeserializeObject<RedeemCodeResponse>(json);
            Assert.IsTrue(r.IsRoom);
            Assert.IsFalse(r.IsLink);
            Assert.AreEqual(7, r.Room.RoomId);
            Assert.AreEqual("Turma 5A", r.Room.Name);
        }

        [Test]
        public void RedeemCodeResponse_Link_ReadsLink()
        {
            var r = JsonConvert.DeserializeObject<RedeemCodeResponse>("{\"kind\":\"link\",\"linkId\":12,\"status\":\"confirmed\",\"room\":null}");
            Assert.IsTrue(r.IsLink);
            Assert.AreEqual(12, r.LinkId);
            Assert.AreEqual(LinkStatusNames.Confirmed, r.Status);
            Assert.IsNull(r.Room);
        }
    }
}
