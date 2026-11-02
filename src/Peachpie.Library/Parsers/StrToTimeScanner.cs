using System.IO;

namespace Pchp.Library.DateTime
{
	partial class Scanner
    {
        public Scanner(string input) : this(new StringReader(input))
        {
        }

        public Tokens Next() => GetNextToken();
    }
}
