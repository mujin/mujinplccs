using System;
using System.Threading;
using System.Threading.Tasks;
using mujinplccs;

namespace mujinplcexamplecs
{
    class Example : IMaterialHandler {
        private IPickWorker pickWorker;

        public Example(PLCMemory plcMemory) {
            this.pickWorker = PickWorkerFactory.Instance().CreatePickWorker(plcMemory, this);
        }

        public void InjectFakeOrders() {
            this.pickWorker.QueueOrder("a", new PickWorkerQueueOrderParameters{
                partType = "facebox",
                orderNumber = 1,
                pickLocationIndex = 1,
                pickContainerId = "00001",
                placeLocationIndex = 3,
                placeContainerId = "pallet1",
                packInputPartIndex = 1,
            });

            this.pickWorker.QueueOrder("b", new PickWorkerQueueOrderParameters{
                partType = "facebox",
                orderNumber = 1,
                pickLocationIndex = 2,
                pickContainerId = "00002",
                placeLocationIndex = 3,
                placeContainerId = "pallet1",
                packInputPartIndex = 2,
            });

            this.pickWorker.QueueOrder("c", new PickWorkerQueueOrderParameters{
                partType = "facebox",
                orderNumber = 1,
                pickLocationIndex = 1,
                pickContainerId = "00003",
                placeLocationIndex = 3,
                placeContainerId = "pallet1",
                packInputPartIndex = 3,
            });

            this.pickWorker.QueueOrder("d", new PickWorkerQueueOrderParameters{
                partType = "facebox",
                orderNumber = 1,
                pickLocationIndex = 2,
                pickContainerId = "00004",
                placeLocationIndex = 3,
                placeContainerId = "pallet1",

                packInputPartIndex = 4,
            });
        }

        // mujin will request agv to move away and next agv to come
        // do not return until the new agv is in position and ready
        public async Task<Tuple<string, string>> MoveLocationAsync(int locationIndex, string expectedContainerId, string expectedContainerType)
        {
            // look at locationIndex, depending on definition, it could be source location or dest location
            // move conveyor or agv, either sync or async, then
            string actualContainerId = ""; // obtained from barcode scanner
            string actualContainerType = "";

            // only return when the location is ready for picking or placing
            return new Tuple<string, string>(actualContainerId, actualContainerType);
        }

        // mujin will not start next order cycle until customer confirms current order by returning
        public async Task FinishOrderAsync(string orderUniqueId, PLCLogic.PLCOrderCycleFinishCode orderFinishCode)
        {
            // report to server about order
            // check orderFinishCode to see if you need human intervention
            return; // return only when okay to continue with next order
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // init plc memory and production cycle
            PLCMemory plcMemory = new PLCMemory();
            
            // init customer example
            Example example = new Example(plcMemory);
            example.InjectFakeOrders(); // TODO: for local testing, need to inject real orders from upper system

            // start server, mujin controller will connect to this server
            PLCServer plcServer = new PLCServer(plcMemory, "tcp://*:5555");
            Console.WriteLine("starting server to listen on {0} ...", plcServer.Address);
            plcServer.Start();

            // wait until connected (optional)
            PLCController plcController = new PLCController(plcMemory, TimeSpan.FromSeconds(1.0), "commCounter");
            plcController.WaitUntilConnected();
            Console.WriteLine("controller connected");

            // TODO: just testing, change to press enter to exit
            Thread.Sleep(3600*1000);
        }
    }
}
