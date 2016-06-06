using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using mujinplccs;

namespace mujintestplccs
{
    [TestClass]
    public class PLCControllerTests
    {
        [TestMethod]
        public void TestPing()
        {
            PLCController controller = new PLCController();
            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandPing };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
            }   
        }

        [TestMethod]
        public void TestReadWrite()
        {
            PLCController controller = new PLCController();
            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
                Assert.IsNull(response.Values);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test" } };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
                Assert.IsNotNull(response.Values);
                Assert.AreEqual(response.Values.Count, 0);
            }

            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test"] = 3.1415926535;
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandWrite, Values = values };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test" } };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
                Assert.IsNotNull(response.Values);
                Assert.AreEqual(response.Values.Count, 1);
                Assert.AreEqual(response.Values["test"], 3.1415926535);
            }
        }

        [TestMethod]
        public void TestReadWriteBulk()
        {
            PLCController controller = new PLCController();
            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
                Assert.IsNull(response.Values);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test1", "test2", "test3" } };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
                Assert.IsNotNull(response.Values);
                Assert.AreEqual(response.Values.Count, 0);
            }

            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = "hello";
                values["test3"] = true;
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandWrite, Values = values };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test1", "test2", "test3" } };
                PLCResponse response = controller.Process(request);
                Assert.IsNull(response.Error);
                Assert.IsNotNull(response.Values);
                Assert.AreEqual(response.Values.Count, 2);
                Assert.AreEqual(response.Values["test1"], "hello");
                Assert.AreEqual(response.Values["test3"], true);
            }
        }

        [TestMethod]
        public void TestInvalidCommand()
        {
            PLCController controller = new PLCController();
            {
                PLCRequest request = new PLCRequest { };
                try
                {
                    controller.Process(request);
                    Assert.Fail();
                }
                catch (PLCInvalidCommandException e)
                {
                    Assert.AreEqual(e.Code, "invalid_command");
                }
            }

            {
                PLCRequest request = new PLCRequest { Command = "" };
                try
                {
                    controller.Process(request);
                    Assert.Fail();
                }
                catch (PLCInvalidCommandException e)
                {
                    Assert.AreEqual(e.Code, "invalid_command");
                }
            }

            {
                PLCRequest request = new PLCRequest { Command = "unknown" };
                try
                {
                    controller.Process(request);
                    Assert.Fail();
                }
                catch (PLCInvalidCommandException e)
                {
                    Assert.AreEqual(e.Code, "invalid_command");
                }
            }
        }
    }
}
