using System.Collections.Generic;

namespace Sezame
{
    public class SezameResult
    {
        private Dictionary<int, string> _data;

        public SezameResult(Dictionary<int, string> data)
        {
            this._data = data;
        }

        public string GetParameter(SezameResultKey key)
        {
            string result;
            this._data.TryGetValue((int)key, out result);
            return result;
        }
    }
}
