using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using CommandLine;
using Newtonsoft.Json;
using mujinplccs;

namespace mujinplcexamplecs
{
    class Program
    {
        public class Options
        {
            [Option]
            public string filename {get; set;}
        }

        public class Order
        {
            public string orderPartType;
            public int orderNumber;
            public int orderPickLocation;
            public int orderPlaceLocation;
            public string orderPickContainerId;
            public string orderPlaceContainerId;
        }

        public static List<Order> LoadJSON(string filename)
        {
            using (StreamReader r = new StreamReader(filename))
            {
                string content = r.ReadToEnd();
                List<Order> orders = JsonConvert.DeserializeObject<List<Order>>(content);
                return orders;
            }
        }

        static void Main(string[] args)
        {
            string filename = "";
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
                    {
                        filename = o.filename;
                    });

            List<Order> orders = LoadJSON(filename);

            // init setup and start plcserver
            var memory = new PLCMemory();
            PLCController controller = new PLCController(memory, TimeSpan.FromSeconds(1.0));
            var logic = new PLCLogic(controller);
            var server = new PLCServer(memory, "tcp://*:5555");
            Console.WriteLine("starting server to listen on {0} ...", server.Address);
            Console.WriteLine("press enter to stop server...");
            server.Start();

            logic.WaitUntilConnected();
            Console.WriteLine("controller connected");
            // try to clear state first
            IDictionary<string, object> writeSignal = new Dictionary<string, object>();
            writeSignal.Add("clearState", true);
            memory.Write(writeSignal);
            writeSignal["clearState"] = false;
            memory.Write(writeSignal);

            if(controller.GetBoolean("isError")){
                Console.WriteLine("controller is in error 0x{0:X}, resetting", controller.Get("errorcode"));
                logic.ResetError();
            }
            if( controller.GetBoolean("isRunningOrderCycle")){
                Console.WriteLine("previous cycle already running, so stop and wait");
                logic.StopOrderCycle();
            }
            for(var orderIndex=0; orderIndex<orders.Count; orderIndex++){
                Order order = orders[orderIndex];
                logic.WaitUntilPreparationCycleReady();
                Console.WriteLine("start preparation order {0}, orderPartType = {1}, orderNumber={2}, orderPickLocation={3}, orderPlaceLocation={4}", orderIndex, order.orderPartType, order.orderNumber, order.orderPickLocation, order.orderPlaceLocation);
                logic.StartPreparationCycle(order.orderPartType, order.orderNumber, order.orderPickLocation, order.orderPickContainerId, order.orderPlaceLocation, order.orderPlaceContainerId);
                logic.WaitUntilPreparationCycleFinish();
                Console.WriteLine("Preparation Cycle Finished.");

                logic.WaitUntilOrderCycleFinish();
                logic.StartOrderCycle(order.orderPartType, order.orderNumber, order.orderPickLocation, order.orderPickContainerId, order.orderPlaceLocation, order.orderPlaceContainerId);


            }
        }
    }
}
