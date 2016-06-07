using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mujinplccs;

namespace mujinplcexamplecs
{
    class Program
    {
        static void Main(string[] args)
        {
            var memory = new PLCMemory();
            var controller = new PLCController(memory);
            var server = new PLCServer(memory, "tcp://*:5555");
            server.Start();

            Console.WriteLine("Server started and listening on {0} ...", server.Address);

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Waiting for start order cycle ...");
                controller.WaitFor("startOrderCycle", true);

                Console.WriteLine("Waiting until stop order cycle ...");
                controller.WaitUntil("stopOrderCycle", true);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);

            server.Stop();
        }
    }
}
