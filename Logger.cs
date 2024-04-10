using System;
using ProtoBuf;
using Org; // Add this line if Org namespace is required
using Newtonsoft.Json;// Add this line if Newtonsoft namespace is required
using Org.BouncyCastle.Security;// Add this line if BouncyCastle namespace is required

namespace WidevineClient
{
    class Logger
    {
        public static void Cyan(object text, bool includeTimestamp = true)
        {
            if (includeTimestamp)
                Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");

            Console.ForegroundColor = ConsoleColor.Cyan;

            if (includeTimestamp)
                Console.Write(text);
            else
                Console.Write("                            " + text);

            Console.ResetColor();
            Console.WriteLine();
        }

        public static void Print(object text, bool newLine = true)
        {
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + text);

            if (newLine)
                Console.WriteLine();
        }
    }
}