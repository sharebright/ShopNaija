using System;
using ShopifyHandle;

namespace CipherTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadKey();
            var m = new HandleManager();
            Console.WriteLine("Insert string to encrypt");
            var input = "http://www.hm.com/gb/product/09592?article=09592-A";
            Console.WriteLine("Encrypted '{0}'", HandleManager.Encrypt(input).ToLower());

            Console.WriteLine("Enter the encrypted string");
            var encrypted = Console.ReadLine();
            Console.WriteLine("Decrypted to {0}", HandleManager.Decrypt(encrypted));

            Console.ReadKey();
        }
    }
}
