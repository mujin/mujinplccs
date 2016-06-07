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
                Assert.Equal(0, memory.Count);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.NotNull(data);
                Assert.Equal(0, data.Count);
            }
        }

        [Fact]
        public void TestRead()
        {
            PLCMemory memory = new PLCMemory();
            {
                memory["test1"] = 10;
                Assert.Equal(1, memory.Count);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.NotNull(data);
                Assert.Equal(1, data.Count);
                Assert.Equal(10, data["test1"]);
            }

            {
                memory["test1"] = "10";
                memory["test2"] = int.MinValue;
                Assert.Equal(2, memory.Count);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.NotNull(data);
                Assert.Equal(2, data.Count);
                Assert.Equal("10", data["test1"]);
                Assert.Equal(int.MinValue, data["test2"]);
            }
        }

        [Fact]
        public void TestWrite()
        {
            PLCMemory memory = new PLCMemory();
            {
                Assert.Equal(0, memory.Count);

                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = true;
                memory.Write(values);

                Assert.Equal(1, memory.Count);
                Assert.Equal(true, memory["test1"]);
            }

            {
                Assert.Equal(1, memory.Count);

                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = "true";
                values["test2"] = null;
                memory.Write(values);

                Assert.Equal(2, memory.Count);
                Assert.Equal("true", memory["test1"]);
                Assert.Equal(null, memory["test2"]);
            }
        }
    }
}
