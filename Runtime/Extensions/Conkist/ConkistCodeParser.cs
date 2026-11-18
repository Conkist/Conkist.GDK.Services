using System.Text;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// Reads what the player typed or what a QR reader returned and gives back a clean code.
    /// One door for both kinds of code:
    ///   account code = 8 digits (made in the Conkist App, Conectar &gt; Meu código);
    ///   room code    = 8 characters, letters and digits without 0, O, 1, I, L, always with at least one letter.
    /// The server also decides the kind; this class only cleans and checks the format so the game can fail fast.
    /// Accepts a bare code ("ABCD2345", "abcd-2345", "1234 5678") or a link
    /// ("https://app.conkist.me/connect?token=ABCD2345", also "?code=").
    /// No Unity types here: it can be tested without the Editor.
    /// </summary>
    public static class ConkistCodeParser
    {
        public const int CodeLength = 8;
        private const string RoomAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>Clean upper-case code, or null when the text is not a valid code.</summary>
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var text = input.Trim();
            if (text.Contains("://"))
            {
                text = FromLink(text);
                if (text == null) return null;
            }

            var sb = new StringBuilder(CodeLength);
            foreach (var c in text)
            {
                if (c == ' ' || c == '-') continue;
                sb.Append(char.ToUpperInvariant(c));
            }
            var code = sb.ToString();
            return IsAccountCode(code) || IsRoomCode(code) ? code : null;
        }

        /// <summary>8 digits.</summary>
        public static bool IsAccountCode(string code)
        {
            if (code == null || code.Length != CodeLength) return false;
            foreach (var c in code) if (c < '0' || c > '9') return false;
            return true;
        }

        /// <summary>8 characters from the room alphabet with at least one letter.</summary>
        public static bool IsRoomCode(string code)
        {
            if (code == null || code.Length != CodeLength) return false;
            var hasLetter = false;
            foreach (var c in code)
            {
                if (RoomAlphabet.IndexOf(c) < 0) return false;
                if (c >= 'A' && c <= 'Z') hasLetter = true;
            }
            return hasLetter;
        }

        /// <summary>Takes ?token= or ?code= from the link; without a query, the last part of the path.</summary>
        private static string FromLink(string link)
        {
            var hash = link.IndexOf('#');
            if (hash >= 0) link = link.Substring(0, hash);
            var q = link.IndexOf('?');
            if (q >= 0)
            {
                foreach (var pair in link.Substring(q + 1).Split('&'))
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0) continue;
                    var key = pair.Substring(0, eq);
                    if (key == "token" || key == "code") return pair.Substring(eq + 1);
                }
                return null;
            }
            var slash = link.TrimEnd('/').LastIndexOf('/');
            return slash >= 0 ? link.Substring(slash + 1) : null;
        }
    }
}
