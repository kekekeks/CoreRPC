using System.Text;

namespace CoreRPC.Typescript
{
    class TypescriptCodeBuilder
    {
        StringBuilder _builder = new StringBuilder();
        private int _level = 0;
        private bool _methodHadParameters;
            
        private TypescriptCodeBuilder BeginTl(string what, string name)
        {
            Pad();
            if(_level==0)
                _builder.Append("export ");
            _builder.Append(what).Append(' ').Append(name).AppendLine(" {");
            _level++;
            return this;
        }

        public override string ToString() => _builder.ToString();

        public TypescriptCodeBuilder BeginInterface(string name) => BeginTl("interface", name);
        public TypescriptCodeBuilder BeginClass(string name) => BeginTl("class", name);
        public TypescriptCodeBuilder BeginEnum(string name) => BeginTl("enum", name);

        StringBuilder Pad() => _builder.Append(' ', _level * 4);

        public TypescriptCodeBuilder End()
        {
            _level--;
            Pad().AppendLine("}");
            return this;
        }
        public TypescriptCodeBuilder AppendInterfaceProperty(string name, string typeName)
        {
            Pad().Append("    ").Append(name).Append(" : ").Append(typeName).AppendLine(";");
            return this;
        }

        public TypescriptCodeBuilder AppendEnumValue(string name, string value)
        {
            Pad().Append("    ").Append(name).Append(" = \"").Append(value).AppendLine("\";");
            return this;
        }

        public TypescriptCodeBuilder AppendLine(string line)
        {
            Pad().AppendLine(line);
            return this;
        }
        public TypescriptCodeBuilder AppendLines(params string[] lines)
        {
            foreach (var l in lines)
                Pad().AppendLine(l);
            return this;
        }

        public TypescriptCodeBuilder BeginMethod(string name, bool @public = true)
        {
            Pad().Append(@public ? "public " : "private ").Append(name).Append(" (");
            _methodHadParameters = false;
            return this;
        }

        public TypescriptCodeBuilder AppendMethodParameter(string name, string type)
        {
            if (_methodHadParameters)
                _builder.Append(", ");
            else
                _methodHadParameters = true;
            _builder.Append(name).Append(" : ").Append(type);
            return this;
        }

        public TypescriptCodeBuilder AppendMethodReturnValueAndBeginBody(string type)
        {
            _builder.Append(") : ").Append(type).AppendLine("{");
            _level++;
            return this;
        }
    }
}