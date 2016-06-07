using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace mujinplccs
{
    public sealed class PLCController
    {
        private PLCMemory memory = null;
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

        private void _Enqueue(Dictionary<string, object> modifications)
        {
            foreach(var pair in modifications)
            {
                queue.Add(pair);
                Console.WriteLine("memory modified: {0} = {1}", pair.Key, pair.Value);
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
            if (timeout != null)
            {
                Stopwatch stopwatch = new Stopwatch();
                if (!queue.TryTake(out pair, timeout.Value))
                {
                    throw new TimeoutException();
                }
                timeout?.Subtract(stopwatch.Elapsed);
            }
            else
            {
                pair = queue.Take();
            }
            this.state[pair.Key] = pair.Value;
            return pair;
        }

        public void Clear()
        {
            this._DequeueAll();
        }

        public object WaitFor(string key, object value, TimeSpan? timeout = null)
        {
            var values = new Dictionary<string, object>(1);
            values[key] = value;
            return this.WaitFor(values, timeout).Value;
        }

        public KeyValuePair<string, object> WaitFor(Dictionary<string, object> expectations, TimeSpan? timeout = null)
        {
            while (true)
            {
                KeyValuePair<string, object> pair = this._Dequeue(timeout);
                if (expectations.ContainsKey(pair.Key))
                {
                    if (pair.Value.Equals(expectations[pair.Key]))
                    {
                        return pair;
                    }
                }
            }
        }

        public object WaitUntil(string key, object value, TimeSpan? timeout = null)
        {
            var values = new Dictionary<string, object>(1);
            values[key] = value;
            return this.WaitUntil(values, timeout).Value;
        }

        public KeyValuePair<string, object> WaitUntil(Dictionary<string, object> expectations, TimeSpan? timeout = null)
        {
            // always clear the queue first
            this._DequeueAll();

            foreach (var pair in expectations)
            {
                if (this.state.ContainsKey(pair.Key) && this.state[pair.Key].Equals(pair.Value))
                {
                    return pair;
                }
            }

            return this.WaitFor(expectations, timeout);
        }
    }
}
