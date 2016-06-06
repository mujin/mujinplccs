using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mujinplccs
{
    /// <summary>
    /// PLCMemory is a key-value store that supports locked PLC memory read write operations.
    /// </summary>
    public sealed class PLCMemory: Dictionary<string, object>
    {
        public PLCMemory(): base()
        {
        }

        /// <summary>
        /// Atomically read PLC memory.
        /// </summary>
        /// <param name="keys">An array of strings representing the named memory addresses.</param>
        /// <returns>A dictionary containing the mapping between requested memory addresses and their stored values. If a requested address does not exist in the memory, it will be omitted here.</returns>
        public Dictionary<string, object> Read(string[] keys)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            lock (this) {
                foreach (string key in keys) {
                    if (this.ContainsKey(key)) {
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
        public void Write(Dictionary<string, object> data)
        {
            lock (this) {
                foreach (KeyValuePair<string, object> pair in data) {
                    this[pair.Key] = pair.Value;
                }
            }
        }
    }
}
