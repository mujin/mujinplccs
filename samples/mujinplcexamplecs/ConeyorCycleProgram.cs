using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using CommandLine;
using Newtonsoft.Json;
using mujinplccs;

namespace mujinplcexamplecs
{
    class Program
    {
        // customer instantiate mujinsystem.
        MujinSystem system = new MujinSystem();
        // customer model the location as PLCLocation, set location info etc.
        // maybe create a location mapping from name to index.
        PLCLocation srclocaiton1 = new PLCLocation(locationIndex=1);
        PLCLocation srclocaiton2 = new PLCLocation(locationIndex=2);
        PLCLocation destlocation1 = new PLCLocation(locationIndex=3);

        // customer add locations to system. 
        system.AddLocation(srcLocation1);
        system.AddLocation(srcLocation2);
        system.AddLocation(srcLocation3);

        // customer create order class, set orderinfo, such as parttype, picklocation, placelocation etc.
        Order order1 = new Order("box1", orderPickLocation=srclocation1, orderPlaceLocation=destlocation3);
        Order order2 = new Order("box2", orderPickLocation=srclocation2, orderPlaceLocation=destlocation3);

        // customer queue the order into corresponding location
        system.AddOrderToLocationQueue(order1, srclocation1);
        system.RemoveOrderFromLocationQueue(order2, srclocation2);

        // MujinSystem finds queue isn't empty, start to deal with order in the queue
        // ...
        // After finish one order on locations
        // Mujin will get an order from queue: nextOrder = GetNextOrder(location);
        // Mujin will call location.Move(nextOrder.containerId, nextOrder.containerType);
        // this step is for verification

        // .....
        // customer move agv/conveyor to location, after it finished, customer will call
        // location.SetStatus(prohibited=False, containerId='123', containerType='sometype');

        // Mujin will check if condition is satisifed and start to pickplace order.

        // Customer need to implement the 1. onFinishCodeChangedCallback and 2. onNumberPutInDestChangedCallback
        // Mujin will invoke these method after corresponding signal and code changed to notify customer system.


        // in summary, what customer need to do is
        // 1. onFinishCodeChangedCallback (order info changed callback)
        // 2. onNumberPutInDestChangedCallback (order info changed callback
        // 3. SetStatus
        // 4. Move
        // 5. AddOrderToLocationQueue and RemoveOrderFromLocationQueue
    }
}
