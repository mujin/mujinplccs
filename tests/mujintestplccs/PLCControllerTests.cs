using System;
using System.Collections.Generic;
using mujinplccs;
using Xunit;

namespace mujintestplccs
{
    public class PLCControllerTests
    {
        [Theory]
        [InlineData(0, 10)]
        [InlineData(0, 200)]
        [InlineData(100, 0)]
        [InlineData(100, 10)]
        [InlineData(100, 200)]
        public void TestWaitTimeout(int maxHeartbeatIntervalMilliseconds, int timeoutMilliseconds)
        {
            var memory = new PLCMemory();
            TimeSpan? maxHeartbeatInterval = null;
            TimeSpan? timeout = null;
            if (maxHeartbeatIntervalMilliseconds > 0)
            {
                maxHeartbeatInterval = TimeSpan.FromMilliseconds(maxHeartbeatIntervalMilliseconds);
            }
            if (timeoutMilliseconds > 0)
            {
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            }
            var controller = new PLCController(memory, maxHeartbeatInterval);

            if (maxHeartbeatInterval.HasValue)
            {
                Assert.Equal(false, controller.IsConnected);
                if (timeout.HasValue)
                {
                    Assert.Throws<TimeoutException>(() =>
                    {
                        controller.WaitUntilConnected(timeout);
                    });
                }
            }
            else
            {
                Assert.Equal(true, controller.IsConnected);
                controller.WaitUntilConnected(timeout);
            }

            Assert.Throws<TimeoutException>(() =>
            {
                controller.WaitFor("someSignal", true, timeout);
            });

            Assert.Throws<TimeoutException>(() =>
            {
                controller.WaitUntil("someSignal", true, timeout);
            });

        }
    }
}
