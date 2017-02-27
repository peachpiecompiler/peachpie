using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library
{
    [PhpExtension("xml")]
    public static class PhpXml
    {
        #region Constants

        [PhpHidden]
        public enum XmlParserError
        {
            XML_ERROR_NONE = 0,
            XML_ERROR_GENERIC = 1,
            XML_ERROR_NO_MEMORY = 1,
            XML_ERROR_SYNTAX = 1,
            XML_ERROR_NO_ELEMENTS = 1,
            XML_ERROR_INVALID_TOKEN = 1,
            XML_ERROR_UNCLOSED_TOKEN = 1,
            XML_ERROR_PARTIAL_CHAR = 1,
            XML_ERROR_TAG_MISMATCH = 1,
            XML_ERROR_DUPLICATE_ATTRIBUTE = 1,
            XML_ERROR_JUNK_AFTER_DOC_ELEMENT = 1,
            XML_ERROR_PARAM_ENTITY_REF = 1,
            XML_ERROR_UNDEFINED_ENTITY = 1,
            XML_ERROR_RECURSIVE_ENTITY_REF = 1,
            XML_ERROR_ASYNC_ENTITY = 1,
            XML_ERROR_BAD_CHAR_REF = 1,
            XML_ERROR_BINARY_ENTITY_REF = 1,
            XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF = 1,
            XML_ERROR_MISPLACED_XML_PI = 1,
            XML_ERROR_UNKNOWN_ENCODING = 1,
            XML_ERROR_INCORRECT_ENCODING = 1,
            XML_ERROR_UNCLOSED_CDATA_SECTION = 1,
            XML_ERROR_EXTERNAL_ENTITY_HANDLING = 1,
        }

        const int XML_ERROR_NONE = (int)XmlParserError.XML_ERROR_NONE;
        const int XML_ERROR_GENERIC = (int)XmlParserError.XML_ERROR_GENERIC;
        const int XML_ERROR_NO_MEMORY = (int)XmlParserError.XML_ERROR_NO_MEMORY;
        const int XML_ERROR_SYNTAX = (int)XmlParserError.XML_ERROR_SYNTAX;
        const int XML_ERROR_NO_ELEMENTS = (int)XmlParserError.XML_ERROR_NO_ELEMENTS;
        const int XML_ERROR_INVALID_TOKEN = (int)XmlParserError.XML_ERROR_INVALID_TOKEN;
        const int XML_ERROR_UNCLOSED_TOKEN = (int)XmlParserError.XML_ERROR_UNCLOSED_TOKEN;
        const int XML_ERROR_PARTIAL_CHAR = (int)XmlParserError.XML_ERROR_PARTIAL_CHAR;
        const int XML_ERROR_TAG_MISMATCH = (int)XmlParserError.XML_ERROR_TAG_MISMATCH;
        const int XML_ERROR_DUPLICATE_ATTRIBUTE = (int)XmlParserError.XML_ERROR_DUPLICATE_ATTRIBUTE;
        const int XML_ERROR_JUNK_AFTER_DOC_ELEMENT = (int)XmlParserError.XML_ERROR_JUNK_AFTER_DOC_ELEMENT;
        const int XML_ERROR_PARAM_ENTITY_REF = (int)XmlParserError.XML_ERROR_PARAM_ENTITY_REF;
        const int XML_ERROR_UNDEFINED_ENTITY = (int)XmlParserError.XML_ERROR_UNDEFINED_ENTITY;
        const int XML_ERROR_RECURSIVE_ENTITY_REF = (int)XmlParserError.XML_ERROR_RECURSIVE_ENTITY_REF;
        const int XML_ERROR_ASYNC_ENTITY = (int)XmlParserError.XML_ERROR_ASYNC_ENTITY;
        const int XML_ERROR_BAD_CHAR_REF = (int)XmlParserError.XML_ERROR_BAD_CHAR_REF;
        const int XML_ERROR_BINARY_ENTITY_REF = (int)XmlParserError.XML_ERROR_BINARY_ENTITY_REF;
        const int XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF = (int)XmlParserError.XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF;
        const int XML_ERROR_MISPLACED_XML_PI = (int)XmlParserError.XML_ERROR_MISPLACED_XML_PI;
        const int XML_ERROR_UNKNOWN_ENCODING = (int)XmlParserError.XML_ERROR_UNKNOWN_ENCODING;
        const int XML_ERROR_INCORRECT_ENCODING = (int)XmlParserError.XML_ERROR_INCORRECT_ENCODING;
        const int XML_ERROR_UNCLOSED_CDATA_SECTION = (int)XmlParserError.XML_ERROR_UNCLOSED_CDATA_SECTION;
        const int XML_ERROR_EXTERNAL_ENTITY_HANDLING = (int)XmlParserError.XML_ERROR_EXTERNAL_ENTITY_HANDLING;

        [PhpHidden]
        public enum XmlOption
        {
            XML_OPTION_CASE_FOLDING,
            XML_OPTION_SKIP_TAGSTART,
            XML_OPTION_SKIP_WHITE,
            XML_OPTION_TARGET_ENCODING
        }

        public const int XML_OPTION_CASE_FOLDING = (int)XmlOption.XML_OPTION_CASE_FOLDING;
        public const int XML_OPTION_SKIP_TAGSTART = (int)XmlOption.XML_OPTION_SKIP_TAGSTART;
        public const int XML_OPTION_SKIP_WHITE = (int)XmlOption.XML_OPTION_SKIP_WHITE;
        public const int XML_OPTION_TARGET_ENCODING = (int)XmlOption.XML_OPTION_TARGET_ENCODING;

        #endregion

        #region utf8_encode, utf8_decode

        /// <summary>
        /// ISO-8859-1 <see cref="Encoding"/>.
        /// </summary>
        static Encoding/*!*/ISO_8859_1_Encoding
        {
            get
            {
                if (_ISO_8859_1_Encoding == null)
                {
                    _ISO_8859_1_Encoding = Encoding.GetEncoding("ISO-8859-1");
                    Debug.Assert(_ISO_8859_1_Encoding != null);
                }

                return _ISO_8859_1_Encoding;
            }
        }
        static Encoding _ISO_8859_1_Encoding = null;

        /// <summary>
        /// This function encodes the string data to UTF-8, and returns the encoded version. UTF-8 is
        /// a standard mechanism used by Unicode for encoding wide character values into a byte stream.
        /// UTF-8 is transparent to plain ASCII characters, is self-synchronized (meaning it is 
        /// possible for a program to figure out where in the bytestream characters start) and can be
        /// used with normal string comparison functions for sorting and such. PHP encodes UTF-8
        /// characters in up to four bytes.
        /// </summary>
        /// <param name="data">An ISO-8859-1 string. </param>
        /// <returns>Returns the UTF-8 translation of data.</returns>
        //[return:CastToFalse]
        public static string utf8_encode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }

            // this function transforms ISO-8859-1 binary string into UTF8 string
            // since our internal representation is native CLR string - UTF16, we have changed this semantic

            //string encoded;

            //if (!data.ContainsBinayString)
            //{
            //    encoded = (string)data;
            //}
            //else
            //{
            //    // if we got binary string, assume it's ISO-8859-1 encoded string and convert it to System.String
            //    encoded = ISO_8859_1_Encoding.GetString((data).ToBytes);
            //}

            //// return utf8 encoded data
            //return (Configuration.Application.Globalization.PageEncoding == Encoding.UTF8) ?
            //    (object)encoded : // PageEncoding is UTF8, we can keep .NET string, which will be converted to UTF8 byte stream as it would be needed
            //    (object)new PhpBytes(Encoding.UTF8.GetBytes(encoded));   // conversion of string to byte[] would not respect UTF8 encoding, convert it now

            return data;
        }

        /// <summary>
        /// This function decodes data, assumed to be UTF-8 encoded, to ISO-8859-1.
        /// </summary>
        /// <param name="data">An ISO-8859-1 string. </param>
        /// <returns>Returns the UTF-8 translation of data.</returns>
        public static PhpString utf8_decode(string data)
        {
            if (data == null)
            {
                return new PhpString();  // empty (binary) string
            }

            // this function converts the UTF8 representation to ISO-8859-1 representation
            // we assume CLR string (UTF16) as input as it is our internal representation

            // if we got System.String string, convert it from UTF16 CLR representation into ISO-8859-1 binary representation
            return new PhpString(ISO_8859_1_Encoding.GetBytes(data));
        }

        #endregion
    }
}
