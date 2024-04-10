using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace WidevineClient.Crypto
{
    public class CryptoUtils
    {
        public static byte[] GetHMACSHA256Digest(byte[] data, byte[] key)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }

        public static byte[] GetCMACDigest(byte[] data, byte[] key)
        {
            IBlockCipher cipher = new AesEngine();
            IMac mac = new CMac(cipher, 128);

            var keyParam = new KeyParameter(key);

            mac.Init(keyParam);

            mac.BlockUpdate(data, 0, data.Length);

            byte[] outBytes = new byte[mac.GetMacSize()];

            mac.DoFinal(outBytes, 0);

            return outBytes;
        }
    }
}
