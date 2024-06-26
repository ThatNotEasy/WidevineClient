﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using WidevineClient.Crypto;

namespace WidevineClient.Widevine
{
    public class CDM
    {
        static Dictionary<string, CDMDevice> Devices { get; } = new Dictionary<string, CDMDevice>()
        {
            ["android_generic"] = new CDMDevice("android_generic", null, null, null)
        };
        static Dictionary<string, Session> Sessions { get; set; } = new Dictionary<string, Session>();

        static byte[] CheckPSSH(string psshB64)
        {
            byte[] systemID = new byte[] { 237, 239, 139, 169, 121, 214, 74, 206, 163, 200, 39, 220, 213, 29, 33, 237 };

            if (psshB64.Length % 4 != 0)
            {
                psshB64 = psshB64.PadRight(psshB64.Length + (4 - (psshB64.Length % 4)), '=');
            }

            byte[] pssh = Convert.FromBase64String(psshB64);

            if (pssh.Length < 30)
                return pssh;

            if (!pssh[12..28].SequenceEqual(systemID))
            {
                List<byte> newPssh = new List<byte>() { 0, 0, 0 };
                newPssh.Add((byte)(32 + pssh.Length));
                newPssh.AddRange(Encoding.UTF8.GetBytes("pssh"));
                newPssh.AddRange(new byte[] { 0, 0, 0, 0 });
                newPssh.AddRange(systemID);
                newPssh.AddRange(new byte[] { 0, 0, 0, 0 });
                newPssh[31] = (byte)(pssh.Length);
                newPssh.AddRange(pssh);

                return newPssh.ToArray();
            }
            else
            {
                return pssh;
            }
        }

        public static string OpenSession(string initDataB64, string deviceName, bool offline = false, bool raw = false)
        {
            byte[] initData = CheckPSSH(initDataB64);

            var device = Devices[deviceName];

            byte[] sessionId = new byte[16];

            if (device.IsAndroid)
            {
                string randHex = "";

                Random rand = new Random();
                string choice = "ABCDEF0123456789";
                for (int i = 0; i < 16; i++)
                    randHex += choice[rand.Next(16)];

                string counter = "01";
                string rest = "00000000000000";
                sessionId = Encoding.ASCII.GetBytes(randHex + counter + rest);
            }
            else
            {
                Random rand = new Random();
                rand.NextBytes(sessionId);
            }

            Session session;
            dynamic parsedInitData = ParseInitData(initData);

            if (parsedInitData != null)
            {
                session = new Session(sessionId, parsedInitData, device, offline);
            }
            else if (raw)
            {
                session = new Session(sessionId, initData, device, offline);
            }
            else
            {
                return null;
            }

            Sessions.Add(Utils.BytesToHex(sessionId), session);

            return Utils.BytesToHex(sessionId);
        }

        static WidevineCencHeader ParseInitData(byte[] initData)
        {
            WidevineCencHeader cencHeader;

            try
            {
                cencHeader = Serializer.Deserialize<WidevineCencHeader>(new MemoryStream(initData[32..]));
            }
            catch
            {
                try
                {
                    //needed for HBO Max

                    PSSHBox psshBox = PSSHBox.FromByteArray(initData);
                    cencHeader = Serializer.Deserialize<WidevineCencHeader>(new MemoryStream(psshBox.Data));
                }
                catch
                {
                    // Logger.Verbose("Unable to parse, unsupported init data format");
                    return null;
                }
            }

            return cencHeader;
        }

        public static bool CloseSession(string sessionId)
        {
            if (Sessions.ContainsKey(sessionId))
            {
                Sessions.Remove(sessionId);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool SetServiceCertificate(string sessionId, byte[] certData)
        {
            if (!Sessions.ContainsKey(sessionId))
            {
                return false;
            }

            SignedMessage signedMessage = new SignedMessage();

            try
            {
                signedMessage = Serializer.Deserialize<SignedMessage>(new MemoryStream(certData));
            }
            catch
            {
                // Logger.Warn("Failed to parse cert as SignedMessage");
            }

            SignedDeviceCertificate serviceCertificate;
            try
            {
                try
                {
                    serviceCertificate = Serializer.Deserialize<SignedDeviceCertificate>(new MemoryStream(signedMessage.Msg));
                }
                catch
                {
                    serviceCertificate = Serializer.Deserialize<SignedDeviceCertificate>(new MemoryStream(certData));
                }
            }
            catch
            {
                return false;
            }

            Sessions[sessionId].ServiceCertificate = serviceCertificate;
            Sessions[sessionId].PrivacyMode = true;

            return true;
        }

        public static byte[] GetLicenseRequest(string sessionId)
        {
            if (!Sessions.ContainsKey(sessionId))
            {
                return null;
            }

            var session = Sessions[sessionId];

            dynamic licenseRequest;

            if (session.InitData is WidevineCencHeader)
            {
                licenseRequest = new SignedLicenseRequest
                {
                    Type = SignedLicenseRequest.MessageType.LicenseRequest,
                    Msg = new LicenseRequest
                    {
                        Type = LicenseRequest.RequestType.New,
                        KeyControlNonce = 1093602366,
                        ProtocolVersion = ProtocolVersion.Current,
                        RequestTime = uint.Parse((DateTime.Now - DateTime.UnixEpoch).TotalSeconds.ToString().Split(".")[0]),
                        ContentId = new LicenseRequest.ContentIdentification
                        {
                            CencId = new LicenseRequest.ContentIdentification.Cenc
                            {
                                LicenseType = session.Offline ? LicenseType.Offline : LicenseType.Default,
                                RequestId = session.SessionId,
                                Pssh = session.InitData
                            }
                        }
                    }
                };
            }
            else
            {
                licenseRequest = new SignedLicenseRequestRaw
                {
                    Type = SignedLicenseRequestRaw.MessageType.LicenseRequest,
                    Msg = new LicenseRequestRaw
                    {
                        Type = LicenseRequestRaw.RequestType.New,
                        KeyControlNonce = 1093602366,
                        ProtocolVersion = ProtocolVersion.Current,
                        RequestTime = uint.Parse((DateTime.Now - DateTime.UnixEpoch).TotalSeconds.ToString().Split(".")[0]),
                        ContentId = new LicenseRequestRaw.ContentIdentification
                        {
                            CencId = new LicenseRequestRaw.ContentIdentification.Cenc
                            {
                                LicenseType = session.Offline ? LicenseType.Offline : LicenseType.Default,
                                RequestId = session.SessionId,
                                Pssh = session.InitData
                            }
                        }
                    }
                };
            }

            if (session.PrivacyMode)
            {
                EncryptedClientIdentification encryptedClientIdProto = new EncryptedClientIdentification();

                using var memoryStream = new MemoryStream();
                Serializer.Serialize(memoryStream, session.Device.ClientID);
                byte[] data = Padding.AddPKCS7Padding(memoryStream.ToArray(), 16);

                using AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider
                {
                    BlockSize = 128,
                    Padding = PaddingMode.PKCS7,
                    Mode = CipherMode.CBC
                };
                aesProvider.GenerateKey();
                aesProvider.GenerateIV();

                using MemoryStream mstream = new MemoryStream();
                using CryptoStream cryptoStream = new CryptoStream(mstream, aesProvider.CreateEncryptor(aesProvider.Key, aesProvider.IV), CryptoStreamMode.Write);
                cryptoStream.Write(data, 0, data.Length);
                encryptedClientIdProto.EncryptedClientId = mstream.ToArray();

                using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSA.ImportRSAPublicKey(session.ServiceCertificate.DeviceCertificate.PublicKey, out int bytesRead);
                encryptedClientIdProto.EncryptedPrivacyKey = RSA.Encrypt(aesProvider.Key, RSAEncryptionPadding.OaepSHA1);
                encryptedClientIdProto.EncryptedClientIdIv = aesProvider.IV;
                encryptedClientIdProto.ServiceId = Encoding.UTF8.GetString(session.ServiceCertificate.DeviceCertificate.ServiceId);
                encryptedClientIdProto.ServiceCertificateSerialNumber = session.ServiceCertificate.DeviceCertificate.SerialNumber;

                licenseRequest.Msg.EncryptedClientId = encryptedClientIdProto;
            }
            else
            {
                licenseRequest.Msg.ClientId = session.Device.ClientID;
            }

            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, licenseRequest.Msg);
                byte[] data = memoryStream.ToArray();
                session.LicenseRequest = data;

                licenseRequest.Signature = session.Device.Sign(data);
            }

            byte[] requestBytes;
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, licenseRequest);
                requestBytes = memoryStream.ToArray();
            }

            Sessions[sessionId] = session;

            return requestBytes;
        }

        public static void ProvideLicense(string sessionId, byte[] license)
        {
            if (!Sessions.ContainsKey(sessionId))
            {
                throw new Exception("Session ID doesn't exist");
            }

            var session = Sessions[sessionId];

            if (session.LicenseRequest == null)
            {
                throw new Exception("Generate a license request first");
            }

            SignedLicense signedLicense;
            try
            {
                signedLicense = Serializer.Deserialize<SignedLicense>(new MemoryStream(license));
            }
            catch
            {
                throw new Exception("Unable to parse license");
            }

            session.License = signedLicense;

            try
            {
                var sessionKey = session.Device.Decrypt(session.License.SessionKey);

                if (sessionKey.Length != 16)
                {
                    throw new Exception("Unable to decrypt session key");
                }

                session.SessionKey = sessionKey;
            }
            catch
            {
                throw new Exception("Unable to decrypt session key");
            }

            session.DerivedKeys = DeriveKeys(session.LicenseRequest, session.SessionKey);

            byte[] licenseBytes;
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, signedLicense.Msg);
                licenseBytes = memoryStream.ToArray();
            }
            byte[] hmacHash = CryptoUtils.GetHMACSHA256Digest(licenseBytes, session.DerivedKeys.Auth1);

            if (!hmacHash.SequenceEqual(signedLicense.Signature))
            {
                throw new Exception("License signature mismatch");
            }

            foreach (License.KeyContainer key in signedLicense.Msg.Keys)
            {
                string type = key.Type.ToString();

                if (type == "Signing")
                    continue;

                byte[] keyId;
                byte[] encryptedKey = key.Key;
                byte[] iv = key.Iv;
                keyId = key.Id;
                if (keyId == null)
                {
                    keyId = Encoding.ASCII.GetBytes(key.Type.ToString());
                }

                byte[] decryptedKey;

                using MemoryStream mstream = new MemoryStream();
                using AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                };
                using CryptoStream cryptoStream = new CryptoStream(mstream, aesProvider.CreateDecryptor(session.DerivedKeys.Enc, iv), CryptoStreamMode.Write);
                cryptoStream.Write(encryptedKey, 0, encryptedKey.Length);
                decryptedKey = mstream.ToArray();

                List<string> permissions = new List<string>();
                if (type == "OperatorSession")
                {
                    foreach (PropertyInfo perm in key._OperatorSessionKeyPermissions.GetType().GetProperties())
                    {
                        if ((uint)perm.GetValue(key._OperatorSessionKeyPermissions) == 1)
                        {
                            permissions.Add(perm.Name);
                        }
                    }
                }
                session.ContentKeys.Add(new ContentKey
                {
                    KeyID = keyId,
                    Type = type,
                    Bytes = decryptedKey,
                    Permissions = permissions
                });
            }

            Sessions[sessionId] = session;
        }

        public static DerivedKeys DeriveKeys(byte[] message, byte[] key)
        {
            byte[] encKeyBase = Encoding.UTF8.GetBytes("ENCRYPTION").Concat(new byte[] { 0x0, }).Concat(message).Concat(new byte[] { 0x0, 0x0, 0x0, 0x80 }).ToArray();
            byte[] authKeyBase = Encoding.UTF8.GetBytes("AUTHENTICATION").Concat(new byte[] { 0x0, }).Concat(message).Concat(new byte[] { 0x0, 0x0, 0x2, 0x0 }).ToArray();

            byte[] encKey = new byte[] { 0x01 }.Concat(encKeyBase).ToArray();
            byte[] authKey1 = new byte[] { 0x01 }.Concat(authKeyBase).ToArray();
            byte[] authKey2 = new byte[] { 0x02 }.Concat(authKeyBase).ToArray();
            byte[] authKey3 = new byte[] { 0x03 }.Concat(authKeyBase).ToArray();
            byte[] authKey4 = new byte[] { 0x04 }.Concat(authKeyBase).ToArray();

            byte[] encCmacKey = CryptoUtils.GetCMACDigest(encKey, key);
            byte[] authCmacKey1 = CryptoUtils.GetCMACDigest(authKey1, key);
            byte[] authCmacKey2 = CryptoUtils.GetCMACDigest(authKey2, key);
            byte[] authCmacKey3 = CryptoUtils.GetCMACDigest(authKey3, key);
            byte[] authCmacKey4 = CryptoUtils.GetCMACDigest(authKey4, key);

            byte[] authCmacCombined1 = authCmacKey1.Concat(authCmacKey2).ToArray();
            byte[] authCmacCombined2 = authCmacKey3.Concat(authCmacKey4).ToArray();

            return new DerivedKeys
            {
                Auth1 = authCmacCombined1,
                Auth2 = authCmacCombined2,
                Enc = encCmacKey
            };
        }

        public static List<ContentKey> GetKeys(string sessionId)
        {
            if (Sessions.ContainsKey(sessionId))
                return Sessions[sessionId].ContentKeys;
            else
            {
                throw new Exception("Session not found");
            }
        }
    }
}
