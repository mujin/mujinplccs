using System;
using System.Collection.Generic;


namespace mujinplccs{

    // these are delegate callback function. Customer implement detailed functions according to these interface, and set public delegate variables point to those functions.
    public delegate void DelOnFinishCodeChangedCallback(PLCLogic.PLCOrderCycleFinishCode orderCycleFinishCode);
    public delegate void DelOnNumberPutInDestChangedCallback(int numberPutInDest);
    // ask conveyor/agv to move. if queue is empty, pass in empty string. this is a function to tell customer system that we have finished 1. current order, 2. please move conveyor/agv  3. the container info we are expecting according to the queue.
    public delegate void DelMove(string expectedContainerId, string expectedContainerType, DelMoveDoneCallback moveDoneCallback);
    public delegate void DelMoveDoneCallback(bool locationProhibited, string containerId, string containerType);

    // Order Model
    public sealed class Order{
        public Order(){}
        // some order finish status
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

    // Location Model
    public sealed class PLCLocation{ // class to represent location model
        public PLCLocation(string locationIndex){
            this.locationIndex = locationIndex;
        }
        public DelMove MoveCallback{set;};
        // custom set this status to tell mujin controller if location is ready to pick. this maybe private impelementation, not sure c# feature here.
        public sealed void SetStatus(bool locationProhibited, string containerId, string containerType){
            this.locationProhibited = locationProhibited;
            this.containerId = containerId;
            this.containerType = containerType;
        }
        public delegate DelMoveDoneCallback = Setstatus;
        public int locationIndex{get; set;};
        // location status, e.g. prohibited or not .
        public bool locationProhibited{get; set};
        public string containerId{get; set};  // used for verification
        public string containerType{get; set}; // used for verification


    }
}
