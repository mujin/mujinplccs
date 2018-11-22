using System;
using System.Collection.Generic;


namespace mujinplccs{

    // these are delegate callback function. Customer implement detailed functions according to these interface, and set public delegate variables point to those functions.
    // mujin partner need to implement business logic for handling different finish codes
    public delegate void DelOnFinishCodeChangedCallback(PLCLogic.PLCOrderCycleFinishCode orderCycleFinishCode);
    // mujin partner need to implement business logic for handling part placement event
    public delegate void DelOnNumberPutInDestChangedCallback(int numberPutInDest);
    // ask conveyor/agv to move. if queue is empty, pass in empty string. this is a function to tell customer system that we have finished 1. current order, 2. please move conveyor/agv  3. the container info we are expecting according to the queue.
    public delegate void DelProcessNextOrderCallback(string expectedContainerId, string expectedContainerType, DelProcessNextOrderDoneCallback processNextOrderDoneCallback);
    public delegate void DelProcessNextOrderDoneCallback(bool locationProhibited, string containerId, string containerType);

    /** base class to store order information and process result, to be implemented by mujin partner
     */
    public sealed class Order{
        public Order(){}//TODO add required fields to constructor and set initial values
        // order finish status
        public PLCLogic.PLCOrderCycleFinishCode orderCycleFinishCode;
        public PLCLogic.PLCPreparationCycleFinishCode preparationCycleFinishCode;

        // customer implemented interface (delegate)
        public DelOnFinishCodeChangedCallback onFinishCodeChangedCallback{set;};
        public DelOnNumberPutInDestChangedCallback onNumberPutInDestChangedCallback{set;};


        // order info
        public string partType{get; set;}
        public int orderNumber{get; set;}
        public string uniqueId{get; set;}
        public string containerId{get; set;}
        public string containerType{get; set;}
        public string robotId{get; set;}
        public string placeContainerId{get; set;}
        public string placeContainerType{get; set;}
        public PLCLocation placeLocation{get; set;}
        public PLCLocation pickLocation{get; set;}
        public int packInputPartIndex{get; set;}
        public string packFormationComputationName{get; set}


    }

    /** base class to store location information and process material handling logic, to be implemented by mujin partner
     */
    public sealed class PLCLocation{ // class to represent location model
        public PLCLocation(string locationIndex){
            //TODO add required fields to constructor and set initial values
            this.locationIndex = locationIndex;
        }
        public DelProcessNextOrderCallback delProcessNextOrderCallback{set;};// ask conveyor/agv to deliver the next container
        // custom set this status to tell mujin controller if location is ready to pick. this maybe private impelementation, not sure c# feature here.
        // when locationProhibited is 1 (such as when the container is still moving), mujin cannot move robot into this location
        public sealed void SetStatus(bool locationProhibited, string containerId, string containerType){
            this.locationProhibited = locationProhibited;
            this.containerId = containerId;
            this.containerType = containerType;
        }
        public DelProcessNextOrderDoneCallback delProcessNextOrderDoneCallback = Setstatus;
        public string locationName; // description for human to read
        public int locationIndex{get; set;};  // unique index
        public int locationProhibited; // 0: not prohibited, mujin robot can enter; 1: prohibited, mujin robot cannot enter; -1: unknown
        public string containerId;  // id of container
        public string containerType; // type of container, empty means use existing container
    }
}
