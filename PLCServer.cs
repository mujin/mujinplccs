using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace mujinplccs
{
    /// <summary>
    /// A ZMQ server that hosts the PLC controller.
    /// </summary>
    public sealed class PLCServer
    {
        private PLCService service = null;
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
        public PLCServer(string addr) : this(new PLCService(), addr)
        {
        }

        /// <summary>
        /// Creates the ZMQ server.
        /// </summary>
        /// <param name="memory">A custom instance of PLC memory</param>
        /// <param name="addr">Endpoint to listen on. For example, "tcp://*:5555".</param>
        public PLCServer(PLCMemory memory, string addr) : this(new PLCService(memory), addr)
        {
        }

        /// <summary>
        /// Creates the ZMQ server.
        /// </summary>
        /// <param name="service">A custom instance of PLC service</param>
        /// <param name="addr">Endpoint to listen on. For example, "tcp://*:5555".</param>
        public PLCServer(PLCService service, string addr)
        {
            this.addr = addr;
            this.service = service;
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
        /// Underlying service instance.
        /// </summary>
        public PLCService Service
        {
            get { return this.service; }
        }

        /// <summary>
        /// Underlying memory instance.
        /// </summary>
        public PLCMemory Memory
        {
            get { return this.service.Memory; }
        }

        private void _ServerThread()
        {
            while (this.isok)
            {
                try
                {
                    using (var socket = new ResponseSocket(this.addr))
                    {
                        // loop until told to stop or when exception is thrown
                        while (this.isok)
                        {
                            // wait for 50 ms
                            if (socket.Poll(PollEvents.PollIn, TimeSpan.FromMilliseconds(50)).HasIn())
                            {
                                this._RecvAndSend(socket);
                            }
                        }
                    }
                }
                catch (NetMQException e)
                {
                    // recover from zmq error by re-creating socket
                    // TODO: log here
                    Console.WriteLine("Encountered ZMQ error: {0}, will re-create socket.", e.Message);
                }
            }
        }

        private void _RecvAndSend(ResponseSocket socket)
        {
            string received = socket.ReceiveFrameString(System.Text.Encoding.UTF8);
            PLCResponse response = null;
            try
            {
                // ask service to handle request
                PLCRequest request = JsonConvert.DeserializeObject<PLCRequest>(received, jsonSettings);
                response = this.service.Handle(request);
            }
            catch (PLCException e)
            {
                // reply with an error
                response = new PLCResponse
                {
                    Error = new PLCResponse.PLCError { Type = e.Code, Desc = e.Message },
                };
            }
            catch (System.Exception e)
            {
                // reply with an error
                response = new PLCResponse
                {
                    Error = new PLCResponse.PLCError { Type = "unkown", Desc = e.Message },
                };
            }

            // serialize to json and send
            if (response == null)
            {
                response = new PLCResponse();
            }
            string serialized = JsonConvert.SerializeObject(response, Formatting.None, jsonSettings);
            socket.SendFrame(System.Text.Encoding.UTF8.GetBytes(serialized));
        }
    }
}
