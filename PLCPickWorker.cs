using System;
using System.Threading.Tasks;

namespace mujinplccs
{
    // struct describing order data
    public struct PickWorkerQueueOrderParameters {
        public string partType; // type of the product to be picked, for example: "cola"
        public int orderNumber; // number of items to be picked, for example: 1
        public int robotId; // set to 1

        public int pickLocationIndex; // index of location for source container, location defined on mujin pendant
        public string pickContainerId; // barcode of the source container, for example: "010023"
        public string pickContainerType; // type of the source container, if all the same, set to ""
        public int placeLocationIndex; // index of location for dest container, location defined on mujin pendant
        public string placeContainerId; // barcode of the dest contianer, for example: "pallet1"
        public string placeContainerType; // type of the source container, if all the same, set to ""

        // packing related params
        public int packInputPartIndex; // when using packFormation, index of the part in the pack
        public string packFormationComputationName; // when using packFormation, name of the formation
    }

    // interface for customer to interact
    // when error happens, throw exceptions instead of return values
    public interface IPickWorker {
        // queue an order, note that the order should be queued according to the container arrival sequence at the pickLocation
        void QueueOrder(string orderUniqueId, PickWorkerQueueOrderParameters queueOrderParameters);
    }

    // to be implemented by customer
    public interface IMaterialHandler {
        // when location needs moving called by mujin
        // send request to agv to move, can return immediately even if agv has not started moving yet
        // function should return a pair of actual containerId and containerType
        Task<Tuple<string, string>> MoveLocationAsync(int locationIndex, string containerId, string containerType);

        // when order status changed called by mujin
        Task FinishOrderAsync(string orderUniqueId, PLCLogic.PLCOrderCycleFinishCode orderFinishCode);
    }

    // the pick worker factory
    public interface IPickWorkerFactory {
        // customer code calls this to create an instance of IPickWorker while supplying their implementation
        IPickWorker CreatePickWorker(PLCMemory plcMemory, IMaterialHandler materialHandler);
    }
}

