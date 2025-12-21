using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CoreRPC.Binding.Default;
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
        public int[] Array { get; set; }
        public List<bool> BoolList { get; set; }
    }

    public interface ITestRpc
    {
        Task<bool> TestMethod(TestDto dto, string a);
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
                RpcClientBaseType = "FCoreRpcClientBase",
                DtoHeaderName = "CommunicationDto"
            };
            var generator = new UeRpcGenerator("", options, new DefaultMethodBinder());
            generator.TypeConverter.AddRpcType(typeof(ITestRpc));
            var r = generator.GenerateHeaderForRpc(typeof(ITestRpc), "CoreRpcProxyTestRpc");
            r = generator.GenerateCodeForRpc(typeof(ITestRpc), "CoreRpcProxyTestRpc");
        }
    }
}