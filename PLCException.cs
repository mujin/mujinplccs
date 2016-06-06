using System;

namespace mujinplccs
{
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

    public sealed class PLCInvalidCommandException : PLCException
    {
        public PLCInvalidCommandException(string message) : base("invalid_command", message)
        {
        }
    }
}
