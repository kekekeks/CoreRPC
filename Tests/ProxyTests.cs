using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CoreRPC.CodeGen;
using Xunit;

namespace Tests
{
    public class ProxyTests
    {
        private class SavedCall
        {
            public MethodInfo Method;
            public object[] Arguments;
        }

        private class MyProxy : IRealProxy
        {
            public object NextRet;
            public List<SavedCall> Calls = new List<SavedCall>();
            public object Invoke(MethodInfo method, IEnumerable args)
            {
                Calls.Add(new SavedCall {Method = method, Arguments = args.Cast<object>().ToArray()});
                return NextRet;
            }
        }

        public interface IEmpty
        {
            
        }

        public interface ISimple
        {
            int Foo(int x, string y);
            void Bar();
        }

        [Fact]
        public void TestEmpty()
        {
            ProxyGen.CreateInstance<IEmpty>(new MyProxy());
        }

        [Fact]
        public void TestCalls()
        {
            var proxy = new MyProxy() {NextRet = 5};
            var tproxy = ProxyGen.CreateInstance<ISimple>(proxy);
            tproxy.Bar();
            Assert.Equal(5, tproxy.Foo(1, "2"));
            Assert.Equal(2, proxy.Calls.Count);
            Assert.Equal(1, proxy.Calls[1].Arguments[0]);
            Assert.Equal("2", proxy.Calls[1].Arguments[1]);

            Assert.Equal("Bar", proxy.Calls[0].Method.Name);
            Assert.Equal("Foo", proxy.Calls[1].Method.Name);

        }


    }
}
