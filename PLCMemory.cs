using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mujinplccs
{
    public sealed class PLCMemory: Dictionary<string, object>
    {
        public PLCMemory(): base()
        {
        }

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
