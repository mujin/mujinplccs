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
        public void Move(string expectedContainerId, string expectedContainerType){

            string nextContainerId = this.barcodeChecker.ScanContainerBarcode();
            string nextContainerType = this.containerPool.GetContainerTypeById(nextContainerId);

            if(nextContainerId != expectedContainerId || nextContainerType != expectedContainerType){
                // PANIC!
                // customer need to deal with exception . cancel order etc..
            }
            else{
                // ready..
                while(!this.Ready()) // this == current location. maybe a subclass from PLCLocation base class.
                {
                    //....move check move check...
                }
                this.SetStatus(prohibited=False, containerId = nextContainerId, containerType=nextContainerType);
            }
        }

        static void Main(string[] args){

            // customer instantiate mujinsystem.
            MujinSystem pickworker = new MujinSystem();
            // customer model the location as Location, set location info etc.
            // maybe create a mapping from location name to location index
            PLCLocation srclocaiton1 = new PLCLocation(locationIndex=1, Move=Move);
            PLCLocation srclocaiton2 = new PLCLocation(locationIndex=2, Move=Move);
            PLCLocation destlocation1 = new PLCLocation(locationIndex=3, Move=Move);

            // customer add locations to system.
            pickworker.AddLocation(srcLocation1);
            pickworker.AddLocation(srcLocation2);
            pickworker.AddLocation(destLocation1);

            // customer create order class, set orderinfo, such as parttype, picklocation, placelocation etc.
            Order order1 = new Order("box1", orderPickLocation=srclocation1, orderPlaceLocation=destlocation1);
            Order order2 = new Order("box2", orderPickLocation=srclocation2, orderPlaceLocation=destlocation1);

            // customer queue the order into corresponding location
            pickworker.AddOrderToLocationQueue(order1, srclocation1);
            pickworker.RemoveOrderFromLocationQueue(order2, srclocation2);

            pickworker.Start();
            // MujinSystem finds queue isn't empty, start to deal with order in the queue
            // ...
            // After finish one order on locations
            // Mujin will call location.ProcessNextOrder(location);
            // this step is for verification

            // .....
            // customer move container (by agv or conveyor) to location, after it finished, customer will call
            // location.SetStatus(prohibited=False, containerId='123', containerType='sometype');

            // Mujin will check if condition is satisifed and start to pickplace order.

            // Customer need to implement the 1. onFinishCodeChangedCallback and 2. onNumberPutInDestChangedCallback
            // Mujin will invoke these method after corresponding signal and code changed to notify customer system.

            // in summary, what customer need to do is 
            // TODO add code
            // 1. onFinishCodeChangedCallback (order info changed callback)
            // 2. onNumberPutInDestChangedCallback (order info changed callback
            // 3. SetStatus
            // 4. Move
            // 5. AddOrderToLocationQueue and RemoveOrderFromLocationQueue
    }
}
