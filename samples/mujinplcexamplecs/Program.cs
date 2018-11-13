using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CommandLine;
using mujinplccs;

namespace mujinplcexamplecs
{
    class Program
    {
        public class Options
        {
            [Option]
            public string orderPartType {get; set;}
            [Option]
            public int orderNumber {get; set;}
            [Option]
            public int orderPickLocation {get; set;}
            [Option]
            public int orderPlaceLocation {get; set;}
        }
        static void Main(string[] args)
        {
            if(args.Length == 0){
                System.Console.WriteLine("Please enter an orderPartType and orderNumber");
                return;
            }
            string orderPartType = "";
            int orderNumber = 0;
            int orderPickLocation = 1;
            int orderPlaceLocation = 3;
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
                    {
                        orderPartType = o.orderPartType;
                        orderNumber = o.orderNumber;
                        orderPickLocation = o.orderPickLocation;
                        orderPlaceLocation = o.orderPlaceLocation;
                    });

            var memory = new PLCMemory();
            PLCController controller = new PLCController(memory, TimeSpan.FromSeconds(1.0));
            var logic = new PLCLogic(controller);
            var server = new PLCServer(memory, "tcp://*:5555");

            Console.WriteLine("Starting server to listen on {0} ...", server.Address);
            Console.WriteLine("Press Enter to stop server...");

            server.Start();

            // try to set IO for example.
            IDictionary<string, object> writeSignal = new Dictionary<string, object>();
            writeSignal.Add("location1Prohibited", true);
            memory.Write(writeSignal);

            // try to read IO for example.
            string[] readSignal = new string[]
            {
                "location1Prohibited"
            };
            Console.WriteLine("Set signal location1Prohibited to {0}", memory.Read(readSignal)["location1Prohibited"]);

            // clean up all signals for example
            writeSignal["location1Prohibited"] = false;
            memory.Write(writeSignal);
            Console.WriteLine("After Clear All Signal, location1Prohibited = {0}", memory.Read(readSignal)["location1Prohibited"]);

            // example logic:
            // 1. move to home
            // 2. startOrderCycle
            // 3. stopOrderCycle
            // 4. move to home
            try{
                Console.WriteLine("Waiting for controller to connect ... ");
                logic.WaitUntilConnected();
                Console.WriteLine("Controller connected ... ");

                if(controller.GetBoolean("isError")){
                    Console.WriteLine("controller is in error 0x{0:X}, resetting", controller.Get("errorcode"));
                    logic.ResetError();
                }
                if( controller.GetBoolean("isRunningOrderCycle")){
                    Console.WriteLine("previous cycle already running, so stop and wait");
                    logic.StopOrderCycle();
                }
                Console.WriteLine("Waiting for cycle ready...");
                logic.WaitUntilOrderCycleReady();
                Console.WriteLine("StartOrderCycle with orderPartType = {0}, orderNumber = {1}", orderPartType, orderNumber);
                PLCLogic.PLCOrderCycleStatus status;
                status = logic.StartOrderCycle(orderPartType, orderNumber, orderPickLocation, orderPlaceLocation);

                while (true)
                {
                    status = logic.WaitForOrderCycleStatusChange();
                    if (!status.isRunningOrderCycle)
                    {
                        Console.WriteLine("Cycle finished. {0}", status.orderCycleFinishCode);
                        break;
                    }
                    Console.WriteLine("Cycle running. numLeftInOrder = {0}", status.numLeftInOrder);
                }
            }
            catch(PLCLogic.PLCError e){
                Console.WriteLine("PLC Error. {0}", e.Message);
            }

            // main loop to receive zmq message.
            try{
                Console.WriteLine("Listening to zmq message .... ");
                while(Console.ReadKey().Key != ConsoleKey.Enter){
                    Thread.Sleep(100);;
                }
                server.Stop();
            }
            catch(PLCLogic.PLCError e){
                Console.WriteLine("PLC Error. {0}", e.Message);
            }
        }
    }
}
