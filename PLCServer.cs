using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZMQ;

namespace mujinplccs
{
    public class PLCServer
    {
        private string addr;
        private bool isok = true;
    
        public PLCServer(string addr = "tcp://*:5555")
        {
            this.addr = addr;
        }

        /// <summary>
        /// Start the PLC server on a background thread.
        /// </summary>
        public void Start()
        {

        }

        /// <summary>
        /// Stop the PLC server. Will block until the background thread teminates.
        /// </summary>
        public void Stop()
        {

        }

        public bool IsRunning()
        {
            return false;
        }

        private void ServerThread()
        {
            using (Context context = new Context())
            using (Socket socket = context.Socket(SocketType.REP))
            {
                socket.Bind(this.addr);

                while (this.isok)
                {
                    byte[] data = socket.Recv();

                    socket.Send(data);
                }
            }
        }
    }
}
