using System.Collections.Generic;
using Newtonsoft.Json;

namespace mujinplccs
{
    public sealed class PLCResponse
    {
        public sealed class PLCError
        {
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "desc")]
            public string Desc { get; set; }
        }

        [JsonProperty(PropertyName = "error")]
        public PLCError Error { get; set; }

        [JsonProperty(PropertyName = "values")]
        public Dictionary<string, object> Values { get; set; }

        public bool ShouldSerializeError()
        {
            return this.Error != null;
        }

        public bool ShouldSerializeValues()
        {
            return this.Values != null;
        }
    }
}
