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

        // moveDoneCallback allow mujin to add a callback function when calling ProcessNextOrder to customer. Also allow customer to do both sync and async job here.
        public void ProcessNextOrder(string expectedContainerId, string expectedContainerType, PLCLocation.DelProcessNextOrderDoneCallback processNextOrderDoneCallback){
            // customer do something, maybe sync, maybe async
            // customer call doneCallback("id", "type")

            // ============= sync version  ==============================
            string nextContainerId = this.barcodeChecker.ScanContainerBarCode();
            string nextContainerType = this.containerPool.GetContainerTypeById(nextContainerId);

            if(nextContainerId != expectedContainerId || nextContainerType != expectedContainerType){
                // PANIC!
                // customer need to deal with exception . cancel order etc..
            }
            else{
                // ready..
                while(!this.Ready()) // this == current location. maybe a subclass from PLCLocation base class.
                {
                    // busy wait here
                    //....move check move check...
                }
                processNextOrderDoneCallback(prohibited=False, containerId = nextContainerId, containerType=nextContainerType);
            }
            // ==================================

            // ==============async wait version =================
            asyncMove(processNextOrderDoneCallback);
            return;
            // =====================
        }
        public void onFinishCodeChangedCallback(PLCLogic.PLCOrderCycleFinishCode orderCycleFinishCode)
        {
            // customer TODO

            // e.g
            warehouse.money += 1000;
        }

        public void onNumberPutInDestChangedCallback(int numberPutInDest){
            // custerom TODO
            // e.g
            warehouse.stock -= 1;
        }

        static void Main(string[] args){

            // customer instantiate mujinsystem.
            MujinPickWorkerBase pickworker = new MujinPickWorkerBase();
            // customer model the location as Location, set location info etc.
            // maybe create a mapping from location name to location index

            PLCLocation.DelProcessNextOrderCallback processNextOrderCallback = ProcessNextOrder;
            PLCLocation srclocaiton1 = new PLCLocation(locationIndex=1);
            PLCLocation srclocaiton2 = new PLCLocation(locationIndex=2);
            PLCLocation destlocation1 = new PLCLocation(locationIndex=3);

            srclocation1.delProcessNextOrderCallback = processNextOrderCallback;
            srclocation2.delProcessNextOrderCallback = processNextOrderCallback;
            destlocation1.delProcessNextOrderCallback = processNextOrderCallback;

            // customer add locations to system.
            pickworker.AddLocation(srcLocation1);
            pickworker.AddLocation(srcLocation2);
            pickworker.AddLocation(destLocation1);


            PLCLocation.DelOnFinishCodeChangedCallback delOnFinishCodeChanged = onFinishCodeChangedCallback;
            PLCLocation.DelOnNumberPutInDestChangedCallback delOnNumberPutInDestChanged = onNumberPutInDestChangedCallback;
            // customer create order class, set orderinfo, such as parttype, picklocation, placelocation etc.
            Order order1 = new Order("box1", orderPickLocation=srclocation1, orderPlaceLocation=destlocation1);
            Order order2 = new Order("box2", orderPickLocation=srclocation2, orderPlaceLocation=destlocation1);
            Order order3 = new Order("wrongorderpart", orderPickLocation=srclocation2, orderPlaceLocation=destlocation1);
            order1.onFinishCodeChangedCallback = delOnFinishCodeChanged;
            order2.onFinishCodeChangedCallback = delOnFinishCodeChanged;
            order3.onFinishCodeChangedCallback = delOnFinishCodeChanged;
            order1.onNumberPutInDestChangedCallback = delOnNumberPutInDestChanged;
            order2.onNumberPutInDestChangedCallback = delOnNumberPutInDestChanged;
            order3.onNumberPutInDestChangedCallback = delOnNumberPutInDestChanged;

            // customer queue the order into corresponding location
            pickworker.AddOrder(order1);
            pickworker.AddOrder(order2);
            pickworker.RemoveOrder(order3);

            pickworker.Start();
            pickworker.Pause();
            pickworker.Resume();
            // MujinSystem finds queue isn't empty, start to deal with order in the queue
            // ...
            // After finish one order on locations
            // Mujin will call location.ProcessNextOrder(location);
            // this step is for verification

            // Mujin system will call customer callback
            // for example location1.

            Order nextOrder = location1.NextOrder();
            location1.delProcessNextOrderCallback(nextOrder.containerId, nextOrder.containerType, location1.delProcessNextOrderDoneCallback); // custeomr can implement this function as aysnc or sync. See example fuction 'Move' above.


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
            // 4. processNextOrder
            // 5. AddOrder and RemoveOrder 
    }
}
