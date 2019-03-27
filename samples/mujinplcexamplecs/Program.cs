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
                for (var iteration = 0; ; iteration++) {
                    if (controller.Get<bool>("isError", false)) {
                        Console.WriteLine("controller is in error 0x{0:X}, resetting", controller.Get<int>("errorcode", 0));
                        logic.ResetError();
                    }

                    if (controller.Get<bool>("isRunningOrderCycle", false)) {
                        Console.WriteLine("previous cycle already running, so stop and wait");
                        logic.StopOrderCycle();
                    }
                    
                    Console.WriteLine("Waiting for cycle ready...");
                    logic.WaitUntilOrderCycleReady();

                    Console.WriteLine("Starting order cycle ...");

                    // switch source and dest based on iteration
                    var orderPartType = (iteration % 2 == 0) ? "A9607-884N-1" : "A9607-884N-2";
                    var orderNumber = 1000;
                    var orderPickLocationIndex = (iteration % 2 == 0) ? 1 : 2;
                    var orderPlaceLocationIndex = (iteration % 2 == 0) ? 2 : 1;
                    var orderPickContainerId = String.Format("{0}-{1}", iteration, orderPickLocationIndex);
                    var orderPlaceContainerId = String.Format("{0}-{1}", iteration, orderPlaceLocationIndex);
                    Console.WriteLine("Starting order cycle, orderPartType = {0}, orderNumber = {1}, orderPickLocation {2} ({3}) -> orderPlaceLocation {4} ({5}).", orderPartType, orderNumber, orderPickLocationIndex, orderPickContainerId, orderPlaceLocationIndex, orderPlaceContainerId);

                    PLCLogic.PLCOrderCycleStatus status = logic.StartOrderCycle(orderPartType, orderNumber, orderPickLocationIndex, orderPickContainerId, orderPlaceLocationIndex, orderPlaceContainerId);
                    Console.WriteLine("Order cycle started. numLeftInOrder = {0}, numPutInDestination = {1}.", status.numLeftInOrder, status.numPutInDestination);

                    while (true)
                    {
                        status = logic.WaitForOrderCycleStatusChange();
                        if (!status.isRunningOrderCycle)
                        {
                            Console.WriteLine("Cycle finished. orderFinishCode = {0}", status.orderCycleFinishCode);
                            break;
                        }
                        Console.WriteLine("Cycle running. numLeftInOrder = {0}, numPutInDestination = {1}.", status.numLeftInOrder, status.numPutInDestination);
                    }
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
