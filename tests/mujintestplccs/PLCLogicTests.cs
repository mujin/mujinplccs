using System;
using System.Threading;
using System.Collections.Generic;
using mujinplccs;
using Xunit;

namespace mujintestplccs
{
    public class PLCLogicTests
    {
        [Fact]
        public void TestStartOrderCycle()
        {
            var memory = new PLCMemory();
            var customerLogic = new PLCLogic(new PLCController(memory));
            var mujinController = new PLCController(memory);

            // indicate we are ready
            mujinController.Set(new Dictionary<string, object>()
            {
                { "isRunningOrderCycle", false },
                { "isRobotMoving", false },
                { "isModeAuto", true },
                { "isSystemReady", true },
                { "isCycleReady", true },
            });

            ThreadPool.QueueUserWorkItem(delegate
            {
                Thread.Sleep(200);
                mujinController.Set(new Dictionary<string, object>()
                {
                    { "isRunningOrderCycle", true },
                    { "numLeftInOrder", 10 },
                    { "numLeftInSupply", 9 },
                });
            });

            var status = customerLogic.StartOrderCycle("test", "coffeebox", 10, TimeSpan.FromSeconds(1));
            Assert.Equal(10, status.NumLeftInOrder);
            Assert.Equal(9, status.NumLeftInSupply);
        }
    }
}
