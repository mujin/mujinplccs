﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace mujinplccs
{
    public static class Extensions
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }

    public sealed class PLCLogic
    {
        /// <summary>
        /// MUJIN PLC ErrorCode
        /// </summary>
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

        /// <summary>
        /// PLCError is raised when an error code is set by MUJIN controller.
        /// </summary>
        public sealed class PLCError : Exception
        {
            private PLCErrorCode _errorcode = PLCErrorCode.ErrorCodeNotAvailable;
            private string _detailedErrorCode;

            public PLCError(PLCErrorCode errorcode, string detailedErrorCode="") : base(String.Format("Error 0x{0:X} has occurred: {1}", errorcode, detailedErrorCode))
            {
                this._errorcode = errorcode;
                this._detailedErrorCode = detailedErrorCode;
            }

            /// <summary>
            /// MUJIN PLC Error Code
            /// </summary>
            public PLCErrorCode errorcode
            {
                get { return this._errorcode; }
            }

            /// <summary>
            /// When ErrorCode is RobotError, DetailedErrorCode contains the error code returned by robot controller.
            /// </summary>
            public string detailedErrorCode
            {
                get { return this._detailedErrorCode; }
            }
        }

        /// <summary>
        /// MUJIN PLC Preparation FinishCode
        /// </summary>

        public enum PLCPreparationFinishCode
        {
            PreparationNotAvailable = 0x0000,
            PreparationFinishedSuccess = 0x0001,
            PreparationFinishedImmediatelyStopped = 0x0102,
            PreparationFinishedBadPartType = 0xfffd,
            PreparationFinishedBadOrderCyclePrecondition = 0xfffe,
            PreparationFinishedGenericError = 0xffff,
        }

        /// <summary>
        /// MUJIN PLC OrderCycleFinishCode
        /// </summary>
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
            FinishedRobotExecutionError = 0x0008,
            FinishedNoDestObstacles = 0x0009,
            FinishedStopped = 0x0101,
            FinishedStoppedImmediately = 0x0102,
            FinishedPlanningFailure = 0x1000,
            FinishedNoValidGrasp = 0x1001,
            FinishedNoValidDest = 0x1002,
            FinishedNoValidGraspDestPair = 0x1003,
            FinishedNoValidPath = 0x1004,
            FinishedNoValidTargets = 0x1005,
            FinishedNoValidBarcodeScan = 0x1006,
            FinishedComputePlanFailure = 0x1007,
            FinishedCannotGenerateGraspingModel = 0x1008,
            FinishedContainerNotDetected = 0x2001,
            FinishedPlaceContainerNotDetected = 0x2002,
            FinishedBadExpectedDetectionHeight = 0x2003,
            FinishedCannotComputeFinishPlan = 0xfff7,
            FinishedUnknownReasonNoError = 0xfff8,
            FinishedCannotGetState = 0xfff9,
            FinishedCycleStopCanceled = 0xfffa,
            FinishedDropOffIsOn = 0xfffb,
            FinishedBadPartType = 0xfffd,
            FinishedBadOrderCyclePrecondition = 0xfffe,
            FinishedGenericFailure = 0xffff,
        }

        /// <summary>
        /// Mujin Pack Composition Finish Codes
        /// </summary>
        public enum PackCompositionFinishCodes
        {
            FinishedPackingUnknown = 0x0000,
            FinishedPackingSuccess = 0x0001,
            FinishedPackingInvalid = 0x0002,
            FinishedPackingStopped = 0x0102,
            FinishedCannotGetState = 0xfff9,
            FinishedBadOrderCyclePrecondition = 0xfffe,
            FinishedPackingError = 0xffff,
        }

        /// <summary>
        /// MUJIN order cycle status information.
        /// </summary>
        public sealed class PLCOrderCycleStatus
        {
            /// <summary>
            /// Whether the order cycle is currently running.
            /// </summary>
            public bool isRunningOrderCycle { get; set; }

            /// <summary>
            /// Whether the robot is currently moving.
            /// </summary>
            public bool isRobotMoving { get; set; }

            /// <summary>
            /// Whether detection is currently running.
            /// </summary>
            public bool location1DetectionRunning { get; set; }

            /// <summary>
            /// Number of items left in order to be picked.
            /// </summary>
            public int numLeftInOrder { get; set; }

            /// <summary>
            /// Number of items placed.
            /// </summary>
            public int numPutInDestination { get; set; }

            /// <summary>
            /// MUJIN PLC OrderCycleFinishCode.
            /// </summary>
            public PLCOrderCycleFinishCode orderCycleFinishCode { get; set; }

            /// <summary>
            /// Whether container at location can be safely moved at the moment.
            /// </summary>
            public bool location1Released { get; set; }

            /// <summary>
            /// Whether container at location can be safely moved at the moment.
            /// </summary>
            public bool location2Released { get; set; }

            /// <summary>
            /// Whether container at location can be safely moved at the moment.
            /// </summary>
            public bool location3Released { get; set; }

            /// <summary>
            /// Whether container at location can be safely moved at the moment.
            /// </summary>
            public bool location4Released { get; set; }

            /// <summary>
            /// Whether container at location is currently empty.
            /// </summary>
            public bool location1NotEmpty { get; set; }

            /// <summary>
            /// Whether container at location is currently empty.
            /// </summary>
            public bool location2NotEmpty { get; set; }

            /// <summary>
            /// Whether container at location is currently empty.
            /// </summary>
            public bool location3NotEmpty { get; set; }

            /// <summary>
            /// Whether container at location is currently empty.
            /// </summary>
            public bool location4NotEmpty { get; set; }

            /// <summary>
            /// Number of detected work parts detected and left in location.
            /// </summary>
            public int numLeftInLocation1 { get; set; }

            /// <summary>
            /// Number of detected work parts detected and left in location.
            /// </summary>
            public int numLeftInLocation2 { get; set; }

            /// <summary>
            /// Number of detected work parts detected and left in location.
            /// </summary>
            public int numLeftInLocation3 { get; set; }

            /// <summary>
            /// Number of detected work parts detected and left in location.
            /// </summary>
            public int numLeftInLocation4 { get; set; }
        }

        /// <summary>
        ///   Mujin Preparation Cycle status
        /// </summary>
        public sealed class PLCPreparationCycleStatus
        {
            /// <summary>
            /// Whether the preparation cycle is currently running
            /// </summary>
            public bool isRunningPreparation { get; set; }
        }

        private PLCController controller = null;

        /// <summary>
        /// MUJIN specific PLC logic implementation.
        /// </summary>
        /// <param name="controller">An instance of PLCController.</param>
        public PLCLogic(PLCController controller)
        {
            this.controller = controller;
        }

        /// <summary>
        /// Clear all signals to the MUJIN controller. Set them all to false.
        /// </summary>
        public void ClearAllSignals()
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

        /// <summary>
        /// Block until connection from MUJIN controller is detected.
        /// </summary>
        /// <param name="timeout"></param>
        public void WaitUntilConnected(TimeSpan? timeout = null)
        {
            this.controller.WaitUntilConnected(timeout);
        }

        /// <summary>
        /// Check if there is an error set by MUJIN controller in the current state. If so, raise a PLCError exception. 
        /// </summary>
        public void CheckError()
        {
            if (this.controller.Get<bool>("isError", false))
            {
                var errorcode = (PLCErrorCode)this.controller.Get<int>("errorcode", 0);
                var detailedErrorCode = this.controller.Get<string>("detailedErrorCode", "");
                throw new PLCError(errorcode, detailedErrorCode);
            }
        }

        public bool IsError()
        {
            return this.controller.SyncAndGet<bool>("isError", false);
        }

        /// <summary>
        /// Reset error on MUJIN controller. Block until error is reset.
        /// </summary>
        /// <param name="timeout"></param>
        public void ResetError(TimeSpan? timeout = null)
        {
            this.controller.Set("resetError", true);
            try
            {
                this.controller.WaitUntil("isError", false, timeout);
            }
            finally
            {
                this.controller.Set("resetError", false);
            }
        }

        public bool IsSystemReady()
        {
            return this.controller.SyncAndGet<bool>("isSystemReady", false);
        }

        public bool IsModeAuto()
        {
            return this.controller.SyncAndGet<bool>("isModeAuto", false);
        }

        public void WaitUntilAutoMode(TimeSpan? timeout = null)
        {
            this.controller.WaitUntil(new Dictionary<string, object>()
            {
                { "isModeAuto", true },
                { "isSystemReady", true },
            }, new Dictionary<string, object>()
            {
                { "isError", true },
            }, timeout);
            this.CheckError();
        }

        /// <summary>
        /// Block until MUJIN controller is ready to start order cycle.
        /// </summary>
        /// <param name="timeout"></param>
        public void WaitUntilOrderCycleReady(TimeSpan? timeout = null)
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
            this.CheckError();
        }

        /// <summary>
        /// Start order cycle. Block until MUJIN controller acknowledge the start command.
        /// </summary>
        /// <param name="orderPartType"></param>
        /// <param name="orderNumber"></param>
        /// <param name="orderPickLocationIndex"></param>
        /// <param name="orderPickContainerId"></param>
        /// <param name="orderPlaceLocationIndex"></param>
        /// <param name="orderPlaceContainerId"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        // public PLCOrderCycleStatus StartOrderCycle(string orderPartType, int orderNumber, int orderPickLocation, string orderPickContainerId, int orderPlaceLocation, string orderPlaceContainerId, TimeSpan? timeout = null)
        public PLCOrderCycleStatus StartOrderCycle(string orderPartType, int orderNumber, int orderPickLocationIndex, string orderPickContainerId, int orderPlaceLocationIndex, string orderPlaceContainerId, TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "orderPartType", orderPartType },
                { "orderNumber", orderNumber },
                { "orderPickLocation", orderPickLocationIndex },
                { "orderPlaceLocation", orderPlaceLocationIndex },
                { "orderPickContainerId", orderPickContainerId },
                { "orderPlaceContainerId", orderPlaceContainerId },
                { "startOrderCycle", true }
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "isRunningOrderCycle", true },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>() {
                    { "startOrderCycle", false },
                });
            }
            this.CheckError();
            return this.GetOrderCycleStatus();
        }

        /// <summary>
        /// Gather order cycle status information in the current state.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public PLCOrderCycleStatus GetOrderCycleStatus(TimeSpan? timeout = null)
        {
            var values = this.controller.SyncAndGet(new string[]
            {
                "isRunningOrderCycle",
                "isRobotMoving",
                "location1DetectionRunning",
                "numLeftInOrder",
                "numPutInDestination",
                "orderCycleFinishCode",
                "location1Released",
                "location2Released",
                "location3Released",
                "location4Released",
                "location1NotEmpty",
                "location2NotEmpty",
                "location3NotEmpty",
                "location4NotEmpty",
                "numLeftInLocation1",
                "numLeftInLocation2",
                "numLeftInLocation3",
                "numLeftInLocation4",
            });

            return new PLCOrderCycleStatus
            {
                isRunningOrderCycle = Convert.ToBoolean(values.Get("isRunningOrderCycle", false)),
                isRobotMoving = Convert.ToBoolean(values.Get("isRobotMoving", false)),
                location1DetectionRunning = Convert.ToBoolean(values.Get("location1DetectionRunning", false)),
                numLeftInOrder = Convert.ToInt32(values.Get("numLeftInOrder", 0)),
                numPutInDestination = Convert.ToInt32(values.Get("numPutInDestination", 0)),
                orderCycleFinishCode = (PLCOrderCycleFinishCode)Convert.ToInt32(values.Get("orderCycleFinishCode", 0)),
                location1Released = Convert.ToBoolean(values.Get("location1Released", false)),
                location2Released = Convert.ToBoolean(values.Get("location2Released", false)),
                location3Released = Convert.ToBoolean(values.Get("location3Released", false)),
                location4Released = Convert.ToBoolean(values.Get("location4Released", false)),
                location1NotEmpty = Convert.ToBoolean(values.Get("location1NotEmpty", false)),
                location2NotEmpty = Convert.ToBoolean(values.Get("location2NotEmpty", false)),
                location3NotEmpty = Convert.ToBoolean(values.Get("location3NotEmpty", false)),
                location4NotEmpty = Convert.ToBoolean(values.Get("location4NotEmpty", false)),
                numLeftInLocation1 = Convert.ToInt32(values.Get("numLeftInLocation1", 0)),
                numLeftInLocation2 = Convert.ToInt32(values.Get("numLeftInLocation2", 0)),
                numLeftInLocation3 = Convert.ToInt32(values.Get("numLeftInLocation3", 0)),
                numLeftInLocation4 = Convert.ToInt32(values.Get("numLeftInLocation4", 0)),
            };
        }

        /// <summary>
        /// Block until values in order cycle status changes.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>Order cycle status information in the current state.</returns>
        public PLCOrderCycleStatus WaitForOrderCycleStatusChange(TimeSpan? timeout = null)
        {
            if (this.controller.Get<bool>("isRunningOrderCycle", false))
            {
                this.controller.WaitFor(new Dictionary<string, object>()
                {
                    { "isError", true },

                    // listen to any changes in the following addresses
                    { "isRunningOrderCycle", null },
                    { "isRobotMoving", null },
                    { "location1DetectionRunning", null },
                    { "numLeftInOrder", null },
                    { "orderCycleFinishCode", null },
                    { "location1Released", null },
                    { "location2Released", null },
                    { "location3Released", null },
                    { "location4Released", null },
                    { "location1NotEmpty", null },
                    { "location2NotEmpty", null },
                    { "location3NotEmpty", null },
                    { "location4NotEmpty", null },
                    { "numLeftInLocation1", null },
                    { "numLeftInLocation2", null },
                    { "numLeftInLocation3", null },
                    { "numLeftInLocation4", null },
                }, timeout);
            }
            this.CheckError();
            return this.GetOrderCycleStatus();
        }

        /// <summary>
        /// Block until MUJIN controller finishes the order cycle.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>Order cycle status information in the finish state.</returns>
        public PLCOrderCycleStatus WaitUntilOrderCycleFinish(TimeSpan? timeout = null)
        {
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRunningOrderCycle", false },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.CheckError();
            return this.GetOrderCycleStatus();
        }

        /// <summary>
        /// Signal MUJIN controller to stop order cycle and block until it is stopped.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>Order cycle status information in the current state.</returns>
        public PLCOrderCycleStatus StopOrderCycle(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "stopOrderCycle", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "isRunningOrderCycle", false },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>() {
                    { "stopOrderCycle", false },
                });
            }
            this.CheckError();
            return this.GetOrderCycleStatus();
        }

        /// <summary>
        /// Stop the current operation on MUJIN controller immediately.
        /// </summary>
        /// <param name="timeout"></param>
        public void StopImmediately(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "stopImmediately", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "isRunningOrderCycle", false },
                    { "isRobotMoving", false },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>() {
                    { "stopImmediately", false },
                });
            }
            this.CheckError();
        }

        /// <summary>
        /// Block until MUJIN controller is ready to start preparation cycle.
        /// </summary>
        /// <param name="timeout"></param>
        public void WaitUntilPreparationCycleReady(TimeSpan? timeout = null)
        {
            this.controller.WaitUntil(new Dictionary<string, object>()
            {
                { "isRunningPreparation", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            }, new Dictionary<string, object>()
            {
                { "isError", true },
            }, timeout);
            this.CheckError();
        }

        /// <summary>
        /// Signal to start the preparation cycle on MUJIN controller.
        /// </summary>
        /// <param name="preparationPartType"></param>
        /// <param name="preparationOrderNumber"></param>
        /// <param name="preparationPickLocationIndex"></param>
        /// <param name="preparationPickContainerId"></param>
        /// <param name="preparationPlaceLocationIndex"></param>
        /// <param name="preparationPlaceContainerId"></param>
        /// <param name="timeout"></param>
        public PLCPreparationCycleStatus StartPreparationCycle(string preparationPartType, int preparationOrderNumber, int preparationPickLocationIndex, string preparationPickContainerId, int preparationPlaceLocationIndex, string preparationPlaceContainerId, TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "preparationPartType", preparationPartType },
                { "preparationOrderNumber", preparationOrderNumber },
                { "preparationPickLocation", preparationPickLocationIndex },
                { "preparationPlaceLocation", preparationPlaceLocationIndex },
                { "preparationPickContainerId", preparationPickContainerId },
                { "preparationPlaceContainerId", preparationPlaceContainerId },
                { "startPreparation", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>(){
                    { "isRunningPreparation", true },
                }, new Dictionary<string, object>(){
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>(){
                    { "startPreparation", false },
                });
            }
            this.CheckError();
            return this.GetPreparationCycleStatus();
        }


        /// <summary>
        /// Gather preparation cycle status information in the current state
        /// </summary>
        public PLCPreparationCycleStatus GetPreparationCycleStatus(TimeSpan? timeout = null)
        {
            var values = this.controller.SyncAndGet(new string[]
            {
                "isRunningPreparation",
            });

            return new PLCPreparationCycleStatus
            {
                isRunningPreparation = Convert.ToBoolean(values.Get("isRunningPreparation", false))
            };
        }

        /// <summary>
        /// Block until MUJIN controller finishes the preparation cycle
        /// </summary>
        public PLCPreparationCycleStatus WaitUntilPreparationCycleFinish(TimeSpan? timeout = null)
        {
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRunningPreparation", false },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.CheckError();
            return this.GetPreparationCycleStatus();
        }

        /// <summary>
        /// Signal to stop the preparation cycle on MUJIN controller.
        /// </summary>
        /// <param name="timeout"></param>
        public PLCPreparationCycleStatus StopPreparationCycle(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "stopPreparation", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "isRunningPreparation", false },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>() {
                    { "stopPreparation", false },
                });
            }
            this.CheckError();
            return this.GetPreparationCycleStatus();
        }

        /// <summary>
        /// Block until MUJIN controller is ready to move robot to home position.
        /// </summary>
        /// <param name="timeout"></param>
        public void WaitUntilMoveToHomeReady(TimeSpan? timeout = null)
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
            this.CheckError();
        }

        public bool IsAtHome()
        {
            return this.controller.SyncAndGet<bool>("isAtHome", false);
        }

        /// <summary>
        /// Signal MUJIN controller to move the robot to its home position. Block until the robot starts moving.
        /// </summary>
        /// <param name="timeout"></param>
        public void StartMoveToHome(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "startMoveToHome", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                   { "isRobotMoving", true },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>() {
                    { "startMoveToHome", false },
                });
            }
            this.CheckError();
        }

        /// <summary>
        /// Block until the robot moving state is expected.
        /// </summary>
        /// <param name="isRobotMoving">true if expecting robot to move, false if expecting robot to stop</param>
        /// <param name="timeout"></param>
        public void WaitUntilRobotMoving(bool isRobotMoving, TimeSpan? timeout = null)
        {
            this.controller.WaitUntil(new Dictionary<string, object>() {
                { "isRobotMoving", isRobotMoving },
            }, new Dictionary<string, object>() {
                { "isError", true },
            }, timeout);
            this.CheckError();
        }

        /// <summary>
        /// Signal MUJIN controller to start detection. Block until detection is running.
        /// </summary>
        /// <param name="timeout"></param>
        public void StartDetection(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "startDetection", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "location1DetectionRunning", true },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>() {
                    { "startDetection", false },
                });
            }
            this.CheckError();
        }

        /// <summary>
        /// Signal MUJIN controller to stop detection. Block until detection stopped running.
        /// </summary>
        /// <param name="timeout"></param>
        public void StopDetection(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "stopDetection", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "location1DetectionRunning", false },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally {
                this.controller.Set(new Dictionary<string, object>() {
                    { "stopDetection", false },
                });
            }
            this.CheckError();
        }

        public bool IsGrabbingTarget()
        {
            return this.controller.SyncAndGet<bool>("isGrabbingTarget", false);
        }

        /// <summary>
        /// Signal MUJIN controller to power off gripper.
        /// </summary>
        /// <param name="timeout"></param>
        public void StopGripper(TimeSpan? timeout = null)
        {
            this.controller.Set("stopGripper", true);
            try
            {
                this.controller.WaitUntil("isGrabbingTarget", false, timeout);
            }
            finally
            {
                this.controller.Set("stopGripper", false);
            }
            this.CheckError();
        }

        public void ChuckGripper(TimeSpan? timeout = null)
        {
            this.controller.Set("chuckGripper", true);
            // TODO: currently there is no signal to indicate chuck gripper command has been received.
            Thread.Sleep(500);
            this.controller.Set("chuckGripper", false);
            this.controller.Sync();
            this.CheckError();
        }

        public void UnchuckGripper(TimeSpan? timeout = null)
        {
            this.controller.Set("unchuckGripper", true);
            // TODO: currently there is no signal to indicate unchuck gripper command has been received.
            Thread.Sleep(500);
            this.controller.Set("unchuckGripper", false);
            this.controller.Sync();
            this.CheckError();
        }
    }
}
