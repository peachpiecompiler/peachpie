using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        // TODO: output filter stack
        // TOOD: CreateConsoleOutputFilter
        // TODO: echo using current output filter

        #region Echo

        public void Echo(object value)
        {
            if (value != null)
                Echo(value.ToString());
        }

        public void Echo(string value)
        {
            if (value != null)
                ConsoleImports.Write(value);
        }

        public void Echo(PhpString value)
        {
            Echo(value.ToString(this));    // TODO: echo string builder chunks to avoid concatenation
        }

        public void Echo(PhpValue value)
        {
            ConsoleImports.Write(value.ToString(this));
        }

        public void Echo(PhpNumber value)
        {
            if (value.IsLong)
                Echo(value.Long);
            else
                Echo(value.Double);
        }

        public void Echo(double value)
        {
            ConsoleImports.Write(Convert.ToString(value, this));
        }

        public void Echo(long value)
        {
            ConsoleImports.Write(value.ToString());
        }

        public void Echo(int value)
        {
            ConsoleImports.Write(value.ToString());
        }

        #endregion
    }
}
