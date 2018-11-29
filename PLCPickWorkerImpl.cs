using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace mujinplccs
{
    class PickWorker : IPickWorker {
        private int maxLocationIndex;
        private int queueCapacity;
        private PLCMemory plcMemory;
        private PLCController plcController;
        private PLCLogic plcLogic;
        private IMaterialHandler materialHandler;

        private Thread orderCycleFinishThread;
        private List<Thread> locationMoveThreads;

        public PickWorker(PLCMemory plcMemory, IMaterialHandler materialHandler) {
            this.plcMemory = plcMemory;
            this.plcController = new PLCController(this.plcMemory);
            this.plcLogic = new PLCLogic(this.plcController);
            this.materialHandler = materialHandler;
            this.queueCapacity = 20;
            this.maxLocationIndex = 4;
            this.locationMoveThreads = new List<Thread>();

            this.ClearAllSignals(); // for now call it automatically

            // start thread here to monitor the plcsignal
            // create new plccontroller
            PLCController plcController;

            for(int locationIndex=1; locationIndex<=this.maxLocationIndex; locationIndex+=1){
                plcController = new PLCController(this.plcMemory);
                Thread thread = new Thread(()=>this.RunLocationMonitor(locationIndex, plcController));
                thread.Start();
                this.locationMoveThreads.Add(thread);

            }
            // Monitor for orderFinishCode
            plcController = new PLCController(this.plcMemory);
            this.orderCycleFinishThread = new Thread(()=>this.RunOrderCycleMonitor(plcController));
            this.orderCycleFinishThread.Start();
        }

        private void RunOrderCycleMonitor(PLCController plcController){
            plcController.WaitUntil(new Dictionary<string, object>(){
                    {"orderCycleFinishCodeRequest", true}
                });
            plcController.Set(new Dictionary<string, object>(){
                    {"orderCycleFinishCodeRequest", false}
                });
            PLCLogic.PLCOrderCycleFinishCode orderFinishCode = plcController.Get("orderCyeleFinishCode", PLCLogic.PLCOrderCycleFinishCode.FinishedNotAvailable);
            string uniqueId = plcController.Get("orderUniqueId", "");
            Task task = this.materialHandler.FinishOrderAsync(uniqueId, orderFinishCode);
            task.Wait(); // wait until server confirm the order info

            plcController.Set(new Dictionary<string, object>(){
                    {"orderCycleFinishCodeConfirmed", true}
                });
        }
        private void RunLocationMonitor(int locationIndex, PLCController plcController){

            plcController.WaitUntil(new Dictionary<string, object>(){
                    {String.Format("location{0}MoveRequest", locationIndex), true},
                });
            int headIndex = (int)plcController.Get(String.Format("location{0}QueueHeadIndex", locationIndex), -1);
            string containerId = plcController.Get(String.Format("location{0}ExpectedContainerId", locationIndex, headIndex), "");
            string containerType = plcController.Get(String.Format("location{0}ExpectedContainerType", locationIndex, headIndex), "");
            Task task = this.materialHandler.MoveLocationAsync(locationIndex, containerId, containerType);
            
            plcController.Set(new Dictionary<string, object>(){
                    {String.Format("location{0}Moving", locationIndex), true}
                });
            task.Wait();
            plcController.Set(new Dictionary<string, object>(){
                    {String.Format("location{0}Moving", locationIndex), false}
                });
        }

        /// <summary>
        /// Clear all signals to the MUJIN controller. Set them all to false.
        /// </summary>
        private void ClearAllSignals()
        {
            // initialize the queue signals
            Dictionary<string, object> signals = new Dictionary<string, object>() {
                { "startProductionCycle", false },
                { "stopProductionCycle", false },
            };
            for (int locationIndex = 1; locationIndex <= this.maxLocationIndex; locationIndex++) {
                string locationQueuePrefix = String.Format("location{0}Queue", locationIndex);
                signals[String.Format("{0}HeadIndex", locationQueuePrefix)] = -1;
                signals[String.Format("{0}TailIndex", locationQueuePrefix)] = 0;
                for (int index = 0; index < this.queueCapacity; index++) {
                    string locationQueueOrderPrefix = String.Format("location{0}Queue{1}", locationIndex, index);
                    signals[String.Format("{0}UniqueId", locationQueueOrderPrefix)] = "";
                    signals[String.Format("{0}PartType", locationQueueOrderPrefix)] = "";
                    signals[String.Format("{0}OrderNumber", locationQueueOrderPrefix)] = 0;
                    signals[String.Format("{0}PickLocationIndex", locationQueueOrderPrefix)] = 0;
                    signals[String.Format("{0}PickContainerId", locationQueueOrderPrefix)] = "";
                    signals[String.Format("{0}PickContainerType", locationQueueOrderPrefix)] = "";
                    signals[String.Format("{0}PlaceLocationIndex", locationQueueOrderPrefix)] = 0;
                    signals[String.Format("{0}PlaceContainerId", locationQueueOrderPrefix)] = "";
                    signals[String.Format("{0}PlaceContainerType", locationQueueOrderPrefix)] = "";
                    signals[String.Format("{0}PackInputPartIndex", locationQueueOrderPrefix)] = 0;
                    signals[String.Format("{0}PackFormationComputationName", locationQueueOrderPrefix)] = "";
                }
            }
            this.plcController.Set(signals);
        }

        // queue an order, note that the order should be queued according to the container arrival sequence at the pickLocation
        public void QueueOrder(string orderUniqueId, PickWorkerQueueOrderParameters queueOrderParameters) {
            // insert to the tail of the queue
            // TODO: need to check uniqueness of orderUniqueId
            string locationQueuePrefix = String.Format("location{0}Queue", queueOrderParameters.pickLocationIndex);
            int headIndex = this.plcController.SyncAndGet<int>(String.Format("{0}HeadIndex", locationQueuePrefix), -1);
            int tailIndex = this.plcController.SyncAndGet<int>(String.Format("{0}TailIndex", locationQueuePrefix), 0);
            int nextTailIndex = (tailIndex) % this.queueCapacity;
            if (nextTailIndex == headIndex) {
                // we are full
                throw new OverflowException("queue is full");
            }
            string locationQueueOrderPrefix = String.Format("location{0}Queue{1}", queueOrderParameters.pickLocationIndex, tailIndex);
            this.plcController.Set(new Dictionary<string, object>() {
                { String.Format("{0}UniqueId", locationQueueOrderPrefix), orderUniqueId },
                { String.Format("{0}PartType", locationQueueOrderPrefix), queueOrderParameters.partType },
                { String.Format("{0}OrderNumber", locationQueueOrderPrefix), queueOrderParameters.orderNumber },
                { String.Format("{0}PickLocationIndex", locationQueueOrderPrefix), queueOrderParameters.pickLocationIndex },
                { String.Format("{0}PickContainerId", locationQueueOrderPrefix), queueOrderParameters.pickContainerId },
                { String.Format("{0}PickContainerType", locationQueueOrderPrefix), queueOrderParameters.pickContainerType },
                { String.Format("{0}PlaceLocationIndex", locationQueueOrderPrefix), queueOrderParameters.placeLocationIndex },
                { String.Format("{0}PlaceContainerId", locationQueueOrderPrefix), queueOrderParameters.placeContainerId },
                { String.Format("{0}PlaceContainerType", locationQueueOrderPrefix), queueOrderParameters.placeContainerType },
                { String.Format("{0}PackInputPartIndex", locationQueueOrderPrefix), queueOrderParameters.packInputPartIndex },
                { String.Format("{0}PackFormationComputationName", locationQueueOrderPrefix), queueOrderParameters.packFormationComputationName },
                { String.Format("{0}TailIndex", locationQueuePrefix), tailIndex + 1 },
            });
            if(headIndex == -1){
                // first order
                this.plcController.Set(new Dictionary<string, object>() {
                        {String.Format("{0}HeadIndex", locationQueuePrefix), 0}
                    });
            }
        }

        // // low level signals

        // // stop immediately
        // public void StopImmediately(TimeSpan? timeout = null) {
        //     this.plcLogic.StopImmediately(timeout);
        // }

        // // pause and resume
        // public void Pause() {
        //     this.plcController.Set(new Dictionary<string, object>() {
        //         { "resume", false },
        //         { "pause", true },
        //     });

        // }
        // public void Resume() {
        //     this.plcController.Set(new Dictionary<string, object>() {
        //         { "resume", true },
        //         { "pause", false },
        //     });
        // }
    }

    public class PickWorkerFactory : IPickWorkerFactory {
        public IPickWorker CreatePickWorker(PLCMemory plcMemory, IMaterialHandler materialHandler) {
            return new PickWorker(plcMemory, materialHandler);
        }

        public static IPickWorkerFactory Instance() {
            return instance;
        }

        private static PickWorkerFactory instance = new PickWorkerFactory();
    }
}
