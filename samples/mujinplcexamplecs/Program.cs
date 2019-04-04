using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mujinplccs;

namespace mujinplcexamplecs
{
    class Program
    {
        static void Main(string[] args)
        {
            // get the order number from command line
            var orderNumber = 50;
            if (args.Length > 0) {
                orderNumber = Convert.ToInt32(args[0]);
            }
            Console.WriteLine("orderNumber = {0}", orderNumber);

            var memory = new PLCMemory();
            PLCController controller = new PLCController(memory, TimeSpan.FromSeconds(1.0));
            var logic = new PLCLogic(controller);
            var server = new PLCServer(memory, "tcp://*:5555");

            Console.WriteLine("Starting server to listen on {0} ...", server.Address);
            server.Start();

            for (var iteration = 0; ; iteration++) {        
                try
                {
                    Console.WriteLine("Waiting for controller connection ...");
                    logic.WaitUntilConnected();
                    Console.WriteLine("Controller connected.");

                    Console.WriteLine("Waiting until auto mode ...");
                    logic.WaitUntilAutoMode();
                    Console.WriteLine("Auto mode.");

                    // if there is error on controller, reset it first
                    if (logic.IsError()) {
                        Console.WriteLine("Controller is in error 0x{0:X}, resetting", controller.Get<int>("errorcode", 0));
                        logic.ResetError();
                    }

                    // if there is an order cycle running, stop it first
                    if (logic.GetOrderCycleStatus().isRunningOrderCycle) {
                        Console.WriteLine("Previous cycle already running, so stop and wait");
                        logic.StopOrderCycle();
                    }

                    // is robot is grabbing target, stop gripper first
                    if (logic.IsGrabbingTarget()) {
                        Console.WriteLine("Robot is grabbing target, stop and unchuck gripper");
                        logic.StopGripper();
                        logic.UnchuckGripper();
                    }

                    // if robot is not at home, move to home before starting cycle
                    if (!logic.IsAtHome()) {
                        Console.WriteLine("Robot is not at home, moving to home");
                        logic.StartMoveToHome();
                    }
                    
                    Console.WriteLine("Waiting for cycle ready...");
                    logic.WaitUntilOrderCycleReady();
                    Console.WriteLine("Cycle ready.");

                    Console.WriteLine("Starting order cycle ...");

                    // switch source and dest based on iteration
                    var orderPartType = (iteration % 2 == 0) ? "A9607-884N-1" : "A9607-884N-2";
                    var orderPickLocationIndex = (iteration % 2 == 0) ? 1 : 2;
                    var orderPlaceLocationIndex = (iteration % 2 == 0) ? 2 : 1;
                    var orderPickContainerId = String.Format("{0}-{1}", iteration, orderPickLocationIndex);
                    var orderPlaceContainerId = String.Format("{0}-{1}", iteration, orderPlaceLocationIndex);
                    Console.WriteLine("Starting order cycle, orderPartType = {0}, orderNumber = {1}, orderPickLocation {2} ({3}) -> orderPlaceLocation {4} ({5}).", orderPartType, orderNumber, orderPickLocationIndex, orderPickContainerId, orderPlaceLocationIndex, orderPlaceContainerId);

                    var status = logic.StartOrderCycle(orderPartType, orderNumber, orderPickLocationIndex, orderPickContainerId, orderPlaceLocationIndex, orderPlaceContainerId);
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
                catch (PLCLogic.PLCError e)
                {
                    Console.WriteLine("PLC Error. {0}", e.Message);
                    if (!controller.IsConnected) {
                        Console.WriteLine("PLC disconnected, quiting");
                        break;
                    }
                    Thread.Sleep(3000);
                }
            }

            server.Stop();
        }
    }
}
