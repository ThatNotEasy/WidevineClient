using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using System;

namespace WidevineClient
{
    class Program
    {
        static void Main(string[] args)
        {
            RunTests();
        }

        static void RunTests()
        {
            Tests.Test();
            Console.WriteLine();
        }
    }
}