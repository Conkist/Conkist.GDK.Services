namespace Conkist.Services
{
    public struct ILeaderboardEntry {

        private string _playerId;
        public string PlayerId => _playerId;
        private string _username;
        public string Username => _username;
        private int _pos;
        public int Position => _pos;
        private int _value;
        public int Value => _value;

        public ILeaderboardEntry(string playerId, string username, int position, int value)
        {
            this._username = username;
            this._pos = position;
            this._value = value;
            this._playerId = playerId;
        }
    }
}