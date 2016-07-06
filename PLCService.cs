using System;
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
        [JsonProperty(PropertyName = "keyvalues")]
        public Dictionary<string, object> Values { get; set; }
    }

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
        [JsonProperty(PropertyName = "keyvalues")]
        public Dictionary<string, object> Values { get; set; }

        public bool ShouldSerializeValues()
        {
            return this.Values != null;
        }
    }

    public sealed class PLCService
    {
        public class PLCServiceException : Exception
        {
            private string code;

            public PLCServiceException(string code, string message) : base(message)
            {
                this.code = code;
            }

            public string Code
            {
                get { return this.code; }
            }
        }

        /// <summary>
        /// Invalid command exception. Thrown when the received command is missing or unrecognized.
        /// </summary>
        public sealed class PLCInvalidCommandException : PLCServiceException
        {
            public PLCInvalidCommandException(string message) : base("invalid_command", message)
            {
            }
        }

        private PLCMemory memory = null;

        /// <summary>
        /// Controling logic for PLC. Handles requests after they are parsed.
        /// </summary>
        public PLCService() : this(new PLCMemory())
        {
        }

        /// <summary>
        /// Controling logic for PLC. Handles requests after they are parsed.
        /// </summary>
        /// <param name="memory">A custom PLC memory instance.</param>
        public PLCService(PLCMemory memory)
        {
            this.memory = memory;
        }

        /// <summary>
        /// Get or set the underlying PLC memory.
        /// </summary>
        public PLCMemory Memory
        {
            get { return this.memory; }
        }

        /// <summary>
        /// Process a PLC request and return a PLC response.
        /// </summary>
        /// <param name="request">Request received from network.</param>
        /// <returns>Response to be sent back to the client.</returns>
        public PLCResponse Handle(PLCRequest request)
        {
            switch (request.Command)
            {
                case PLCRequest.CommandPing:
                    return new PLCResponse { };

                case PLCRequest.CommandRead:
                    Dictionary<string, object> values = null;
                    if (request.Keys != null && request.Keys.Length > 0)
                    {
                        values = this.memory.Read(request.Keys);
                    }
                    return new PLCResponse { Values = values };

                case PLCRequest.CommandWrite:
                    if (request.Values != null && request.Values.Count > 0)
                    {
                        this.memory.Write(request.Values);
                    }
                    return new PLCResponse { };

                default:
                    throw new PLCInvalidCommandException(String.Format("Command {0} is unknown.", request.Command));
            }
        }
    }
}
