using System;
using System.Collections.Generic;

namespace mujinplccs
{
    /// <summary>
    /// PLCMemory is a key-value store that supports locked PLC memory read write operations.
    /// </summary>
    public sealed class PLCMemory : Dictionary<string, object>
    {
        public delegate void Observer(IDictionary<string, object> modifications);
        //private DateTime lastHeartbeatTimestamp = DateTime.MinValue;
        
        public event Observer Modified = null;

        public PLCMemory() : base()
        {
        }

        /// <summary>
        /// Atomically read PLC memory.
        /// </summary>
        /// <param name="keys">An array of strings representing the named memory addresses.</param>
        /// <returns>A dictionary containing the mapping between requested memory addresses and their stored values. If a requested address does not exist in the memory, it will be omitted here.</returns>
        public Dictionary<string, object> Read(string[] keys)
        {
            var data = new Dictionary<string, object>();
            lock (this)
            {
                foreach (string key in keys)
                {
                    if (this.ContainsKey(key))
                    {
                        data[key] = this[key];
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Atomically write PLC memory.
        /// </summary>
        /// <param name="data">A dictionary containing the mapping between named memory addresses and their desired values.</param>
        public void Write(IDictionary<string, object> data)
        {
            var modifications = new Dictionary<string, object>();

            lock (this)
            {
                foreach (var pair in data)
                {
                    if (!this.ContainsKey(pair.Key) || !this[pair.Key].Equals(pair.Value))
                    {
                        modifications[pair.Key] = pair.Value;
                    }
                    this[pair.Key] = pair.Value;
                }

            }

            // notify observers of the modifications
            if (modifications.Count > 0)
            {
                this.Modified?.Invoke(modifications);
            }
        }
    }
}
