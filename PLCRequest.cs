using System.Collections.Generic;
using Newtonsoft.Json;

namespace mujinplccs
{
    /// <summary>
    /// A PLC request received on the ZMQ socket. Usually in JSON format.
    /// </summary>
    public sealed class PLCRequest
    {
        public const string CommandPing = "ping";
        public const string CommandRead = "read";
        public const string CommandWrite = "write";

        /// <summary>
        /// Command type, one of "ping", "read" and "write".
        /// </summary>
        [JsonProperty(PropertyName = "command")]
        public string Command { get; set; }

        /// <summary>
        /// In case of read command, the list of named addresses to read from.
        /// </summary>
        [JsonProperty(PropertyName = "keys")]
        public string[] Keys { get; set; }

        /// <summary>
        /// In case of write command, the mapping bewteen named addresses and their desired values.
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        public Dictionary<string, object> Values { get; set; }
    }
}
