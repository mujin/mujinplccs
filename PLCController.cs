using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace mujinplccs
{
    public sealed class PLCController
    {
        private PLCMemory memory = null;
        private DateTime timestamp = DateTime.Now;
        private Dictionary<string, object> state = new Dictionary<string, object>();
        private BlockingCollection<KeyValuePair<string, object>> queue = new BlockingCollection<KeyValuePair<string, object>>(
            new ConcurrentQueue<KeyValuePair<string, object>>()
        );

        public PLCController(PLCMemory memory)
        {
            this.memory = memory;

            // register observer
            this.memory.Modified += new PLCMemory.Observer(this._Enqueue);

            // copy memory
            lock (memory)
            {
                foreach (var pair in memory)
                {
                    this.state[pair.Key] = pair.Value;
                }
            }
        }

        private void _Enqueue(IDictionary<string, object> modifications)
        {
            lock (this)
            {
                // timestamp of last activity
                this.timestamp = DateTime.Now;
            }

            foreach(var pair in modifications)
            {
                queue.Add(pair);
                // Console.WriteLine("memory modified: {0} = {1}", pair.Key, pair.Value);
            }
        }

        private void _DequeueAll()
        {
            KeyValuePair<string, object> pair;
            while (queue.TryTake(out pair))
            {
                this.state[pair.Key] = pair.Value;
            }
        }

        private KeyValuePair<string, object> _Dequeue(TimeSpan?timeout = null)
        {
            KeyValuePair<string, object> pair;
            while (true)
            {
                Stopwatch stopwatch = new Stopwatch();
                if (queue.TryTake(out pair, TimeSpan.FromMilliseconds(50)))
                {
                    // successfully took
                    break;
                }

                // subtract timeout and see if we are timed out
                timeout = timeout?.Subtract(stopwatch.Elapsed);
                if (timeout.HasValue && timeout <= TimeSpan.Zero)
                {   
                    throw new TimeoutException();
                }
            }
            this.state[pair.Key] = pair.Value;
            return pair;
        }

        public void Sync()
        {
            this._DequeueAll();
        }

        public object WaitFor(string key, object value, TimeSpan? timeout = null)
        {
            return this.WaitFor(new Dictionary<string, object>()
            {
                { key, value },
            }, timeout).Value;
        }

        public KeyValuePair<string, object> WaitFor(IDictionary<string, object> signals, TimeSpan? timeout = null)
        {
            while (true)
            {
                Stopwatch stopwatch = new Stopwatch();
                KeyValuePair<string, object> pair = this._Dequeue(timeout);
                if (signals.ContainsKey(pair.Key))
                {
                    if (signals[pair.Key] == null || pair.Value.Equals(signals[pair.Key]))
                    {
                        return pair;
                    }
                }
                timeout = timeout?.Subtract(stopwatch.Elapsed);
            }
        }

        public void WaitUntil(string key, object value, TimeSpan? timeout = null)
        {
            this.WaitUntil(new Dictionary<string, object>()
            {
                { key, value },
            }, null, timeout);
        }

        public void WaitUntil(IDictionary<string, object> expectations, IDictionary<string, object> exceptions = null, TimeSpan? timeout = null)
        {
            // combine all signals
            var all = new Dictionary<string, object>(expectations);
            if (exceptions != null)
            {
                foreach (var pair in exceptions)
                {
                    all[pair.Key] = pair.Value;
                }
            }

            // always clear the queue first
            this._DequeueAll();

            while (true)
            {
                // check if any exceptions is already met
                foreach (var pair in exceptions)
                {
                    if (this.state.ContainsKey(pair.Key) && this.state[pair.Key].Equals(pair.Value))
                    {
                        return;
                    }
                }
                
                // check if all expectations are already met
                bool met = true;
                foreach (var pair in expectations)
                {
                    if (!this.state.ContainsKey(pair.Key) || !this.state[pair.Key].Equals(pair.Value))
                    {
                        met = false;
                        break;
                    }
                }
                if (met)
                {
                    return;
                }

                // wait for it to change
                Stopwatch stopwatch = new Stopwatch();
                this.WaitFor(all, timeout);
                timeout = timeout?.Subtract(stopwatch.Elapsed);
            }
        }

        public object Get(string key, object defaultValue = null)
        {
            if (this.state.ContainsKey(key))
            {
                return this.state[key];
            }
            return defaultValue;
        }

        public IDictionary<string, object> Get(string[] keys)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            foreach (var key in keys)
            {
                if (this.state.ContainsKey(key))
                {
                    values[key] = this.state[key];
                }
            }
            return values;
        }

        public void Set(string key, object value)
        {
            this.memory.Write(new Dictionary<string, object>()
            {
                { key, value },
            });
        }

        public void Set(IDictionary<string, object> values)
        {
            this.memory.Write(values);
        }
    }
}
