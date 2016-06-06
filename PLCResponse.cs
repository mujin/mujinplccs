using System.Collections.Generic;
using Newtonsoft.Json;

namespace mujinplccs
{
    /// <summary>
    /// A PLC response to be sent on the ZMQ socket. Can be converted to JSON format.
    /// </summary>
    public sealed class PLCResponse
    {
        public sealed class PLCError
        {
            /// <summary>
            /// Type of the error.
            /// </summary>
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            /// <summary>
            /// Description of the error.
            /// </summary>
            [JsonProperty(PropertyName = "desc")]
            public string Desc { get; set; }
        }

        /// <summary>
        /// Error field will be populated if an error has occured
        /// </summary>
        [JsonProperty(PropertyName = "error")]
        public PLCError Error { get; set; }

        public bool ShouldSerializeError()
        {
            return this.Error != null;
        }

        /// <summary>
        /// In case of read request, this field will contain the returned data as a mapping between named addresses and their values.
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        public Dictionary<string, object> Values { get; set; }

        public bool ShouldSerializeValues()
        {
            return this.Values != null;
        }
    }
}
