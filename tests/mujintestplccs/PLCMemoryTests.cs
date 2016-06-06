using System.Collections.Generic;
using mujinplccs;
using Xunit;

namespace mujintestplccs
{
    public class PLCMemoryTests
    {
        [Fact]
        public void TestEmpty()
        {
            PLCMemory memory = new PLCMemory();
            {
                Assert.Equal(memory.Count, 0);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.NotNull(data);
                Assert.Equal(data.Count, 0);
            }
        }

        [Fact]
        public void TestRead()
        {
            PLCMemory memory = new PLCMemory();
            {
                memory["test1"] = 10;
                Assert.Equal(memory.Count, 1);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.NotNull(data);
                Assert.Equal(data.Count, 1);
                Assert.Equal(data["test1"], 10);
            }

            {
                memory["test1"] = "10";
                memory["test2"] = int.MinValue;
                Assert.Equal(memory.Count, 2);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.NotNull(data);
                Assert.Equal(data.Count, 2);
                Assert.Equal(data["test1"], "10");
                Assert.Equal(data["test2"], int.MinValue);
            }
        }

        [Fact]
        public void TestWrite()
        {
            PLCMemory memory = new PLCMemory();
            {
                Assert.Equal(memory.Count, 0);

                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = true;
                memory.Write(values);

                Assert.Equal(memory.Count, 1);
                Assert.Equal(memory["test1"], true);
            }

            {
                Assert.Equal(memory.Count, 1);

                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = "true";
                values["test2"] = null;
                memory.Write(values);

                Assert.Equal(memory.Count, 2);
                Assert.Equal(memory["test1"], "true");
                Assert.Equal(memory["test2"], null);
            }
        }
    }
}
