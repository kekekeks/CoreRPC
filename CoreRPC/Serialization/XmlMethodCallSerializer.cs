using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using CoreRPC.Binding;
using CoreRPC.Routing;
using CoreRPC.Transferable;
using CoreRPC.Utility;

namespace CoreRPC.Serialization
{
    /// <summary>
    /// Implementation limitations: root element children MUST be in exactly same order that is used by serializer
    /// </summary>
    public class XmlMethodCallSerializer : IMethodCallSerializer
    {
        private static readonly XmlAttributeOverrides Attributes;

        private static readonly ConcurentBulkReadOptimizedCache<Type, ISerializableFactory> Cache =
            new ConcurentBulkReadOptimizedCache<Type, ISerializableFactory> (t => (ISerializableFactory)Activator.CreateInstance (typeof (ArgumentContainer<>).MakeGenericType (t)));

        public interface ISerializable
        {
            [XmlIgnore]
            object Value { get; set; }
        }

        public interface ISerializableFactory
        {
            [XmlIgnore]
            XmlSerializer Serializer { get; }

            ISerializable Create();
        }

        [XmlRoot("Object")]
        public class ArgumentContainer<T> : ISerializable, ISerializableFactory
        {
// ReSharper disable StaticFieldInGenericType
            private static readonly XmlSerializer Serializer = new XmlSerializer(typeof (ArgumentContainer<T>));
// ReSharper restore StaticFieldInGenericType
            [XmlIgnore]
            XmlSerializer ISerializableFactory.Serializer { get { return Serializer; } }

            public ISerializable Create()
            {
                return new ArgumentContainer<T>();
            }

            [XmlIgnore]
            object ISerializable.Value
            {
                get { return Value; }
                set { Value = (T) value; }
            }

            public T Value { get; set; }
        }



        public void SerializeCall(Stream stream, IMethodBinder info, string target, MethodCall call)
        {
            var writer = XmlWriter.Create (stream);
            writer.WriteStartElement ("MethodCall");
            writer.WriteElementString ("Target", target);
            writer.WriteElementString("MethodSignature", Convert.ToBase64String(info.GetMethodSignature(call.Method)));

            writer.WriteStartElement("Arguments");
            var ctx = Cache.GetContext ();


            var parameters = call.Method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var serializer = ctx[parameter.ParameterType];
                var container = serializer.Create();
                container.Value = call.Arguments[i];
                serializer.Serializer.Serialize(writer, container);
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Flush();

        }

        public MethodCall DeserializeCall(Stream stream, IMethodBinder info, ITargetSelector selector)
        {
            var reader = XmlReader.Create(stream);

            while (reader.NodeType != XmlNodeType.Element)
                reader.Read();

            var rv = new MethodCall();

            reader.ReadStartElement("MethodCall"); //Skip root node

            rv.Target = selector.GetTarget(reader.ReadElementContentAsString());
            rv.Method = info.GetInfoProviderFor(rv.Target).GetMethod(Convert.FromBase64String(reader.ReadElementContentAsString()));

            reader.ReadStartElement("Arguments");

            var ctx = Cache.GetContext();
            rv.Arguments = rv.Method.GetParameters().Select(parameter => ((ISerializable) ctx[parameter.ParameterType].Serializer.Deserialize(reader)).Value).ToArray();
            return rv;
        }

        public void SerializeResult(Stream stream, object result)
        {
            var writer = XmlWriter.Create(stream);
            writer.WriteStartElement("MethodCallResult");
            if(result!=null)
            {
                var ser = Cache.GetContext()[result.GetType()];
                var value = ser.Create();
                value.Value = result;
                ser.Serializer.Serialize(writer, value);
            }
            writer.WriteEndElement();
            writer.Flush();
        }

        public void SerializeException (Stream stream, string exception)
        {
            var writer = XmlWriter.Create (stream);
            writer.WriteStartElement ("MethodCallResult");
            writer.WriteElementString("Exception", exception);
            writer.WriteEndElement ();
            writer.Flush ();
        }


        public MethodCallResult DeserializeResult(Stream stream, Type expectedType)
        {
            var reader = XmlReader.Create(stream);
            while (reader.NodeType != XmlNodeType.Element)
                reader.Read();

            if (!reader.IsEmptyElement)
                reader.ReadStartElement();

            var rv = new MethodCallResult();

            if (reader.Name == "Exception")
                rv.Exception = reader.ReadElementContentAsString();
            else if (reader.Name == "Object")
            {
                var ser = Cache.GetContext()[expectedType];
                rv.Result = ((ISerializable) ser.Serializer.Deserialize(reader)).Value;
            }
            return rv;
        }
    }
}
