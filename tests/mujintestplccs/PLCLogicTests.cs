using System;
using System.Collections.Generic;
using mujinplccs;
using Xunit;

namespace mujintestplccs
{
    public class PLCLogicTests
    {
        [Theory]
        [InlineData("123", "coffeebox", 10, 10)]
        [InlineData("123", "coffeebox", 4, 10)]
        [InlineData("123", "coffeebox", 10, 9)]
        public void TestOrderCycle(string orderId, string partType, int orderNumber, int supplyNumber)
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
                { "numLeftInSupply", supplyNumber },
            });
            var status = customerLogic.StartOrderCycle(orderId, partType, orderNumber, timeout);
            Assert.Equal(true, status.IsRunningOrderCycle);
            Assert.Equal(orderNumber, status.NumLeftInOrder);
            Assert.Equal(supplyNumber, status.NumLeftInSupply);

            // check order status
            while (true)
            {
                mujinController.Set(new Dictionary<string, object>()
                {
                    { "isRobotMoving", true },
                    { "numLeftInOrder", orderNumber },
                    { "numLeftInSupply", supplyNumber },
                });
                status = customerLogic.WaitForOrderCycleStatusChange(timeout);
                Assert.Equal(true, status.IsRobotMoving);
                Assert.Equal(orderNumber, status.NumLeftInOrder);
                Assert.Equal(supplyNumber, status.NumLeftInSupply);

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
                    { "numLeftInSupply", supplyNumber },
                    { "orderCycleFinishCode", (int)PLCLogic.PLCOrderCycleFinishCode.FinishedOrderComplete },
                });
                status = customerLogic.WaitUntilOrderCycleFinish(timeout);
                Assert.Equal(false, status.IsRunningOrderCycle);
                Assert.Equal(false, status.IsRobotMoving);
                Assert.Equal(0, status.NumLeftInOrder);
                Assert.Equal(supplyNumber, status.NumLeftInSupply);
                Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedOrderComplete, status.OrderCycleFinishCode);
            }
            else
            {
                mujinController.Set(new Dictionary<string, object>()
                {
                    { "isRobotMoving", false },
                    { "isRunningOrderCycle", false },
                    { "numLeftInOrder", orderNumber },
                    { "numLeftInSupply", supplyNumber },
                    { "orderCycleFinishCode", (int)PLCLogic.PLCOrderCycleFinishCode.FinishedNoMoreTargets },
                });
                status = customerLogic.WaitUntilOrderCycleFinish(timeout);
                Assert.Equal(false, status.IsRunningOrderCycle);
                Assert.Equal(false, status.IsRobotMoving);
                Assert.Equal(orderNumber, status.NumLeftInOrder);
                Assert.Equal(0, status.NumLeftInSupply);
                Assert.Equal(PLCLogic.PLCOrderCycleFinishCode.FinishedNoMoreTargets, status.OrderCycleFinishCode);
            }
        }
    }
}
