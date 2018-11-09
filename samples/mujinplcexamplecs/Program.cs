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
            PLCController controller = new PLCController(memory, TimeSpan.FromSeconds(1.0));
            var logic = new PLCLogic(controller);
            var server = new PLCServer(memory, "tcp://*:5555");

            Console.WriteLine("Starting server to listen on {0} ...", server.Address);
            server.Start();

            Console.WriteLine("Waiting for controller connection ...");
            logic.WaitUntilConnected();
            Console.WriteLine("Controller connected.");

            try
            {
                if( controller.GetBoolean("isError") ) {
                    Console.WriteLine("controller is in error 0x{0:X}, resetting", controller.Get("errorcode"));
                    logic.ResetError();
                }

                if( controller.GetBoolean("isRunningOrderCycle") ) {
                    Console.WriteLine("previous cycle already running, so stop and wait");
                    logic.StopOrderCycle();
                }
                
                Console.WriteLine("Waiting for cycle ready...");
                logic.WaitUntilOrderCycleReady();

                Console.WriteLine("Starting order cycle ...");
                PLCLogic.PLCOrderCycleStatus status;


                // first work piece
                controller.Set("orderRobotId",1);
                status = logic.StartOrderCycle("Mr05_milk_copy", 1);

                Console.WriteLine("Order cycle started. numLeftInOrder = {0}, numLeftInLocation1 = {1}.", status.numLeftInOrder, status.numLeftInLocation1);

                while (true)
                {
                    status = logic.WaitForOrderCycleStatusChange();
                    if (!status.isRunningOrderCycle)
                    {
                        Console.WriteLine("Cycle finished. {0}", status.orderCycleFinishCode);
                        break;
                    }
                    Console.WriteLine("Cycle running. numLeftInOrder = {0}, numLeftInLocation1 = {1}.", status.numLeftInOrder, status.numLeftInLocation1);
                }
            }
            catch (PLCLogic.PLCError e)
            {
                Console.WriteLine("PLC Error. {0}", e.Message);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);

            server.Stop();
        }
    }
}
