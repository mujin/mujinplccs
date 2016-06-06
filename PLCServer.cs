using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZMQ;
using Newtonsoft.Json;

namespace mujinplccs
{
    /// <summary>
    /// A ZMQ server that hosts the PLC controller.
    /// </summary>
    public sealed class PLCServer
    {
        private PLCController controller = null;
        private string addr = "";
        private bool isok = true;
        private Thread thread = null;
        private JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        /// <summary>
        /// Creates the ZMQ server.
        /// </summary>
        /// <param name="addr">Endpoint to listen on. For example, "tcp://*:5555".</param>
        public PLCServer(string addr) : this(new PLCController(), addr)
        {
        }

        /// <summary>
        /// Creates the ZMQ server.
        /// </summary>
        /// <param name="controller">A custom instance of PLC controller</param>
        /// <param name="addr">Endpoint to listen on. For example, "tcp://*:5555".</param>
        public PLCServer(PLCController controller, string addr)
        {
            this.addr = addr;
            this.controller = controller;
        }

        /// <summary>
        /// Start the PLC server on a background thread.
        /// </summary>
        public void Start()
        {
            this.Stop();
            this.isok = true;
            this.thread = new Thread(new ThreadStart(this._ServerThread));
            this.thread.Start();
        }

        /// <summary>
        /// Stop the PLC server. Will block until the background thread teminates.
        /// </summary>
        public void Stop()
        {
            this.isok = false;
            if (this.thread != null)
            {
                this.thread.Join();
                this.thread = null;
            }
        }

        /// <summary>
        /// Whether ZMQ server is currently running.
        /// </summary>
        public bool IsRunning
        {
            get { return this.thread != null && this.thread.IsAlive; }
        }

        /// <summary>
        /// Listening endpoint of the ZMQ server.
        /// </summary>
        public string Address
        {
            get { return this.addr; }
        }

        /// <summary>
        /// Underlying controller instance.
        /// </summary>
        public PLCController Controller
        {
            get { return this.controller; }
            set { lock(this) { this.controller = value; } }
        }

        private void _ServerThread()
        {
            Console.WriteLine("Thread started.");
            while (this.isok)
            {
                try
                {
                    using (Context context = new Context())
                    using (Socket socket = context.Socket(SocketType.REP))
                    {
                        // bind to address
                        socket.Bind(this.addr);

                        // prepare poll item
                        PollItem[] pollItems = new PollItem[] {
                            socket.CreatePollItem(IOMultiPlex.POLLIN)
                        };

                        // loop until told to stop or when exception is thrown
                        while (this.isok)
                        {
                            // wait for 50 ms
                            if (Context.Poller(pollItems, 50000) > 0) {
                                this._RecvAndSend(socket);
                            }
                        }
                    }
                }
                catch (ZMQ.Exception e)
                {
                    // recover from zmq error by re-creating socket
                    // TODO: log here
                    Console.WriteLine("Encountered ZMQ error: {0}, will re-create socket.", e);
                }
            }
        }

        private void _RecvAndSend(Socket socket)
        {
            byte[] rawdata = socket.Recv(SendRecvOpt.NOBLOCK);
            string jsonstring = System.Text.Encoding.UTF8.GetString(rawdata);
            PLCResponse response = null;
            try
            {
                PLCRequest request = JsonConvert.DeserializeObject<PLCRequest>(jsonstring, jsonSettings);
                lock (this) {
                    response = this.controller.Process(request);
                }
            }
            catch (PLCException e)
            {
                // reply with an error
                response = new PLCResponse {
                    Error = new PLCResponse.PLCError { Type = e.Code, Desc = e.Message },
                };
            }
            catch (System.Exception e)
            {
                // reply with an error
                response = new PLCResponse {
                    Error = new PLCResponse.PLCError { Type = "unkown", Desc = e.Message },
                };
            }
            
            // serialize to json and send
            if (response == null) {
                response = new PLCResponse();
            }
            string serialized = JsonConvert.SerializeObject(response, Formatting.None, jsonSettings);
            socket.Send(System.Text.Encoding.UTF8.GetBytes(serialized));
        }
    }
}
