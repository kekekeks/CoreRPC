using System;
using System.Collections.Generic;
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
        private readonly bool _useBson;

        public JsonMethodCallSerializer(JsonSerializer serializer, bool useBson = false)
        {
            _serializer = serializer;
            _useBson = useBson;
        }

        public JsonMethodCallSerializer(bool useBson = false) : this(new JsonSerializer(), useBson)
        {

        }

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        JsonWriter CreateWriter(Stream stream) => _useBson
            ? (JsonWriter) new BsonWriter(stream) {CloseOutput = false}
            : new JsonTextWriter(new StreamWriter(stream, Utf8, 1024, true));

        JsonReader CreateReader(Stream stream) => _useBson
            ? (JsonReader) new BsonReader(stream)
            : new JsonTextReader(new StreamReader(stream));

        public void SerializeCall(Stream stream, IMethodBinder binder, string target, MethodCall call)
        {
            var sig = binder.GetMethodSignature(call.Method);
            using (var writer = CreateWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Target");
                writer.WriteValue(target);
                writer.WritePropertyName("MethodSignature");
                writer.WriteValue(_useBson ? (object) sig : Convert.ToBase64String(sig));
                writer.WritePropertyName("Arguments");
                _serializer.Serialize(writer, call.Arguments);
                writer.WriteEndObject();
            }
        }



        public MethodCall DeserializeCall(Stream stream, IMethodBinder binder, ITargetSelector selector)
        {
            var reader = CreateReader(stream).MoveToContent();
            var rv = new MethodCall
            {
                Target = selector.GetTarget(reader.ReadProperty("Target").ToString())
            };

            var osig = reader.ReadProperty("MethodSignature");
            var sig = osig as byte[] ?? Convert.FromBase64String((string) osig);

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
            return rv;
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
}
