using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using CoreRPC.Binding;
using CoreRPC.Routing;
using CoreRPC.Transferable;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace CoreRPC.Serialization
{
    public class JsonMethodCallSerializer : IMethodCallSerializer
    {
        private readonly JsonSerializer _serializer;

        public JsonMethodCallSerializer(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public JsonMethodCallSerializer() : this(new JsonSerializer())
        {

        }

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        protected virtual JsonWriter CreateWriter(Stream stream) =>
            new JsonTextWriter(new StreamWriter(stream, Utf8, 1024, true));

        protected virtual JsonReader CreateReader(Stream stream) => new JsonTextReader(new StreamReader(stream))
        {
            DateParseHandling = DateParseHandling.None
        };

        public void SerializeCall(Stream stream, IMethodBinder binder, string target, MethodCall call)
        {
            var sig = binder.GetMethodSignature(call.Method);
            using (var writer = CreateWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Target");
                writer.WriteValue(target);
                writer.WritePropertyName("MethodSignature");
                writer.WriteValue(sig);
                writer.WritePropertyName("Arguments");
                _serializer.Serialize(writer, call.Arguments);
                writer.WriteEndObject();
            }
        }



        public virtual MethodCall DeserializeCall(Stream stream, IMethodBinder binder,
            ITargetSelector selector, object callContext)
        {
            var call = new MethodCall();
            DeserializeCallCore(call, CreateReader(stream), binder, selector, callContext);
            return call;
        }
        
        protected virtual void DeserializeCallCore(MethodCall rv, JsonReader reader, 
            IMethodBinder binder, ITargetSelector selector, object callContext)
        {
            reader.MoveToContent();

            rv.Target = selector.GetTarget(reader.ReadProperty("Target").ToString(), callContext);

            reader.ExpectProperty("MethodSignature");
            var osig = reader.Value;
            byte[] sig;
            if (osig is byte[])
                sig = (byte[])osig;
            else if (osig is string)
                sig = Convert.FromBase64String((string)osig);
            else
                sig = (byte[])TypeDescriptor.GetConverter(osig).ConvertTo(osig, typeof(byte[]));
            reader.Next();
            
            rv.Method = binder.GetInfoProviderFor(rv.Target).GetMethod(sig);
            reader.ExpectProperty("Arguments");

            var args = rv.Method.GetParameters();
            if (_serializer.TypeNameHandling >= TypeNameHandling.All)
            {
                rv.Arguments = _serializer.Deserialize<object[]>(reader);
                for (var c = 0; c < rv.Arguments.Length && c < args.Length; c++)
                    if (args[c].ParameterType.GetTypeInfo().IsPrimitive)
                        rv.Arguments[c] = Convert.ChangeType(rv.Arguments[c], args[c].ParameterType);
                reader.Next();
            }
            else
            {
                rv.Arguments = new object[args.Length];

                if (reader.TokenType != JsonToken.StartArray)
                    throw new ArgumentException("Arguments should be an array");

                for (var c = 0; c < args.Length; c++)
                    rv.Arguments[c] = _serializer.Deserialize(reader.Next(), args[c].ParameterType);
                if (reader.Next().TokenType != JsonToken.EndArray)
                    throw new ArgumentException("Expected end array");
                reader.Next();
            }
            reader.MoveToEnd();
        }

        public void SerializeResult(Stream stream, object result)
        {
            using (var w = CreateWriter(stream))
            {
                w.WriteStartObject();
                w.WritePropertyName("Result");
                _serializer.Serialize(w, result);
                w.WriteEndObject();
            }
        }

        public void SerializeException(Stream stream, string exception)
        {
            using (var w = CreateWriter(stream))
            {
                w.WriteStartObject();
                w.WritePropertyName("Exception");
                _serializer.Serialize(w, exception);
                w.WriteEndObject();
            }
        }

        public MethodCallResult DeserializeResult(Stream stream, Type expectedType)
        {
            var reader = CreateReader(stream);
            reader.MoveToContent();
            var rv = new MethodCallResult();
            if (reader.Value.ToString() == "Result")
            {
                rv.Result = _serializer.Deserialize(reader.Next(), expectedType);
                reader.Next();
            }
            else
                rv.Exception = reader.ReadProperty("Exception").ToString();
            reader.MoveToEnd();
            return rv;
        }
    }

    static class JsonReaderExtensions
    {
        public static JsonReader Next(this JsonReader reader)
        {
            if (!reader.Read())
                throw new EndOfStreamException();
            return reader;
        }

        public static JsonReader ExpectProperty(this JsonReader reader, string name)
        {
            if (reader.TokenType != JsonToken.PropertyName || reader.Value.ToString() != name)
                throw new Exception("Expected property: " + name);
            return reader.Next();
        }

        public static object ReadProperty(this JsonReader reader, string name)
        {
            var rv = reader.ExpectProperty(name).Value;
            reader.Next();
            return rv;
        }

        public static JsonReader MoveToContent(this JsonReader reader)
        {
            if (reader.Next().TokenType != JsonToken.StartObject)
                throw new ArgumentException("Expected json object");
            return reader.Next();
        }

        public static JsonReader MoveToEnd(this JsonReader reader)
        {
            while (reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                    reader.Skip();
                else
                    throw new ArgumentException("Unexpected token " + reader.TokenType);
            }
            reader.Read();
            return reader;
        }
    }
    
    public class BsonMethodCallSerializer : JsonMethodCallSerializer
    {
        protected override JsonWriter CreateWriter(Stream stream) =>
            new BsonWriter(stream) { CloseOutput = false };

        protected override JsonReader CreateReader(Stream stream) => new BsonReader(stream)
            { DateParseHandling = DateParseHandling.None };

        public BsonMethodCallSerializer()
        {
            
        }

        public BsonMethodCallSerializer(JsonSerializer serializer) : base(serializer)
        {

        }
    }
}
