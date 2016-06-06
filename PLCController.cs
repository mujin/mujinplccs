using System;
using System.Collections.Generic;

namespace mujinplccs
{
    public sealed class PLCController
    {
        private PLCMemory memory = null;

        public PLCController() : this(new PLCMemory())
        {
        }

        public PLCController(PLCMemory memory)
        {
            this.memory = memory;
        }

        public PLCMemory Memory
        {
            get { return this.memory; }
            set { lock (this) { this.memory = value; } }
        }

        public PLCResponse Process(PLCRequest request)
        {
            switch (request.Command)
            {
                case PLCRequest.CommandPing:
                    return new PLCResponse { };

                case PLCRequest.CommandRead:
                    Dictionary<string, object> values = null;
                    if (request.Keys != null && request.Keys.Length > 0)
                    {
                        lock (this)
                        {
                            values = this.memory.Read(request.Keys);
                        }
                    }
                    return new PLCResponse { Values = values };

                case PLCRequest.CommandWrite:
                    if (request.Values != null && request.Values.Count > 0)
                    {
                        lock (this)
                        {
                            this.memory.Write(request.Values);
                        }
                    }
                    return new PLCResponse { };

                default:
                    throw new PLCInvalidCommandException(String.Format("Command {0} is unknown.", request.Command));
            }
        }
    }
}
