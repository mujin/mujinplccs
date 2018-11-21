using System;
using System.Collection.Generic;

namespace mujinplccs
{
    public class MujinSystem{
        List<Location> locations;
        public void AddLocation(PLCLocation location){
            this.location.Insert(location);
        }
        abstract public bool QueueOrder(Order order, PLCLocation location);
        abstract public Order DequeueOrder(PLCLocation location);
    }
}
