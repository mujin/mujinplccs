using System.Collections.Generic;
using mujinplccs;
using Xunit;

namespace mujintestplccs
{
    public class PLCServiceTests
    {
        [Fact]
        public void TestPing()
        {
            PLCService service = new PLCService();
            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandPing };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
            }   
        }

        [Fact]
        public void TestReadWrite()
        {
            PLCService service = new PLCService();
            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
                Assert.Null(response.Values);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test" } };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
                Assert.NotNull(response.Values);
                Assert.Equal(response.Values.Count, 0);
            }

            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test"] = 3.1415926535;
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandWrite, Values = values };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test" } };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
                Assert.NotNull(response.Values);
                Assert.Equal(response.Values.Count, 1);
                Assert.Equal(response.Values["test"], 3.1415926535);
            }
        }

        [Fact]
        public void TestReadWriteBulk()
        {
            PLCService service = new PLCService();
            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
                Assert.Null(response.Values);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test1", "test2", "test3" } };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
                Assert.NotNull(response.Values);
                Assert.Equal(response.Values.Count, 0);
            }

            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                values["test1"] = "hello";
                values["test3"] = true;
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandWrite, Values = values };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
            }

            {
                PLCRequest request = new PLCRequest { Command = PLCRequest.CommandRead, Keys = new string[] { "test1", "test2", "test3" } };
                PLCResponse response = service.Handle(request);
                Assert.Null(response.Error);
                Assert.NotNull(response.Values);
                Assert.Equal(response.Values.Count, 2);
                Assert.Equal(response.Values["test1"], "hello");
                Assert.Equal(response.Values["test3"], true);
            }
        }

        [Fact]
        public void TestInvalidCommand()
        {
            PLCService service = new PLCService();
            {
                PLCRequest request = new PLCRequest { };
                try
                {
                    service.Handle(request);
                    Assert.True(false);
                }
                catch (PLCInvalidCommandException e)
                {
                    Assert.Equal(e.Code, "invalid_command");
                }
            }

            {
                PLCRequest request = new PLCRequest { Command = "" };
                try
                {
                    service.Handle(request);
                    Assert.True(false);
                }
                catch (PLCInvalidCommandException e)
                {
                    Assert.Equal(e.Code, "invalid_command");
                }
            }

            {
                PLCRequest request = new PLCRequest { Command = "unknown" };
                try
                {
                    service.Handle(request);
                    Assert.True(false);
                }
                catch (PLCInvalidCommandException e)
                {
                    Assert.Equal(e.Code, "invalid_command");
                }
            }
        }
    }
}
