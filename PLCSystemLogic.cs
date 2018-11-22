using System;
using System.Collection.Generic;

namespace mujinplccs
{
    /** base class to store locations and process orders, to be implemented by mujin partner
     */
    public sealed class MujinPickWorkerBase{
        public MujinPickWorkerBase(List<Location> locations) {
            this.locations = locations;
        }
        // maybe a dictionary mapping from locationIndex to location;
        List<PLCLocation> locations; // locations to be supported by this pickworker

        public void AddLocation(PLCLocation location){  // user call system.AddLocation(location1) to add location, when system start and location queue is not empty, will automatics start Cycle after condition satisifed
            this.locations.Insert(location);
        }
        abstract public bool AddOrder(Order order);  // adds order to order's source location
        abstract public bool RemoveOrder(Order order);    // removes order from order's source location
        abstract public bool Start(); // starts pick and place task
        abstract public bool StopImmediately(); // stops pick and place task immeidately
        abstract public bool Stop(); // stops pick and place task after current pick and place task
        abstract public bool Pause(); // pauses pick and place task
        abstract public bool Resume(); // resumes pick and place task

    }
}
