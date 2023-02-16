using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JsonDiff
{
    public class AnsiColoredDiffJsonWriter : JsonTextWriter
    {
        TextWriter _writer;

        string _propertyName;
        bool _isColor;

        public AnsiColoredDiffJsonWriter(TextWriter textWriter) : base(textWriter) => _writer = textWriter;

        public override void WritePropertyName(string name)
        {
            _propertyName = name;
            WriteBeginColor();
            base.WritePropertyName(name);
        }

        protected override void WriteEnd(JsonToken token)
        {
            WriteEndColor();
            base.WriteEnd(token);
        }

        protected override void WriteValueDelimiter()
        {
            if (WriteState != WriteState.Array)
                WriteEndColor();
            base.WriteValueDelimiter();
            if (WriteState != WriteState.Array)
                WriteBeginColor();
        }

        private void WriteBeginColor()
        {
            if (!String.IsNullOrWhiteSpace(_propertyName))
            {
                if (_propertyName.StartsWith('-'))
                {
                    _writer.Write("\x1b[31m");
                    _isColor = true;
                }
                if (_propertyName.StartsWith('+'))
                {
                    _writer.Write("\x1b[32m");
                    _isColor |= true;
                }
            }
        }

        private void WriteEndColor()
        {
            if (_isColor)
            {
                _writer.Write("\x1b[0m");
                _isColor |= false;
            }
        }
    }
}
