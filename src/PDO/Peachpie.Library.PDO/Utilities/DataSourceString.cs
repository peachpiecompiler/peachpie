using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.PDO.Utilities
{
    /// <summary>
    /// Helper class for manipulation with data source string as it is in PDO.
    /// </summary>
    [PhpHidden]
    public static class DataSourceString
    {
        static bool TryNextNameValue(ReadOnlySpan<char> datasource, ref int position, out ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
        {
            // look for '='
            int eq = position;
            while (eq < datasource.Length)
            {
                if (datasource[eq] == '=')
                {
                    name = datasource.Slice(position, eq - position);

                    // look for ';' or end
                    int end = ++eq;
                    while (end < datasource.Length && datasource[end] != ';')
                    {
                        end++;
                    }

                    value = datasource.Slice(eq, end - eq);
                    position = end + 1;
                    return true;
                }

                eq++;
            }
            
            //
            name = default(ReadOnlySpan<char>);
            value = default(ReadOnlySpan<char>);
            return false;
        }

        /// <summary>
        /// Parses the raw data source values.
        /// </summary>
        public static void ParseNameValue<T>(ReadOnlySpan<char> datasource, T @object, Action<T, string, string>/*!!*/callback)
        {
            int position = 0;

            while (TryNextNameValue(datasource, ref position, out var name, out var value))
            {
                callback(@object, name.Trim().ToString(), value.Trim().ToString());
            }
        }
    }
}
