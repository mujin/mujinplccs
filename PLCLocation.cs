using System;
using System.Collection.Generic;


namespace mujinplccs{

    public class Order{
        public Order(){}
        // some order finish status
        public PLCLogic.PLCOrderCycleFinishCode orderCycleFinishCode;
        public PLCLogic.PLCPreparationCycleFinishCode preparationCycleFinishCode;

        // customer implemented interface
        abstract public void onFinishCodeChangedCallback();
        abstract public void onNumberPutInDestChangedCallback();
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
    public class PLCLocation{ // class to represent location model
        public PLCLocation(string locationName){
            this.locationName = locationName;
        }
        abstract public void SetStatus(bool locationProhibited, string containerId, string containerType);  // custom set this status to tell mujin controller if location is ready to pick
        abstract public void Move(string expectedContainerId, string expectedContainerType); // ask conveyor/agv to move. if queue is empty, pass in empty string. this is a function to tell customer system that we have finished 1. current order, 2. please move conveyor/agv  3. the container info we are expecting according to the queue.
        public int locationIndex{get; set;};
        // location status, e.g. prohibited or not .
        public bool locationProhibited;
        public string containerId;  // used for verification
        public string containerType; // used for verification
    }
 
}
