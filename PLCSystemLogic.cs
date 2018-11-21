using System;
using System.Collection.Generic;

namespace mujinplccs
{
    public class MujinSystem{

        List<Location> sourceLocations;
        List<Location> destLocations;

        public void AddSourceLocation(PLCLocation location){
            this.sourceLocations.Insert(location);
        }
        public void AddDestLocation(PLCLocation location){
            this.destLocations.Insert(location);
        }

        public void StartCycle(PLCLocation location);
        public void StopCycle(PLCLocation location);

        abstract public bool QueueOrder(Order order, PLCLocation location);
        abstract public Order DequeueOrder(PLCLocation location);

    }

    public void StartConveyorCycle(){
        while(!conveyor.terminate){
            foreach(var location in conveyor.sourceLocation){
                if(location.count() > 0){
                    PLCQueue.OrderItem order = location.Peak();
                    if(order.orderCycleFinishCode == PLCLogic.PLCOrderCycleFinishCode.FinishedNotAvailable){
                        // already deal with this cycle, wait customer to remove it .
                        continue;
                    }
                    // start orderCycle;
                }
            }
        }
        PLCQueue.OrderItem order = queue.peak();
    }
    public void StopConveyorCycle(ref PLCConveyro conveyor){
        conveyor.terminate = true;
    }
}
