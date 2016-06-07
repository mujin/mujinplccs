using System;
using System.Collections.Generic;

namespace mujinplccs
{
    public sealed class PLCService
    {
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
