﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CoreRPC;
using CoreRPC.Binding;
using CoreRPC.Binding.Default;
using CoreRPC.CodeGen;
using CoreRPC.JsonLikeBinarySerializer;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transferable;
using Newtonsoft.Json;
using Xunit;

namespace Tests
{

    public class XmlMethodCallSerializerTests : MethodCallSerializerTests
    {
        public XmlMethodCallSerializerTests() : base(new XmlMethodCallSerializer())
        {
        }
    }

    public class JsonMethodCallSerializerTests : MethodCallSerializerTests
    {
        public JsonMethodCallSerializerTests() : base(new JsonMethodCallSerializer())
        {
        }
    }

    public class BsonMethodCallSerializerTests : MethodCallSerializerTests
    {
        public BsonMethodCallSerializerTests() : base(new BsonMethodCallSerializer())
        {
        }
    }

    public class BinaryMethodCallSerializerTests : MethodCallSerializerTests
    {
        public BinaryMethodCallSerializerTests():base(new BinaryJsonLikeMethodCallSerializer())
        {
            
        }
    }

    public abstract class MethodCallSerializerTests
    {
        private readonly IMethodCallSerializer _serializer;

        public MethodCallSerializerTests(IMethodCallSerializer serializer)
        {
            _serializer = serializer;
        }

        public interface IFoo
        {
            Task Test(int x, List<string> y, string test);
            Task<byte[]> Test(byte[] data);
        }

        private class Target : IFoo
        {
            public Task Test(int x, List<string> y, string z)
            {
                Assert.Equal(1, x);
                Assert.Equal("2", y[0]);
                Assert.Equal("3", y[1]);
                Assert.Equal("2022-11-29T18:00:00.000Z", z);
                return Task.CompletedTask;
            }

            public async Task<byte[]> Test(byte[] data)
            {
                return data.Reverse().ToArray();
            }
        }

        protected class TargetSelector : ITargetSelector
        {
            private readonly object _target;

            public TargetSelector(object target)
            {
                _target = target;
            }

            public object GetTarget(string target, object callContext)
            {
                Assert.Equal("Target", target);
                Assert.Equal("ctx", callContext);
                return _target;
            }
        }

        protected class Proxy : IRealProxy
        {
            public MethodCall LastCall;
            public Task<T> Invoke<T>(MethodInfo method, IEnumerable args)
            {
                LastCall = new MethodCall { Method = method, Arguments = args.Cast<object>().ToArray() };
                return null;
            }
        }


        [Fact]
        public void TestSerializeAndDeserialize()
        {
            var proxy = new Proxy();
            var tproxy = ProxyGen.CreateInstance<IFoo>(proxy);
            tproxy.Test(1, new List<string> { "2", "3" }, "2022-11-29T18:00:00.000Z");
            var ser = _serializer;
            var binder = new DefaultMethodBinder();
            var ms = new MemoryStream();

            ser.SerializeCall(ms, binder, "Target", proxy.LastCall);
            ms.Seek(0, SeekOrigin.Begin);

            var call = ser.DeserializeCall(ms, binder, new TargetSelector(new Target()), "ctx");
            call.Method.Invoke(call.Target, call.Arguments);
        }
        
        protected MethodCallResult Reserialize(object obj, Type expectedType)
        {
            var ser = _serializer;
            var ms = new MemoryStream();
            ser.SerializeResult(ms, obj);
            ms.Seek(0, SeekOrigin.Begin);
            return ser.DeserializeResult(ms, expectedType);
        }

        MethodCallResult ReserializeException(string ex)
        {
            var ser = _serializer;
            var ms = new MemoryStream();
            ser.SerializeException(ms, ex);
            ms.Seek(0, SeekOrigin.Begin);
            return ser.DeserializeResult(ms, typeof(string));
        }

        [Fact]
        public void TestResultSerialization()
        {
            var emptyResult = Reserialize(null, null);
            Assert.Null(emptyResult.Exception);
            Assert.Null(emptyResult.Result);

            var exceptionResult = ReserializeException("fail");
            Assert.Null(exceptionResult.Result);
            Assert.Equal("fail", exceptionResult.Exception);

            var valueResult = Reserialize(new List<string> { "success" }, typeof(List<string>));
            Assert.Null(valueResult.Exception);
            Assert.Equal("success", ((List<string>)valueResult.Result)[0]);
        }
        
        [Fact]
        public void TestSerializeAndDeserializeBytes()
        {
            var proxy = new Proxy();
            var tproxy = ProxyGen.CreateInstance<IFoo>(proxy);
            tproxy.Test(new byte[] { 1, 2, 3 });
            var ser = _serializer;
            var binder = new DefaultMethodBinder();
            var ms = new MemoryStream();

            ser.SerializeCall(ms, binder, "Target", proxy.LastCall);
            ms.Seek(0, SeekOrigin.Begin);

            byte[] res;
            using (var call = ser.DeserializeCall(ms, binder, new TargetSelector(new Target()), "ctx"))
                res = ((Task<byte[]>)call.Method.Invoke(call.Target, call.Arguments)).Result;

            var result = (byte[])Reserialize(res, typeof(byte[])).Result;
            Assert.Equal(new byte[] { 3, 2, 1 }, result);

        }

    }

    public abstract class TypePreservingMethodCallSerializerTests : MethodCallSerializerTests
    {
        private readonly IMethodCallSerializer _serializer;

        public interface IBar
        {
            Task Test(object arg1, object[] arg2);
        }
        
        public TypePreservingMethodCallSerializerTests(IMethodCallSerializer serializer) : base(serializer)
        {
            _serializer = serializer;
        }

        public class A { }
        public class B { }
        public class C { }

        class Target : IBar
        {
            public Task Test(object arg1, object[] arg2)
            {
                Assert.IsType<A>(arg1);
                Assert.IsType<B>(arg2[0]);
                Assert.IsType<C>(arg2[1]);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void SerializerShouldPreserveTypes()
        {
            var proxy = new Proxy();
            var tproxy = ProxyGen.CreateInstance<IBar>(proxy);
            tproxy.Test(new A(), new object[] {new B(), new C()});
            var ser = _serializer;
            var binder = new DefaultMethodBinder();
            var ms = new MemoryStream();

            ser.SerializeCall(ms, binder, "Target", proxy.LastCall);
            ms.Seek(0, SeekOrigin.Begin);
            
            var call = ser.DeserializeCall(ms, binder, new TargetSelector(new Target()), "ctx");
            call.Method.Invoke(call.Target, call.Arguments);
        }
    }

    public class TypePreservingJsonMethodCallSerializerTests : TypePreservingMethodCallSerializerTests
    {
        public TypePreservingJsonMethodCallSerializerTests() 
            : base(new JsonMethodCallSerializer(new JsonSerializer(){TypeNameHandling = TypeNameHandling.Auto}))
        {
        }
    }

    public class TypePreservingBsonMethodCallSerializerTests : TypePreservingMethodCallSerializerTests
    {
        public TypePreservingBsonMethodCallSerializerTests()
            : base(new BsonMethodCallSerializer(new JsonSerializer() { TypeNameHandling = TypeNameHandling.All }))
        {
        }
    }
    
    public class TypePreservingBinaryMethodCallSerializerTests : TypePreservingMethodCallSerializerTests
    {
        public TypePreservingBinaryMethodCallSerializerTests()
            : base(new BinaryJsonLikeMethodCallSerializer(new JsonSerializer() { TypeNameHandling = TypeNameHandling.All }, true))
        {
        }
    }
}
