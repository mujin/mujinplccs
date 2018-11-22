using System;
using System.Collection.Generic;

namespace mujinplccs
{
    public sealed class MujinSystem{
        // maybe a dictionary mapping from locationIndex to location;
        List<PLCLocation> locations;

        public void AddLocation(PLCLocation location){  // user call system.AddLocation(location1) to add location, when system start and location queue is not empty, will automatics start Cycle after condition satisifed
            this.locations.Insert(location);
        }
        abstract public bool AddOrderToLocationQueue(Order order);  // Customer system adds order to location
        abstract public Order RemoveOrderFromLocationQueue(PLCLocation location);    // Customer system removes order from location
    }
}
