using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace mujinplccs
{
    // implements the production cycle control
    // can be internally deployed on mujin controller
    // this is a csharp version
    public class PLCProductionCycle {
        private PLCMemory plcMemory;
        private int maxLocationIndex;
        private int queueCapacity;
        private Thread thread;
        private IMaterialHandler iMaterialHandler;

        // Order struct used internal
        // TODO for now it's bascilly the same as PickWorkerQueueOrderParameters.
        private class Order{
            public string orderPartType;
            public int orderNumber;
            public int orderRobotId;
            public string orderUniqueId;
            public int orderPickLocation;
            public string orderPickContainerId;
            public string orderPickContainerType;
            public int orderPlaceLocation;
            public string orderPlaceContainerId;
            public string orderPlaceContainerType;

            // packing related params
            public int packInputPartIndex;
            public string packFormationComputationName;
            public string packContainerId;
            public int packLocation;
        }

        public enum State{
            Idle,  // Production Cycle is started.  Queue is empty. System is in Idle
            Start, // Queue has order, start to deal with new order.
            Running,  // Order from start state meet the condition , start to run order
            Finish, // Order Finished. Publish orderFinishCode to high level system and wait for return to continue.
            Stop  // production cycle stopped. Waiting for startProductionCycle to be true.
            // order cycle finish code
        }

        public PLCProductionCycle(PLCMemory plcMemory) {
            this.plcMemory = plcMemory;
            this.maxLocationIndex = 4;
            this.queueCapacity = 20;
        }
        
        public void Start() {
            this.thread = new Thread(new ThreadStart(this.RunThread));
            this.thread.Start();
        }
        /// <summary>
        /// Extract order info from PLC memory and fullfill it into an internal order structure.
        /// </summary>
        private Order GetOrderFromPLCMemory(PLCController plcController, int locationIndex, int queueIndex){
            string locationQueueOrderPrefix = String.Format("location{0}Queue{1}", locationIndex, queueIndex);
            // pick and place related info
            Order order = new Order();
            order.orderPickLocation = (int)plcMemory.Get(String.Format("{0}PickLocation", locationQueueOrderPrefix), 0);
            order.orderPlaceLocation = (int)plcMemory.Get(String.Format("{0}PlaceLocation", locationQueueOrderPrefix), 0);
            order.orderPickContainerId = (string)plcMemory.Get(String.Format("{0}PickContainerId", locationQueueOrderPrefix), "");
            order.orderPlaceContainerId = (string)plcController.Get(String.Format("{0}PlaceContainerId", locationQueueOrderPrefix), "");
            order.orderUniqueId = (string)plcController.Get(String.Format("{0}UniqueId", locationQueueOrderPrefix), "");
            // TODO: preparation and pack
            return order;
        }

        /// <summary>
        /// Extract A list orders by given locationIndex, it will get each order from GetOrderFromPLCMemory.
        /// </summary>
        private List<Order> GetOrdersFromLocation(PLCController plcController, int locationIndex){
            string locationQueuePrefix = String.Format("location{0}Queue", locationIndex);
            int headIndex = (int)Convert.ToUInt16(plcController.Get(String.Format("{0}HeadIndex", locationQueuePrefix), 0)) - 1;
            int tailIndex = (int)Convert.ToUInt16(plcController.Get(String.Format("{0}TailIndex", locationQueuePrefix), 0)) - 1;

            List<Order> orderList = new List<Order>();
            if(headIndex == tailIndex){
                return orderList;
            }
            for(int index=headIndex;;index=(index+1)%this.queueCapacity){
                if(index == tailIndex){
                    break;
                }
                orderList.Add(this.GetOrderFromPLCMemory(plcController, locationIndex, index));
            }
            return orderList;
        }
        /// <summary>
        ///   Get next order to execute.
        /// </summary>
        private Order GetNextOrder(PLCController plcController, Order prevOrder){
            List<Order> candidates = new List<Order>();
            // selecte all head order from locations
            for (int locationIndex = 1; locationIndex <= this.maxLocationIndex; locationIndex++) {
                List<Order> orders = this.GetOrdersFromLocation(plcController, locationIndex);
                if(orders.Count > 0){
                    candidates.Add(orders[0]);
                }
            }

            // select one from candidates
            foreach(Order order in candidates){
                if(order.orderPickLocation == prevOrder.orderPickLocation ){
                    return order;
                }
                if(order.orderPlaceLocation == prevOrder.orderPlaceLocation){
                    return order;
                }
            }

            // return the first one
            if(candidates.Count > 0){
                return candidates[0];
            }
            // no orders in queue, return empty Order;
            return new Order();
        }

        private void DequeueOrder(PLCController plcController, int locationIndex){
            int headIndex = (int)Convert.ToUInt16(plcController.Get(String.Format("location{0}QueueHeadIndex", locationIndex), 0));
            plcController.Set(new Dictionary<string, object>(){
                    {String.Format("location{0}QueueHeadIndex", locationIndex), headIndex+1}
                });
            return;
        }

        private void RunThread() {
            PLCController plcController = new PLCController(this.plcMemory);
            PLCLogic plcLogic = new PLCLogic(plcController);

            bool isRunningProductionCycle = false;
            plcController.Set(new Dictionary<string, object>() {
                { "isRunningProductionCycle", isRunningProductionCycle },
            });


            State state = State.Stop;
            while (true) {
                plcController.Sync();

                Order nextOrder = new Order();
                Order prevOrder = new Order();
                switch(state)
                {
                    case State.Stop:
                        plcController.WaitUntil(new Dictionary<string, object>() {
                                { "startProductionCycle", true },
                                { "stopProductionCycle", false },
                                    });
                        // now we are running
                        state = State.Idle;
                        plcController.Set(new Dictionary<string, object>() {
                                { "isRunningProductionCycle", isRunningProductionCycle },
                                    });
                        break;
                    case State.Idle:
                        // check if stopProductionCycle is True
                        if (plcController.Get<bool>("stopProductionCycle", false)) {
                            // before we stop the system, need to stop current order and preparation
                            // TODO: think about timeout, and stop immediately if took too long
                            plcLogic.StopOrderCycle();
                            plcLogic.StopPreparationCycle();
                            // stop the production cycle
                            state = State.Stop;
                            plcController.Set(new Dictionary<string, object>() {
                                    { "isRunningProductionCycle", isRunningProductionCycle },
                                        });
                        }
                        // try to get order from Queue
                        // get combined order . the first one is next order to execute preparationcycle or ordercycle.
                        nextOrder = this.GetNextOrder(plcController, prevOrder);
                        if(nextOrder.orderUniqueId == ""){
                            // no order in queue
                            continue;
                        }
                        else{
                            // request to change location
                            string srcContainerId = plcController.Get(String.Format("location{0}PickContainerId", nextOrder.orderPickLocation), "");
                            string destContainerId = plcController.Get(String.Format("location{1}PlaceContainerId", nextOrder.orderPlaceLocation), "");

                            if(srcContainerId != nextOrder.orderPickContainerId){
                                // move source location
                                plcController.Set(new Dictionary<string, object>(){
                                        {String.Format("location{0}MoveRequest", nextOrder.orderPickLocation), true},

                                    });
                                plcController.WaitUntil(new Dictionary<string, object>(){
                                        {String.Format("location{0}Moving", nextOrder.orderPickLocation), true}
                                    });
                                plcController.Set(new Dictionary<string, object>(){
                                        {String.Format("location{0}MoveRequest", nextOrder.orderPickLocation), false},
                                        {String.Format("location{0}ExpectedContainerId", nextOrder.orderPickContainerId), ""},
                                        {String.Format("location{0}ExpectedContainerType", nextOrder.orderPickContainerType), ""},

                                            });
                            }
                            if(destContainerId != nextOrder.orderPlaceContainerId){
                                // move dest location
                                plcController.Set(new Dictionary<string, object>(){
                                        {String.Format("location{0}MoveRequest", nextOrder.orderPlaceLocation), true},

                                    });
                                plcController.WaitUntil(new Dictionary<string, object>(){
                                        {String.Format("location{0}Moving", nextOrder.orderPlaceLocation), true}
                                    });
                                plcController.Set(new Dictionary<string, object>(){
                                        {String.Format("location{0}MoveRequest", nextOrder.orderPlaceLocation), false},
                                        {String.Format("location{0}ExpectedContainerId", nextOrder.orderPlaceContainerId), ""},
                                        {String.Format("location{0}ExpectedContainerType", nextOrder.orderPlaceContainerType), ""},
                                    });
                            }
                            state = State.Start;
                        }
                        break;
                    case State.Start:
                        // TODO: check if condition meet for nextOrder to running
                        plcController.WaitUntil(new Dictionary<string, object>()
                                                     {
                                                         {"isModeAuto", true},
                                                         {"isSystemReady", true},
                                                         {"isCycleReady", true},
                                                         {"isRobotMoving", false}
                                                     });

                        // wait for location.
                        plcController.WaitUntil(new Dictionary<string, object>(){
                                {String.Format("location{0}Moving", nextOrder.orderPickLocation), false},
                                {String.Format("location{0}Moving", nextOrder.orderPlaceLocation), false},
                                {String.Format("location{0}Prohibited", nextOrder.orderPickLocation), false},
                                {String.Format("location{0}Prohibited", nextOrder.orderPlaceLocation), false},
                            });
                        // start order cycle
                        plcController.Set(new Dictionary<string, object>(){
                                {"orderPartType", nextOrder.orderPartType},
                                {"orderNumber", nextOrder.orderNumber},
                                {"orderPickLocationIndex", nextOrder.orderPickLocation},
                                {"orderPlaceLocationIndex", nextOrder.orderPlaceLocation},
                                {"orderPickContainerId", nextOrder.orderPickContainerId},
                                {"orderPlaceContainerId", nextOrder.orderPlaceContainerId},
                                {"startOrderCycle", true}
                            });
                        plcController.WaitUntil(new Dictionary<string, object>(){
                                {"isRunningOrderCycle", true}
                            });
                        state = State.Running;
                        break;
                    case State.Running:
                        plcController.WaitUntil(new Dictionary<string, object>(){
                                {"isRunningOrderCycle", false},
                            });
                        state = State.Finish;
                        break;
                    case State.Finish:
                        plcController.Set(new Dictionary<string, object>(){
                                {"orderCycleFinishCodeRequest", true},
                                {"orderUniqueId", nextOrder.orderUniqueId}
                            });
                        plcController.WaitUntil(new Dictionary<string, object>(){
                                {"orderCycleFinishCodeConfirmed", true}
                            });
                        plcController.Set(new Dictionary<string, object>(){
                                {"orderCycleFinishCodeConfirmed", false}
                            });

                        this.DequeueOrder(plcController, nextOrder.orderPickLocation);
                        prevOrder = nextOrder;
                        state = State.Idle;
                        break;
                }
                // if we are running order cycle
                // we need to monitor finish code, error, num placed
                // otherwise, not running order cycle
                // we need to tell conveyor to move either away or to next container, this can happen when order cycle is running on other locations
                // if customer dequeued, then we can start next cycle


                // when to startOrderCycle
                // 1. isRunningOrderCycle is false
                // 2. location condition met:
                //    a. HeadIndex is not yet processed
                //    b. location status (containerId, containerType, prohibited) matches order expection
                // 3. preparation is finished (unless we need to cancel the running preparation)
                // when multiple locations can be picked:
                // - run the one that is already prepared on controller
                // - randomly pick one to run

                // when to startPreparationCycle
                // 1. isRunningPreparationCycle is false
                // 2. for now, preparation result is used (might have case where we have to waste the previous preparation)
                // 3. location HeadIndex is not yet processed, but status do not need to match order expection
                // when multiple locations met this condition:
                // - run the one with dest location condition met

                // alternatively, we should combine all location queues
                // and create a stable order sequence
                // then it is easy to manage which order is to be run, which order is to be prepared

                // TODO:
                // - have a function that looks at current memory, returns the stable order sequence
                // - we also need to know current running order (question is do we need to save additional signal?)
                // - we also need to know current running preparation (do we need addigional signal to track?)

                // when to move conveyor for a location
                // 1. HeadIndex is processed, sometimes, no movement is required for next order
                // 2. need to also look at dest location as well, but dest should only move after order finished
                // so
                // - just look at the per location queue to move
                // - if no more order, we still need tell conveyor to move away
                // - if there is order, obviously tell conveyor to advance to that container
            }
        }
    }
}
