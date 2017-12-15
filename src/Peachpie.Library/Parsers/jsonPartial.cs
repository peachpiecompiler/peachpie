using System;
using System.Diagnostics;
using Pchp.Core;

namespace Pchp.Library.Json
{
    [PhpHidden]
    partial class Parser
    {
        /// <summary>Simple linked list.</summary>
        internal class Node<T>
        {
            /// <summary>Node value.</summary>
            public T Value { get; private set; }

            /// <summary>Next node in the list.</summary>
            public Node<T> Next { get; set; }

            public Node(T value, Node<T> next = null)
            {
                this.Value = value;
                this.Next = next;
            }
        }

        protected override Position CombinePositions(Position first, Position last)
        {
            return new Position(first.Start, last.Start + last.Length - first.Start);
        }

        readonly PhpSerialization.JsonSerializer.DecodeOptions/*!*/decodeOptions;

        internal Parser(PhpSerialization.JsonSerializer.DecodeOptions/*!*/decodeOptions)
        {
            Debug.Assert(decodeOptions != null);

            this.decodeOptions = decodeOptions;
        }

        public PhpValue Result { get; private set; }
    }

    [PhpHidden]
    partial class Lexer
    {
        Position token_end_pos;

        char Map(char c)
        {
            return (c > SByte.MaxValue) ? 'a' : c;
        }
    }
}
