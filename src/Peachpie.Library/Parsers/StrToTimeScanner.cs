using System.IO;

namespace Pchp.Library.DateTime
{
	partial class Scanner
    {
        public Scanner(string input) : this(new StringReader(input))
        {
        }

        public Tokens Next()
        {
            var token = GetNextToken();

            // Cross-platform CsLex returns Tokens.ERROR instead of Tokens.EOF
            // when input is exhausted with no pending token (last_accept_state
            // stays NoState). Treat an ERROR with no recorded errors as
            // end-of-input so the parsing loop can complete successfully.
            if (token == Tokens.ERROR && Errors == 0)
            {
                return Tokens.EOF;
            }

            return token;
        }
    }
}
