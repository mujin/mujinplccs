using System;
using System.Collections.Generic;
using mujinplccs;
using Xunit;

namespace mujintestplccs
{
    public class PLCLogicTests
    {
        [Theory]
        [InlineData("coffeebox", 10, 1, "1", 2, "2", 10)]
        [InlineData("coffeebox", 4, 1, "1", 2, "2", 10)]
        [InlineData("coffeebox", 10, 1, "1", 2, "2", 9)]
        public void TestOrderCycle(string orderPartType, int orderNumber, int orderPickLocationIndex, string orderPickContainerId, int orderPlaceLocationIndex, string orderPlaceContainerId, int supplyNumber)
        {
            var timeout = TimeSpan.FromMilliseconds(100);
            var memory = new PLCMemory();
            var customerLogic = new PLCLogic(new PLCController(memory));
            var mujinController = new PLCController(memory);

            customerLogic.ClearAllSignals();

            // indicate we are ready
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", false },
                { "isRobotMoving", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            });
            customerLogic.WaitUntilOrderCycleReady(timeout);

            // start order cycle
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", true },
                { "numLeftInOrder", orderNumber },
                { "numLeftInLocation1", supplyNumber },
            });
            var status = customerLogic.StartOrderCycle(orderPartType, orderNumber, orderPickLocationIndex, orderPickContainerId, orderPlaceLocationIndex, orderPlaceContainerId, timeout);
            Assert.Equal(true, status.isRunningOrderCycle);
            Assert.Equal(orderNumber, status.numLeftInOrder);
            Assert.Equal(supplyNumber, status.numLeftInLocation1);

            // check order status
            while (true)
            {
                mujinController.Set(new Dictionary<string, object>()
                {
                    { "isRobotMoving", true },
                    { "numLeftInOrder", orderNumber },
                    { "numLeftInLocation1", supplyNumber },
                });
                status = customerLogic.GetOrderCycleStatus(timeout);
                Assert.Equal(true, status.isRobotMoving);
                Assert.Equal(orderNumber, status.numLeftInOrder);
                Assert.Equal(supplyNumber, status.numLeftInLocation1);

                if (orderNumber <= 0 || supplyNumber <= 0)
                {
                    break;
                }

                orderNumber--;
                supplyNumber--;
            }

            if (orderNumber == 0)
            {
                mujinController.Set(new Dictionary<string, object>()
                {
                    { "isRobotMoving", false },
                    { "isRunningOrderCycle", false },
                    { "numLeftInOrder", orderNumber },
                    { "numLeftInLocation1", supplyNumber },
                    { "orderCycleFinishCode", (int)PLCLogic.PLCOrderCycleFinishCode.FinishedOrderComplete },
                });
                status = customerLogic.WaitUntilOrderCycleFinish(timeout);
                Assert.Equal(false, status.isRunningOrderCycle);
                Assert.Equal(false, status.isRobotMoving);
                Assert.Equal(0, status.numLeftInOrder);
                Assert.Equal(supplyNumber, status.numLeftInLocation1);
                Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedOrderComplete, status.orderCycleFinishCode);
            }
            else
            {
                mujinController.Set(new Dictionary<string, object>()
                {
                    { "isRobotMoving", false },
                    { "isRunningOrderCycle", false },
                    { "numLeftInOrder", orderNumber },
                    { "numLeftInLocation1", supplyNumber },
                    { "orderCycleFinishCode", (int)PLCLogic.PLCOrderCycleFinishCode.FinishedNoMoreTargets },
                });
                status = customerLogic.WaitUntilOrderCycleFinish(timeout);
                Assert.Equal(false, status.isRunningOrderCycle);
                Assert.Equal(false, status.isRobotMoving);
                Assert.Equal(orderNumber, status.numLeftInOrder);
                Assert.Equal(0, status.numLeftInLocation1);
                Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedNoMoreTargets, status.orderCycleFinishCode);
            }
        }

        [Fact]
        public void TestStopOrderCycle()
        {
            var timeout = TimeSpan.FromMilliseconds(100);
            var memory = new PLCMemory();
            var customerLogic = new PLCLogic(new PLCController(memory));
            var mujinController = new PLCController(memory);

            customerLogic.ClearAllSignals();

            // indicate we are ready
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", false },
                { "isRobotMoving", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            });
            customerLogic.WaitUntilOrderCycleReady(timeout);

            // start order cycle
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", true },
                { "numLeftInOrder", 1 },
                { "numLeftInLocation1", 1 },
            });
            var status = customerLogic.StartOrderCycle("orderPartType", 1, 1, "1", 2, "2", timeout);
            Assert.Equal(true, status.isRunningOrderCycle);
            Assert.Equal(1, status.numLeftInOrder);
            Assert.Equal(1, status.numLeftInLocation1);

            // stop order cycle
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRobotMoving", false },
                { "isRunningOrderCycle", false },
                { "numLeftInOrder", 1 },
                { "numLeftInLocation1", 1 },
                { "orderCycleFinishCode", (int)PLCLogic.PLCOrderCycleFinishCode.FinishedStopped },
            });

            status = customerLogic.StopOrderCycle();
            Assert.Equal(false, status.isRunningOrderCycle);
            Assert.Equal(false, status.isRobotMoving);
            Assert.Equal(1, status.numLeftInOrder);
            Assert.Equal(1, status.numLeftInLocation1);
            Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedStopped, status.orderCycleFinishCode);
        }

        [Fact]
        public void TestStopImmediately()
        {
            var timeout = TimeSpan.FromMilliseconds(100);
            var memory = new PLCMemory();
            var customerLogic = new PLCLogic(new PLCController(memory));
            var mujinController = new PLCController(memory);

            customerLogic.ClearAllSignals();

            // indicate we are ready
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", false },
                { "isRobotMoving", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            });
            customerLogic.WaitUntilOrderCycleReady(timeout);

            // start order cycle
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", true },
                { "numLeftInOrder", 1 },
                { "numLeftInLocation1", 1 },
            });
            var status = customerLogic.StartOrderCycle("orderPartType", 1, 1, "1", 2, "2", timeout);
            Assert.Equal(true, status.isRunningOrderCycle);
            Assert.Equal(1, status.numLeftInOrder);
            Assert.Equal(1, status.numLeftInLocation1);

            // stop immediately
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRobotMoving", false },
                { "isRunningOrderCycle", false },
                { "numLeftInOrder", 1 },
                { "numLeftInLocation1", 1 },
                { "orderCycleFinishCode", (int)PLCLogic.PLCOrderCycleFinishCode.FinishedStoppedImmediately },
            });
            customerLogic.StopImmediately();

            // controller might reset variables
            // mujinController.Set(new Dictionary<string, object>()
            // {
            //     { "numLeftInOrder", 0 },
            //     { "numLeftInLocation1", 0 },
            //     { "orderCycleFinishCode", 0 },
            // });

            status = customerLogic.GetOrderCycleStatus();
            Assert.Equal(false, status.isRunningOrderCycle);
            Assert.Equal(false, status.isRobotMoving);
            Assert.Equal(1, status.numLeftInOrder);
            Assert.Equal(1, status.numLeftInLocation1);
            Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedStoppedImmediately, status.orderCycleFinishCode);
        }

        [Fact]
        public void TestResetError()
        {
            var timeout = TimeSpan.FromMilliseconds(100);
            var memory = new PLCMemory();
            var customerLogic = new PLCLogic(new PLCController(memory));
            var mujinController = new PLCController(memory);

            customerLogic.ClearAllSignals();

            // indicate we are ready
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", false },
                { "isRobotMoving", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            });
            customerLogic.WaitUntilOrderCycleReady(timeout);

            // start order cycle
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", true },
                { "numLeftInOrder", 1 },
                { "numLeftInLocation1", 1 },
            });
            var status = customerLogic.StartOrderCycle("orderPartType", 1, 1, "1", 2, "2", timeout);
            Assert.Equal(true, status.isRunningOrderCycle);
            Assert.Equal(1, status.numLeftInOrder);
            Assert.Equal(1, status.numLeftInLocation1);

            // set error
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRobotMoving", false },
                { "isRunningOrderCycle", false },
                { "numLeftInOrder", 1 },
                { "numLeftInLocation1", 1 },
                { "orderCycleFinishCode", (int)PLCLogic.PLCOrderCycleFinishCode.FinishedPlanningFailure },
                { "isError", true },
                { "errorcode", (int)PLCLogic.PLCErrorCode.PlanningError },
            });
            var e = Assert.Throws<PLCLogic.PLCError>(() =>
            {
                customerLogic.WaitUntilOrderCycleFinish();
            });
            Assert.Equal(PLCLogic.PLCErrorCode.PlanningError, e.errorcode);
            //Assert.Equal(0, e.detailedErrorCode);

            status = customerLogic.GetOrderCycleStatus();
            Assert.Equal(false, status.isRunningOrderCycle);
            Assert.Equal(false, status.isRobotMoving);
            Assert.Equal(1, status.numLeftInOrder);
            Assert.Equal(1, status.numLeftInLocation1);
            Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedPlanningFailure, status.orderCycleFinishCode);

            // reset error
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isError", false },
                { "errorcode", 0 },
                { "orderCycleFinishCode", 0 },
            });
            customerLogic.ResetError();

            status = customerLogic.GetOrderCycleStatus();
            Assert.Equal(false, status.isRunningOrderCycle);
            Assert.Equal(false, status.isRobotMoving);
            Assert.Equal(1, status.numLeftInOrder);
            Assert.Equal(1, status.numLeftInLocation1);
            Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedNotAvailable, status.orderCycleFinishCode);
        }
    }
}
