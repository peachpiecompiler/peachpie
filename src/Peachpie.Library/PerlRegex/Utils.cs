using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.PerlRegex
{
    internal static class Utils
    {
        public static bool IsDelimiterChar(char ch)
        {
            switch (ch)
            {
                case '\\':
                case '+':
                case '*':
                case '?':
                case '[':
                case '^':
                case ']':
                case '$':
                case '(':
                case ')':
                case '{':
                case '}':
                case '=':
                case '!':
                case '<':
                case '>':
                case '|':
                case ':':
                case '.':
                    return true;

                default:
                    return false;
            }
        }
    }
}
