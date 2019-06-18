using ExtensionMethods;
using System;

namespace Utils
{
    class Base32Int
    {

        public static bool TryParse(string s, out int n)
        {
            n = 0;
            if (s == string.Empty || s == null) return false;

            byte[] b = s.ToUpper().Encode();
            Array.Reverse(b);

            for (int i = 0; i < b.Length; i++)
            {
                int mult = (int)Math.Pow(32, i);
                int a = b[i] - 48;
                if (a > 9)
                {
                    a -= 7;
                }
                if (a > 31)
                {
                    return false;
                }
                n += a * mult;
            }
            return true;
        }
    }
}
