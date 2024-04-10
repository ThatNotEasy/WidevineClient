using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;

namespace WidevineClient.Widevine
{
    [Serializable]
    public class ContentKey
    {
        [JsonProperty("key_id")]
        public byte[] KeyID { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("bytes")]
        public byte[] Bytes { get; set; }

        [NotMapped]
        [JsonProperty("permissions")]
        public List<string> Permissions
        {
            get
            {
                return PermissionsString?.Split(",").ToList() ?? new List<string>();
            }
            set
            {
                PermissionsString = string.Join(",", value);
            }
        }

        [JsonIgnore]
        public string PermissionsString { get; set; }

        public override string ToString()
        {
            return $"{BitConverter.ToString(KeyID).Replace("-", "").ToLower()}:{BitConverter.ToString(Bytes).Replace("-", "").ToLower()}";
        }
    }
}
