using System.Diagnostics;
using CoreRPC.UnrealEngine;
using Xunit;

namespace Tests
{

    public enum TestEnum
    {
        None = 0,
        Value1 = 1,
        Value2 = 2
    }
    
    public class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TestEnum EnumValue { get; set; }
    }
    
    public class UnrealEngineCodeGenTests
    {
        [Fact]
        public void TestDtoHeader()
        {
            var options = new UeCodeGenOptions()
            {
                ApiDefine = "TEST_API",
                FutureClassName = "SD::TExpectedFuture",
                RpcClientBaseType = "FCoreRpcClientBase"
            };
            var generator = new UeRpcGenerator("", options);
            var headerData = generator.GenerateHeaderWithDto("TestHeader", new[] { typeof(TestDto) });
            Debug.Print(headerData);
        }
    }
}