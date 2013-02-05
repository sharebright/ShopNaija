using System;
using System.Text;
using Rest4Net.BitLy;
using System.Linq;

namespace ShopifyHandle
{
    public class HandleManager
    {
        public static string Encrypt(string input)
        {
            var bitly = new BitLyProvider("monkieboy", "R_b36be051b416a3797b8f3c67526b872f");
            var shortened = bitly.Shorten(input);
            input = shortened.Data.Url;
            var bareCode = input.Replace("http://bit.ly/", string.Empty);

            string encodedBareCode = Encode(bareCode);

            return encodedBareCode;
        }

        private static string Encode(string bareCode)
        {
            var result = new StringBuilder();
            foreach (var c in bareCode)
            {
                if (c >= 'A' && c <= 'Z')
                {
                    result.Append("xpz");
                }
                result.Append(c);
            }
            return result.ToString().ToLower();
        }

        private static string Decode(string input)
        {
            var result = new StringBuilder();
            var splits = input.Split(new[] {"xpz"}, StringSplitOptions.None);
            result.Append(splits.First());
            foreach (var s in splits.Skip(1))
            {
                result.Append((char) (s.First() - 32));
                result.Append(s.Substring(1));
            }
            return result.ToString();
        }

        public static string Decrypt(string input)
        {
            var decodedInput = Decode(input);
            return string.Concat(new[] { "http://bit.ly/", decodedInput });
        }
    }
}
