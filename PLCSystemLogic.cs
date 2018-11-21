using System;
using System.Collection.Generic;

namespace mujinplccs
{
    public class MujinSystem{
        List<Location> locations;
        public void AddLocation(PLCLocation location){  // user call system.AddLocation(location1) to add location, when system start and location queue is not empty, will automatics start Cycle after condition satisifed
            this.location.Insert(location);
        }
        abstract public bool QueueOrder(Order order, PLCLocation location);  // Customer add order to specified location
        abstract public Order DequeueOrder(PLCLocation location);    // Customer dequeue order
    }
}
