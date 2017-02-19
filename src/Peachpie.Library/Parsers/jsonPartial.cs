using System;
using Pchp.Core;

namespace Pchp.Library.Json
{
    [PhpHidden]
    partial class Parser
    {
        protected override Position CombinePositions(Position first, Position last)
        {
            return new Position(first.Start, last.Start + last.Length - first.Start);
        }
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
