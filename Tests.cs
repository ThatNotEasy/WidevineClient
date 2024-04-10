using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using WidevineClient.Widevine;
using static WidevineClient.HttpUtil;
using static WidevineClient.Utils;

namespace WidevineClient
{
    class Tests
    {
        // Test 1: Shaka Player Demo
        public static void Test()
        {
            Console.WriteLine("Enter the license URL:");
            string licenseUrl = Console.ReadLine();

            Console.WriteLine("Enter the PSSH:");
            string pssh = Console.ReadLine();

            Logger.Cyan("Get cert...");
            var resp1 = PostData(licenseUrl, null, new byte[] { 0x08, 0x04 });
            var certDataB64 = Convert.ToBase64String(resp1);
            Logger.Cyan("Get challenge...");
            var cdm = new CDMApi();
            var challenge = cdm.GetChallenge(pssh, certDataB64, false, false);
            Logger.Cyan("Get license...");
            var resp2 = PostData(licenseUrl, null, challenge);
            var licenseB64 = Convert.ToBase64String(resp2);
            cdm.ProvideLicense(licenseB64);
            Logger.Cyan("Get keys...");
            List<ContentKey> keys = cdm.GetKeys();
            foreach (var key in keys)
            {
                Logger.Print(key);
            }

            Console.ReadLine(); // Wait for user input before closing the console window
        }
    }
}
