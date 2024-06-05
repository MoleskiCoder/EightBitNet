namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //info csym = 0, file = 3, lib = 0, line = 380, mod = 1, scope = 12, seg = 8, span = 356, sym = 61, type = 3
            public class Information : Section
            {
                public Information()
                {
                    _ = this._integer_keys.Add("csym");
                    _ = this._integer_keys.Add("file");
                    _ = this._integer_keys.Add("lib");
                    _ = this._integer_keys.Add("line");
                    _ = this._integer_keys.Add("mod");
                    _ = this._integer_keys.Add("scope");
                    _ = this._integer_keys.Add("seg");
                    _ = this._integer_keys.Add("span");
                    _ = this._integer_keys.Add("sym");
                    _ = this._integer_keys.Add("type");
                }

                public int Count(string key) => this.TakeInteger(key);
            }
        }
    }
}