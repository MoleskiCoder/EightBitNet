namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //version major = 2, minor = 0
            public class Version : Section
            {
                public int Major => this.TakeInteger("major");

                public int Minor => this.TakeInteger("minor");

                public Version()
                {
                    _ = this._integer_keys.Add("major");
                    _ = this._integer_keys.Add("minor");
                }
            }
        }
    }
}