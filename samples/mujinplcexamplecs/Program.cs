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
            var logic = new PLCLogic(controller);
            var server = new PLCServer(memory, "tcp://*:5555");
            server.Start();

            Console.WriteLine("Server started and listening on {0} ...", server.Address);

            try
            {

                Console.WriteLine("Starting order cycle ...");
                var status = logic.StartOrderCycle("123", "123", 10);
                Console.WriteLine("Order cycle started. numLeftInOrder = {0}, mumLeftInSupply = {1}.", status.NumLeftInOrder, status.NumLeftInSupply);

                while (true)
                {
                    status = logic.WaitForOrderCycleStatusChange();
                    if (!status.IsRunningOrderCycle)
                    {
                        Console.WriteLine("Cycle finished. {0}", status.OrderCycleFinishCode);
                        break;
                    }
                    Console.WriteLine("Cycle running. numLeftInOrder = {0}, mumLeftInSupply = {1}.", status.NumLeftInOrder, status.NumLeftInSupply);
                }
            }
            catch (PLCLogic.PLCError e)
            {
                Console.WriteLine("PLC Error. {0}. {1}x{2}", e.Message, (int)e.ErrorCode, e.DetailedErrorCode);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);

            server.Stop();
        }
    }
}
