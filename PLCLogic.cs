using System;
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
            private PLCErrorCode errorCode = PLCErrorCode.ErrorCodeNotAvailable;
            private int detailedErrorCode = 0;

            public PLCError(PLCErrorCode errorCode, int detailedErrorCode = 0) : base(String.Format("An error has occurred: {0}", errorCode))
            {
                this.errorCode = errorCode;
                this.detailedErrorCode = detailedErrorCode;
            }

            /// <summary>
            /// MUJIN PLC Error Code
            /// </summary>
            public PLCErrorCode ErrorCode
            {
                get { return this.errorCode; }
            }

            /// <summary>
            /// When ErrorCode is RobotError, DetailedErrorCode contains the error code returned by robot controller.
            /// </summary>
            public int DetailedErrorCode
            {
                get { return this.detailedErrorCode; }
            }
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
            FinishedCommandDisabled = 0x0100,
            FinishedStopped = 0x0101,
            FinishedStoppedImmediately = 0x0102,
            FinishedPlanningFailure = 0x0100,
            FinishedGenericFailure = 0xffff,
        }

        /// <summary>
        /// MUJIN order cycle status information.
        /// </summary>
        public sealed class PLCOrderCycleStatus
        {
            /// <summary>
            /// Whether the order cycle is currently running.
            /// </summary>
            public bool IsRunningOrderCycle { get; set; }

            /// <summary>
            /// Whether the robot is currently moving.
            /// </summary>
            public bool IsRobotMoving { get; set; }

            /// <summary>
            /// Whether detection is currently running.
            /// </summary>
            public bool IsSupplyDetectionRunning { get; set; }

            /// <summary>
            /// Number of items left in order to be picked.
            /// </summary>
            public int NumLeftInOrder { get; set; }

            /// <summary>
            /// Number of items detected in the source container.
            /// </summary>
            public int NumLeftInSupply { get; set; }

            /// <summary>
            /// MUJIN PLC OrderCycleFinishCode.
            /// </summary>
            public PLCOrderCycleFinishCode OrderCycleFinishCode { get; set; }

            /// <summary>
            /// Whether source container can be safely moved at the moment.
            /// </summary>
            public bool CanChangeSupplyContainer { get; set; }

            /// <summary>
            /// Whether destination container can be safely moved at the moment.
            /// </summary>
            public bool CanChangeDestContainer { get; set; }

            /// <summary>
            /// Whether source container is currently empty.
            /// </summary>
            public bool SupplyContainerNotEmpty { get; set; }

            /// <summary>
            /// Whether destination container is currently full.
            /// </summary>
            public bool DestContainerFull { get; set; }
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
            if (this.controller.Get("isError", false).Equals(true))
            {
                var errorcode = (PLCErrorCode)Convert.ToInt32(this.controller.Get("errorcode", 0));
                var detailedErrorCode = Convert.ToInt32(this.controller.Get("detailedErrorCode", 0));
                throw new PLCError(errorcode, detailedErrorCode);
            }
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
        /// <param name="orderId"></param>
        /// <param name="partType"></param>
        /// <param name="orderNumber"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public PLCOrderCycleStatus StartOrderCycle(string orderId, string partType, int orderNumber, TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", true },
                { "orderId", orderId },
                { "partType", orderId },
                { "orderNumber", orderId },
                { "startOrderCycle", true },
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
                    { "enableCommands", false },
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
                IsRunningOrderCycle = Convert.ToBoolean(values.Get("isRunningOrderCycle", false)),
                IsRobotMoving = Convert.ToBoolean(values.Get("isRobotMoving", false)),
                IsSupplyDetectionRunning = Convert.ToBoolean(values.Get("isSupplyDetectionRunning", false)),
                NumLeftInOrder = Convert.ToInt32(values.Get("numLeftInOrder", 0)),
                NumLeftInSupply = Convert.ToInt32(values.Get("numLeftInSupply", 0)),
                OrderCycleFinishCode = (PLCOrderCycleFinishCode)Convert.ToInt32(values.Get("orderCycleFinishCode", 0)),
                CanChangeSupplyContainer = Convert.ToBoolean(values.Get("canChangeSupplyContainer", false)),
                CanChangeDestContainer = Convert.ToBoolean(values.Get("canChangeDestContainer", false)),
                SupplyContainerNotEmpty = Convert.ToBoolean(values.Get("supplyContainerNotEmpty", false)),
                DestContainerFull = Convert.ToBoolean(values.Get("destContainerFull", false)),
            };
        }

        /// <summary>
        /// Block until values in order cycle status changes.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>Order cycle status information in the current state.</returns>
        public PLCOrderCycleStatus WaitForOrderCycleStatusChange(TimeSpan? timeout = null)
        {
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
                { "enableCommands", true },
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
                    { "enableCommands", false },
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
                { "enableCommands", true },
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
                    { "enableCommands", false },
                    { "stopImmediately", false },
                });
            }
            this.CheckError();
        }

        /// <summary>
        /// Signal to start the preparation cycle on MUJIN controller.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="partType"></param>
        /// <param name="orderNumber"></param>
        /// <param name="timeout"></param>
        public void StartPreparation(string orderId, string partType, int orderNumber, TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", true },
                { "prepareOrderId", orderId },
                { "preparePartType", orderId },
                { "prepareOrderNumber", orderId },
                { "startPreparation", true },
            });
            // TODO: currently no signal to indicate preparation has started
            Thread.Sleep(1000);
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", false },
                { "startPreparation", false },
            });
            this.controller.Sync();
            this.CheckError();
        }

        /// <summary>
        /// Signal to stop the preparation cycle on MUJIN controller.
        /// </summary>
        /// <param name="timeout"></param>
        public void StopPreparation(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", true },
                { "stopPreparation", true },
            });
            // TODO: currently no signal to indicate preparation has stopped
            Thread.Sleep(1000);
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", false },
                { "stopPreparation", false },
            });
            this.CheckError();
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

        /// <summary>
        /// Signal MUJIN controller to move the robot to its home position. Block until the robot starts moving.
        /// </summary>
        /// <param name="timeout"></param>
        public void StartMoveToHome(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", true },
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
                    { "enableCommands", false },
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
                { "enableCommands", true },
                { "startDetection", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "isSupplyDetectionRunning", true },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally
            {
                this.controller.Set(new Dictionary<string, object>() {
                    { "enableCommands", false },
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
                { "enableCommands", true },
                { "stopDetection", true },
            });
            try
            {
                this.controller.WaitUntil(new Dictionary<string, object>() {
                    { "isSupplyDetectionRunning", false },
                }, new Dictionary<string, object>() {
                    { "isError", true },
                }, timeout);
            }
            finally {
                this.controller.Set(new Dictionary<string, object>() {
                    { "enableCommands", false },
                    { "stopDetection", false },
                });
            }
            this.CheckError();
        }

        /// <summary>
        /// Signal MUJIN controller to power off gripper.
        /// </summary>
        /// <param name="timeout"></param>
        public void StopGripper(TimeSpan? timeout = null)
        {
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", true },
                { "stopGripper", true },
            });
            // TODO: currently there is no signal to indicate stop gripper command has been received.
            Thread.Sleep(1000);
            this.controller.Set(new Dictionary<string, object>() {
                { "enableCommands", false },
                { "stopGripper", false },
            });
            this.controller.Sync();
            this.CheckError();
        }
    }
}
