using System;
using System.Collection.Generic;


namespace mujinplccs{

    public class Order{
        public Order(){}
        public PLCLogic.PLCOrderCycleFinishCode orderCycleFinishCode;
        public PLCLogic.PLCPreparationCycleFinishCode preparationCycleFinishCode;

        abstract public void onFinishCodeChangedCallback();
        abstract public void onNumberPutInDestChangedCallback();

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
    public class PLCLocation{
        public PLCLocation(string locationName){
            this.locationName = locationName;
        }
        abstract public void SetStatus();
        abstract public void Move(string expectedContainerId, string expectedContainerType); // ask conveyor/agv to move. if queue is empty, pass in empty string
        public int locationIndex{get; set;};
        public bool locationProhibited;
        public string containerId;  // used for verification
        public string containerType; // used for verification
    }
 
}
