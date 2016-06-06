using System;

namespace mujinplccs
{
    /// <summary>
    /// Base class for all PLC exceptions.
    /// </summary>
    public class PLCException : Exception
    {
        private string code;

        public PLCException(string code, string message) : base(message)
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
    public sealed class PLCInvalidCommandException : PLCException
    {
        public PLCInvalidCommandException(string message) : base("invalid_command", message)
        {
        }
    }
}
