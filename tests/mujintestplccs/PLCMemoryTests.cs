using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using mujinplccs;

namespace mujintestplccs
{
    [TestClass]
    public class PLCMemoryTests
    {
        [TestMethod]
        public void TestEmpty()
        {
            PLCMemory memory = new PLCMemory();
            {
                Assert.AreEqual(memory.Count, 0);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.IsNotNull(data);
                Assert.AreEqual(data.Count, 0);
            }
        }

        [TestMethod]
        public void TestRead()
        {
            PLCMemory memory = new PLCMemory();
            {
                memory["test1"] = 10;
                Assert.AreEqual(memory.Count, 1);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.IsNotNull(data);
                Assert.AreEqual(data.Count, 1);
                Assert.AreEqual(data["test1"], 10);
            }

            {
                memory["test1"] = "10";
                memory["test2"] = int.MinValue;
                Assert.AreEqual(memory.Count, 2);

                Dictionary<string, object> data = memory.Read(new string[] { "test1", "test2" });
                Assert.IsNotNull(data);
                Assert.AreEqual(data.Count, 2);
                Assert.AreEqual(data["test1"], "10");
                Assert.AreEqual(data["test2"], int.MinValue);
            }
        }

        [TestMethod]
        public void TestWrite()
        {
            PLCMemory memory = new PLCMemory();
            {
                Assert.AreEqual(memory.Count, 0);

                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = true;
                memory.Write(values);

                Assert.AreEqual(memory.Count, 1);
                Assert.AreEqual(memory["test1"], true);
            }

            {
                Assert.AreEqual(memory.Count, 1);

                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = "true";
                values["test2"] = null;
                memory.Write(values);

                Assert.AreEqual(memory.Count, 2);
                Assert.AreEqual(memory["test1"], "true");
                Assert.AreEqual(memory["test2"], null);
            }
        }
    }
}
