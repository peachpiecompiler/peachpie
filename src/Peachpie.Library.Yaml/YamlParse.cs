using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Peachpie.Library.Yaml
{
    [PhpExtension(YamlExtension.Name)]
    public static class YamlParse
    {
        public static PhpValue yaml_parse(string input, int pos = 0)
        {
            return yaml_parse(input, pos, out _, null);
        }

        public static PhpValue yaml_parse(string input, int pos, out int ndocs, PhpArray callbacks = null)
        {
            var parser = new YamlParser(new StringReader(input));

            parser.Parse();

            ndocs = parser.DocumentsCount;

            return parser.GetDocument(pos);
        }
    }

    sealed class YamlParser : Parser, IParsingEventVisitor
    {
        readonly PhpArray _callbacks;

        public int DocumentsCount => throw new NotImplementedException();

        public PhpValue GetDocument(int pos)
        {
            throw new NotImplementedException();
        }

        public YamlParser(TextReader input, PhpArray callbacks = null) : base(input)
        {
            _callbacks = callbacks;
        }

        public void Parse()
        {
            while (MoveNext())
            {
                Current.Accept(this);
            }
        }

        void IParsingEventVisitor.Visit(StreamStart e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(StreamEnd e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(DocumentStart e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(DocumentEnd e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(AnchorAlias e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(Scalar e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(SequenceStart e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(SequenceEnd e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(MappingStart e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(MappingEnd e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }

        void IParsingEventVisitor.Visit(Comment e)
        {
            Debug.WriteLine(e.GetType().Name + ": " + e.ToString());
        }
    }
}
