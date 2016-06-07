using System;
using System.Collections.Generic;
using System.Threading;

namespace mujinplccs
{

    public static class Extensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }

    public sealed class PLCLogic
    {
        public enum PLCErrorCode
        {
            ErrorCodeNotAvailable = 0x0000,
            EStopError = 0x1000,
            PLCError = 0x2000,
            PLCSupplyInterlockError = 0x2001,
            PLCDestInterlockError = 0x2002,
            PLCOtherInterlockError = 0x2003,
            PLCCommandError = 0x2010,
            PlanningError = 0x3000,
            DetectionError = 0x4000,
            SensorError = 0x5000,
            RobotError = 0x6000,
            SystemError = 0x7000,
            OtherCycleError = 0xf000,
            InCycleError = 0xf001,
            InvalidOrderNumberError = 0xf003,
            NotRunningError = 0xf004,
            FailedToMoveToError = 0xf009,
            GenericError = 0xffff,
        }

        public sealed class PLCError : PLCException
        {
            private PLCErrorCode errorCode = PLCErrorCode.ErrorCodeNotAvailable;
            private int detailedErrorCode = 0;

            public PLCError(PLCErrorCode errorCode, int detailedErrorCode = 0) : base("plc_error", String.Format("An error has occurred: {0}", errorCode))
            {
                this.errorCode = errorCode;
                this.detailedErrorCode = detailedErrorCode;
            }

            public PLCErrorCode ErrorCode
            {
                get { return this.errorCode; }
            }

            public int DetailedErrorCode
            {
                get { return this.detailedErrorCode; }
            }
        }

        public enum PLCOrderCycleFinishCode
        {
            FinishedNotAvailable = 0x0000,
            FinishedOrderComplete = 0x0001,
            FinishedNoMoreTargets = 0x0002,
            FinishedNoMoreTargetsNotEmpty = 0x0003,
            FinishedNoMoreDest = 0x0004,
            FinishedNoEnvironmentUpdate = 0x0005,
            FinishedDropTargetFailure = 0x0006,
            FinishedTooManyPickFailures = 0x0007,
            FinishedCommandDisabled = 0x0100,
            FinishedStopped = 0x0101,
            FinishedStoppedImmediately = 0x0102,
            FinishedPlanningFailure = 0x0100,
            FinishedGenericFailure = 0xffff,
        }

        public sealed class PLCOrderCycleStatus
        {
            public bool IsRunningOrderCycle { get; set; }
            public bool IsRobotMoving { get; set; }
            public bool IsSupplyDetectionRunning { get; set; }
            public int NumLeftInOrder { get; set; }
            public int NumLeftInSupply { get; set; }
            public PLCOrderCycleFinishCode OrderCycleFinishCode { get; set; }
            public bool CanChangeSupplyContainer { get; set; }
            public bool CanChangeDestContainer { get; set; }
            public bool SupplyContainerNotEmpty { get; set; }
            public bool DestContainerFull { get; set; }
        }

        private PLCController controller = null;

        public PLCLogic(PLCController controller)
        {
            this.controller = controller;
        }

        public void ClearSignals()
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "startOrderCycle", false },
                { "stopOrderCycle", false },
                { "stopImmediately", false },
                { "startPreparation", false },
                { "stopPreparation", false },
                { "startMoveToHome", false },
                { "startDetection", false },
                { "stopDetection", false },
                { "stopGripper", false },
                { "resetError", false },
            });
        }

        private void _CheckError()
        {
            if (this.controller.Get("isError", false).Equals(true))
            {
                var errorcode = (PLCErrorCode)Convert.ToInt32(this.controller.Get("errorcode", 0));
                var detailedErrorCode = Convert.ToInt32(this.controller.Get("detailedErrorCode", 0));
                throw new PLCError(errorcode, detailedErrorCode);
            }
        }

        public void ResetError(TimeSpan? timeout = null)
        {
            this.controller.Set("resetError", true);
            this.controller.WaitUntil("isError", false, timeout);
            this.controller.Set("resetError", false);
        }

        public PLCOrderCycleStatus StartOrderCycle(string orderId, string partType, int orderNumber, TimeSpan? timeout = null)
        {
            this.controller.WaitUntil(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", false },
                { "isRobotMoving", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            }, new Dictionary<string, object>()
            {
                { "isError", true },
            }, timeout);
            this._CheckError();

            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", true },
                { "orderId", orderId },
                { "partType", orderId },
                { "orderNumber", orderId },
                { "startOrderCycle", true },
                { "supplyContainerChanging", false },
                { "destContainerChanging", false },
            });
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRunningOrderCycle", true },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.controller.Set("startOrderCycle", false);
            this._CheckError();
            this.controller.Sync();
            return this.GetOrderCycleStatus();
        }

        public PLCOrderCycleFinishCode GetOrderCycleFinishCode()
        {
            return (PLCOrderCycleFinishCode)Convert.ToInt32(this.controller.Get("orderCycleFinishCode", 0));
        }

        public PLCOrderCycleStatus GetOrderCycleStatus(TimeSpan? timeout = null)
        {
            var values = this.controller.Get(new string[]
            {
                "isRunningOrderCycle",
                "isRobotMoving",
                "isSupplyDetectionRunning",
                "numLeftInOrder",
                "numLeftInSupply",
                "orderCycleFinishCode",
                "canChangeSupplyContainer",
                "canChangeDestContainer",
                "supplyContainerNotEmpty",
                "destContainerFull",
            });

            return new PLCOrderCycleStatus
            {
                IsRunningOrderCycle = Convert.ToBoolean(values.GetValueOrDefault("isRunningOrderCycle", false)),
                IsRobotMoving = Convert.ToBoolean(values.GetValueOrDefault("isRobotMoving", false)),
                IsSupplyDetectionRunning = Convert.ToBoolean(values.GetValueOrDefault("isSupplyDetectionRunning", false)),
                NumLeftInOrder = Convert.ToInt32(values.GetValueOrDefault("numLeftInOrder", 0)),
                NumLeftInSupply = Convert.ToInt32(values.GetValueOrDefault("numLeftInSupply", 0)),
                OrderCycleFinishCode = (PLCOrderCycleFinishCode)Convert.ToInt32(values.GetValueOrDefault("orderCycleFinishCode", 0)),
                CanChangeSupplyContainer = Convert.ToBoolean(values.GetValueOrDefault("canChangeSupplyContainer", false)),
                CanChangeDestContainer = Convert.ToBoolean(values.GetValueOrDefault("canChangeDestContainer", false)),
                SupplyContainerNotEmpty = Convert.ToBoolean(values.GetValueOrDefault("supplyContainerNotEmpty", false)),
                DestContainerFull = Convert.ToBoolean(values.GetValueOrDefault("destContainerFull", false)),
            };
        }

        public PLCOrderCycleStatus CheckOrderCycleStatus(TimeSpan? timeout = null)
        {
            this.controller.Sync();
            if (this.controller.Get("isRunningOrderCycle", false).Equals(true))
            {
                this.controller.WaitFor(new Dictionary<string, object>()
                {
                    { "isError", true },

                    // listen to any changes in the following addresses
                    { "isRunningOrderCycle", null },
                    { "isRobotMoving", null },
                    { "isSupplyDetectionRunning", null },
                    { "numLeftInOrder", null },
                    { "numLeftInSupply", null },
                    { "orderCycleFinishCode", null },
                    { "canChangeSupplyContainer", null },
                    { "canChangeDestContainer", null },
                    { "supplyContainerNotEmpty", null },
                    { "destContainerFull", null },
                }, timeout);
                this.controller.Sync();
            }
            this._CheckError();
            return this.GetOrderCycleStatus();
        }

        public PLCOrderCycleFinishCode StopOrderCycle(TimeSpan? timeout = null)
        {
            this.controller.Set("stopOrderCycle", true);
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRunningOrderCycle", false },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.controller.Set("stopOrderCycle", false);
            this._CheckError();
            return this.GetOrderCycleFinishCode();
        }

        public PLCOrderCycleFinishCode StopImmediately(TimeSpan? timeout = null)
        {
            this.controller.Set("stopImmediately", true);
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRunningOrderCycle", false },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.controller.Set("stopImmediately", false);
            this._CheckError();
            return this.GetOrderCycleFinishCode();
        }

        public void StartPreparation(string orderId, string partType, int orderNumber, TimeSpan? timeout = null)
        {

        }

        public void StopPreparation(TimeSpan? timeout = null)
        {
            this.controller.Set("stopPreparation", true);
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRunningOrderCycle", false },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.controller.Set("stopPreparation", false);
            this._CheckError();
        }

        public void MoveToHome(TimeSpan? timeout = null)
        {
            this.controller.WaitUntil(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", false },
                { "isRobotMoving", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            }, new Dictionary<string, object>()
            {
                { "isError", true },
            }, timeout);
            this._CheckError();

            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", true },
                { "startMoveToHome", true },
            });
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRobotMoving", true },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.controller.Set("startMoveToHome", false);
            this._CheckError();
        }

        public void StartDetection(TimeSpan? timeout = null)
        {

        }

        public void StopDetection(TimeSpan? timeout = null)
        {
            this.controller.Set("stopDetection", true);
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isSupplyDetectionRunning", false },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.controller.Set("stopDetection", false);
            this._CheckError();
        }

        public void StopGripper(TimeSpan? timeout = null)
        {
            this.controller.Set("stopGripper", true);
            Thread.Sleep(1);
            this.controller.Set("stopGripper", false);
            this.controller.Sync();
            this._CheckError();
        }
    }
}
