using System.Collections.Generic;
using Newtonsoft.Json;

namespace mujinplccs
{
    public sealed class PLCRequest
    {
        public const string CommandPing = "ping";
        public const string CommandRead = "read";
        public const string CommandWrite = "write";

        [JsonProperty(PropertyName = "command")]
        public string Command { get; set; }

        [JsonProperty(PropertyName = "keys")]
        public string[] Keys { get; set; }

        [JsonProperty(PropertyName = "values")]
        public Dictionary<string, object> Values { get; set; }
    }
}
