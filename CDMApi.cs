using System;
using ProtoBuf;
using System.Collections.Generic;
using Org; // Add this line if Org namespace is required
using Newtonsoft.Json; // Add this line if Newtonsoft namespace is required
using Org.BouncyCastle.Security; // Add this line if BouncyCastle namespace is required

namespace WidevineClient.Widevine
{
    public class CDMApi
    {
        private string SessionId { get; set; }

        public byte[] GetChallenge(string initDataB64, string certDataB64, bool offline = false, bool raw = false)
        {
            SessionId = CDM.OpenSession(initDataB64, "android_generic", offline, raw);
            CDM.SetServiceCertificate(SessionId, Convert.FromBase64String(certDataB64));
            return CDM.GetLicenseRequest(SessionId);
        }

        public bool ProvideLicense(string licenseB64)
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                throw new InvalidOperationException("SessionId is not set. Call GetChallenge first.");
            }

            CDM.ProvideLicense(SessionId, Convert.FromBase64String(licenseB64));
            return true;
        }

        public List<ContentKey> GetKeys()
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                throw new InvalidOperationException("SessionId is not set. Call GetChallenge first.");
            }

            return CDM.GetKeys(SessionId);
        }
    }
}
