using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
	/// Binary data converter (implements pack/unpack PHP functions).
	/// </summary>
	/// <remarks>
	/// <list>
	///   <term>a</term><description>0x00-padded string</description>
	///   <term>A</term><description>0x20-padded string</description>
	///   <term>h</term><description>Hex string, low nibble first</description>
	///   <term>H</term><description>Hex string, high nibble first</description>
	///   <term>c</term><description>signed char</description>
	///   <term>C</term><description>unsigned char</description>
	///   <term>s</term><description>signed short - 16 bit, machine byte order</description>
	///   <term>S</term><description>unsigned short - 16 bit, machine byte order</description>
	///   <term>n</term><description>unsigned short - 16 bit, big endian byte order</description>
	///   <term>v</term><description>unsigned short - 16 bit, little endian byte order</description>
	///   <term>i</term><description>signed integer - 32 bit and byte order (PHP: machine dependent size)</description>
	///   <term>I</term><description>unsigned integer - 32 bit and byte order (PHP: machine dependent size)</description>
	///   <term>l</term><description>signed long - 32 bit, machine byte order</description>
	///   <term>L</term><description>unsigned long - 32 bit, machine byte order</description>
	///   <term>N</term><description>unsigned long - 32 bit, big endian byte order</description>
	///   <term>V</term><description>unsigned long - 32 bit, little endian byte order</description>
	///   <term>f</term><description>float - machine dependent size and representation</description>
	///   <term>d</term><description>double - machine dependent size and representation</description>
	///   <term>x</term><description>0x00 byte</description>
	///   <term>X</term><description>Back up one byte</description>
	///   <term>@</term><description>0x00-fill to absolute position</description>
	/// </list>
	/// </remarks>
    [PhpExtension("standard")]
    public static class PhpBitConverter
    {
        /// <summary>
        /// Integer representing '*' repeater.
        /// </summary>
        const int InfiniteRepeater = -1;

        /// <summary>
        /// Formats given integers to a string of bytes according to specified format string.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="format">The string defining the format of the result. See PHP manual for details.</param>
        /// <param name="args">Integers to be formatted to binary data according to <paramref name="format"/>.</param>
        /// <returns>Binary data.</returns>
        public static PhpString pack(Context ctx, string format, params PhpValue[] args)
        {
            if (format == null)
            {
                return new PhpString();
            }

            // the number of specifiers is at most length of the format string and usualy close to it:
            var specifiers = new char[format.Length];
            var repeaters = new int[format.Length];

            // parses specifiers and repeaters into the arrays, returns the number of used slots:
            int count = ParseFormat(ctx, format, args, specifiers, repeaters);
            if (count == 0) return new PhpString();

            // determines the result length (length) and the working buffer size (size):
            int length, size;
            GetPackedDataSize(specifiers, repeaters, count, out length, out size);

            // packs data using buffer:
            var buffer = new byte[size];
            PackInternal(ctx, buffer, args, specifiers, repeaters, count);

            // gets the result as an initial part of the buffer:
            var result = new byte[length];
            Buffer.BlockCopy(buffer, 0, result, 0, length);

            //
            return new PhpString(result);
        }

        /// <summary>
        /// Parses pack format. Stores specifiers and repeaters into the respective arrays.
        /// Repeaters are ensured to be finite and non-negative (infinite repeaters are converted to finite).
        /// Some arguments are also converted to another form (e.g. to string) because we will need that form later.
        /// </summary>
        /// <returns>Returns the number of parsed specifiers or 0 on error.</returns>
        static int ParseFormat(Context ctx, string format, PhpValue[] args, char[] specifiers, int[] repeaters)
        {
            var encoding = ctx.StringEncoding;

            int i = 0;       // current position in format
            int a = 0;       // current argument index
            int result = 0;  // number of parsed specifiers

            while (i < format.Length)
            {
                char specifier = format[i++];
                int repeater = ParseRepeater(format, ref i);

                switch (specifier)
                {
                    case 'x': // NUL byte 
                    case '@': // NUL-fill to absolute position 
                    case 'X': // Back up one byte 

                        // consumes no arguments => repeater cannot be '*'
                        if (repeater == InfiniteRepeater)
                        {
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("asterisk_ignored", specifier));
                            repeater = 1;
                        }
                        break;

                    case 'Z': // equivalent functionality to "a" for Perl compatibility
                    case 'a': // NUL-padded string
                    case 'A': // SPACE-padded string 
                    case 'h': // Hex string, low/high nibble first - converts to a string, takes n hex digits:
                    case 'H':
                        {
                            // consumes one argument:
                            if (a == args.Length)
                            {
                                PhpException.Throw(PhpError.Warning, LibResources.GetString("not_enought_arguments", specifier));
                                return 0;
                            }

                            // converts the current argument to a string and stores it back:
                            string s = args[a].ToString(ctx);
                            args[a] = (PhpValue)s;
                            a++;

                            if (specifier == 'h' || specifier == 'H')
                            {
                                if (repeater > s.Length)
                                {
                                    PhpException.Throw(PhpError.Warning, LibResources.GetString("not_enought_characters", specifier));
                                    repeater = s.Length;
                                }
                            }
                            else
                            {
                                if (encoding.GetByteCount(s) != s.Length)
                                {
                                    PhpException.Throw(PhpError.Warning, LibResources.GetString("multibyte_chars_unsupported", specifier));
                                    return 0;
                                }
                            }

                            // adjusts infinite repeater to the string length:
                            if (repeater == InfiniteRepeater)
                                repeater = s.Length;

                            break;
                        }

                    case 'c': // signed char
                    case 'C': // unsigned char 
                    case 's': // signed short (always 16 bit, machine byte order) 
                    case 'S': // unsigned short (always 16 bit, machine byte order) 
                    case 'n': // unsigned short (always 16 bit, big endian byte order) 
                    case 'v': // unsigned short (always 16 bit, little endian byte order) 
                    case 'i': // signed integer (machine dependent size and byte order) 
                    case 'I': // unsigned integer (machine dependent size and byte order) 
                    case 'l': // signed long (always 32 bit, machine byte order) 
                    case 'L': // unsigned long (always 32 bit, machine byte order) 
                    case 'N': // unsigned long (always 32 bit, big endian byte order) 
                    case 'V': // unsigned long (always 32 bit, little endian byte order) 
                    case 'f': // float (machine dependent size and representation) 
                    case 'd': // double (machine dependent size and representation) 

                        if (repeater == InfiniteRepeater)
                        {
                            // infinite repeater is converted to the number of remaining arguments (can be zero):
                            repeater = args.Length - a;
                        }
                        else if (repeater > args.Length - a)
                        {
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("not_enought_arguments", specifier));
                            return 0;
                        }

                        // consume arguments:
                        a += repeater;
                        break;

                    default:
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("unknown_format_code", specifier));
                        return 0;
                }

                specifiers[result] = specifier;
                repeaters[result] = repeater;
                result++;
            }

            // reports unused arguments:
            if (a < args.Length)
                PhpException.Throw(PhpError.Warning, LibResources.GetString("unused_arguments", args.Length - a));

            return result;
        }

        /// <summary>
        /// Parses repeater.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="i">The current position in the format string.</param>
        /// <returns>
        /// The value of repeater. Either non-negative integer or <see cref="InfiniteRepeater"/> (asterisk).
        /// </returns>
        static int ParseRepeater(string format, ref int i)
        {
            // no repeater:
            if (i == format.Length) return 1;

            // infinite repeater:
            if (format[i] == '*')
            {
                i++;
                return InfiniteRepeater;
            }

            int j = i;
            long result = Core.Convert.SubstringToLongInteger(format, format.Length - j, ref j);

            // invalid repeater or no repeater:
            if (result < 0 || i == j || unchecked((int)result) != result)   // not an int or too big int
                return 1;

            // advance index:
            i = j;
            return (int)result;
        }

        /// <summary>
        /// Computes the total size of binary data according to given specifiers and repeaters.
        /// Only <c>count</c> of them are valid.
        /// </summary>
        static void GetPackedDataSize(char[] specifiers, int[] repeaters, int count, out int resultLength, out int maxLength)
        {
            long result = 0;
            maxLength = 0;

            for (int i = 0; i < count; i++)
            {
                long repeater = repeaters[i];
                char specifier = specifiers[i];

                switch (specifier)
                {
                    case 'x':
                        // NUL byte repeated for "repeater" count:
                        result += repeater;
                        break;

                    case '@':
                        // NUL-fill to absolute position;
                        // if it is less then the current position the result is shortened
                        result = repeater;
                        break;

                    case 'X':
                        // shortens the result by "repeater" bytes (underflow has to be checked):
                        if (result < repeater)
                        {
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("outside_string", specifier));
                            result = 0;
                        }
                        else
                        {
                            result -= repeater;
                        }
                        break;

                    case 'a': // NUL-padded string
                    case 'A': // SPACE-padded string 
                    case 'c': // signed char
                    case 'C': // unsigned char 
                        result += repeater;
                        break;

                    case 's': // signed short (always 16 bit, machine byte order) 
                    case 'S': // unsigned short (always 16 bit, machine byte order) 
                    case 'n': // unsigned short (always 16 bit, big endian byte order) 
                    case 'v': // unsigned short (always 16 bit, little endian byte order) 
                        result += repeater * 2;
                        break;

                    case 'i': // signed integer (machine dependent size and byte order - always 32 bit) 
                    case 'I': // unsigned integer (machine dependent size and byte order - always 32 bit) 
                    case 'l': // signed long (always 32 bit, machine byte order) 
                    case 'L': // unsigned long (always 32 bit, machine byte order) 
                    case 'N': // unsigned long (always 32 bit, big endian byte order) 
                    case 'V': // unsigned long (always 32 bit, little endian byte order) 
                    case 'f': // float (machine dependent size and representation) 
                        result += repeater * 4;
                        break;

                    case 'd': // double (machine dependent size and representation) 
                        result += repeater * 8;
                        break;

                    case 'h': // Hex string, low/high nibble first - converts to a string, takes n hex digits from it:
                    case 'H':
                        result += (repeater + 1) / 2;
                        break;

                    default:
                        Debug.Fail("Invalid repeater");
                        break;
                }

                // checks for overflow:
                if (result > Int32.MaxValue)
                {
                    PhpException.Throw(PhpError.Warning, LibResources.GetString("binary_data_overflown", specifier));
                    result = Int32.MaxValue;
                }

                // expands the max length:
                if (result > maxLength)
                    maxLength = unchecked((int)result);
            }

            resultLength = unchecked((int)result);
        }

        /// <summary>
        /// Packs arguments into the buffer according to given specifiers and repeaters.
        /// Count specifies the number of valid specifiers/repeaters.
        /// </summary>
        static void PackInternal(Context ctx, byte[] buffer, PhpValue[] args, char[] specifiers, int[] repeaters, int count)
        {
            var encoding = ctx.StringEncoding;
            bool le = BitConverter.IsLittleEndian;
            int a = 0;            // index of the current argument
            int pos = 0;          // the position in the buffer

            PhpNumber num;
            bool le2;

            for (int i = 0; i < count; i++)
            {
                char specifier = specifiers[i];
                int repeater = repeaters[i];

                switch (specifier)
                {
                    case 'x':
                        // NUL byte repeated for "repeater" count:
                        ArrayUtils.Fill(buffer, 0, pos, repeater);
                        pos += repeater;
                        break;

                    case '@':
                        // NUL-fill to absolute position;
                        // if it is less then the current position the result is shortened
                        if (repeater > pos)
                            ArrayUtils.Fill(buffer, 0, pos, repeater - pos);
                        pos = repeater;
                        break;

                    case 'X':
                        pos = Math.Max(0, pos - repeater);
                        break;

                    case 'a': // NUL-padded string
                    case 'A': // SPACE-padded string 
                        {
                            // argument has already been converted to string:
                            string s = args[a++].ToString(ctx);

                            int length = Math.Min(s.Length, repeater);
                            int byte_count = encoding.GetBytes(s, 0, length, buffer, pos);
                            Debug.Assert(byte_count == length, "Multibyte characters not supported");

                            // padding:
                            if (repeater > length)
                                ArrayUtils.Fill(buffer, (byte)((specifier == 'a') ? 0x00 : 0x20), pos + length, repeater - length);

                            pos += repeater;
                            break;
                        }

                    case 'h': // Hex string, low/high nibble first - converts to a string, takes n hex digits from string:
                    case 'H':
                        {
                            // argument has already been converted to string:
                            string s = args[a++].ToString(ctx);

                            int nibble_shift = (specifier == 'h') ? 0 : 4;

                            for (int j = 0; j < repeater; j++)
                            {
                                int digit = Core.Convert.AlphaNumericToDigit(s[j]);
                                if (digit > 15)
                                {
                                    PhpException.Throw(PhpError.Warning, LibResources.GetString("illegal_hex_digit", specifier, s[j]));
                                    digit = 0;
                                }

                                if (j % 2 == 0)
                                {
                                    buffer[pos] = unchecked((byte)(digit << nibble_shift));
                                }
                                else
                                {
                                    buffer[pos] |= unchecked((byte)(digit << (4 - nibble_shift)));
                                    pos++;
                                }
                            }

                            // odd number of hex digits (append '0' digit):
                            if (repeater % 2 == 1) pos++;

                            break;
                        }

                    case 'c': // signed char
                    case 'C': // unsigned char 
                        while (repeater-- > 0)
                            buffer[pos++] = unchecked((byte)args[a++].ToLong());
                        break;

                    case 's': // signed short (always 16 bit, machine byte order) 
                    case 'S': // unsigned short (always 16 bit, machine byte order) 
                        while (repeater-- > 0)
                        {
                            var ni = args[a++].ToNumber(out num);
                            PackNumber(BitConverter.GetBytes(unchecked((ushort)num.ToLong())), le, buffer, ref pos);
                        }
                        break;

                    case 'n': // unsigned short (always 16 bit, big endian byte order) 
                    case 'v': // unsigned short (always 16 bit, little endian byte order) 
                        while (repeater-- > 0)
                            PackNumber(BitConverter.GetBytes(unchecked((ushort)args[a++].ToLong())), specifier == 'v', buffer, ref pos);
                        break;

                    case 'i': // signed integer (machine dependent size and byte order - always 32 bit) 
                    case 'I': // signed integer (machine dependent size and byte order - always 32 bit) 
                    case 'l': // signed long (always 32 bit, machine byte order) 
                    case 'L': // unsigned long (always 32 bit, machine byte order) 
                        while (repeater-- > 0)
                            PackNumber(BitConverter.GetBytes((int)args[a++].ToLong()), le, buffer, ref pos);
                        break;

                    case 'N': // unsigned long (always 32 bit, big endian byte order) 
                    case 'V': // unsigned long (always 32 bit, little endian byte order) 
                        while (repeater-- > 0)
                            PackNumber(BitConverter.GetBytes((int)args[a++].ToLong()), specifier == 'V', buffer, ref pos);
                        break;

                    case 'f': // float (machine dependent size and representation - size is always 4B) 
                    case 'g': // float (machine dependent size, little endian byte order)
                    case 'G': // float (machine dependent size, big endian byte order)
                        le2 = specifier == 'f' ? le : (specifier == 'g');
                        while (repeater-- > 0)
                        {
                            PackNumber(BitConverter.GetBytes(unchecked((float)args[a++].ToDouble())), le2, buffer, ref pos);
                        }
                        break;

                    case 'd': // double (machine dependent size and representation - size is always 8B) 
                    case 'e': // double (machine dependent size, little endian byte order)
                    case 'E': // double (machine dependent size, big endian byte order)
                        le2 = specifier == 'd' ? le : (specifier == 'e');
                        while (repeater-- > 0)
                        {
                            PackNumber(BitConverter.GetBytes(args[a++].ToDouble()), le2, buffer, ref pos);
                        }
                        break;

                    default:
                        Debug.Fail("Invalid specifier");
                        break;
                }
            }
        }

        /// <summary>
        /// Packs a number (integer or double) into the buffer.
        /// </summary>
        /// <param name="bytes">The number converted to bytes by <see cref="BitConverter"/>.</param>
        /// <param name="toLittleEndian">Whether the result should be in little endian encoding.</param>
        /// <param name="buffer">The buffer where to copy the covnerted number.</param>
        /// <param name="pos">The position where to start in the buffer. Advanced by the length of bytes.</param>
        static void PackNumber(byte[] bytes, bool toLittleEndian, byte[] buffer, ref int pos)
        {
            if (BitConverter.IsLittleEndian ^ toLittleEndian)
            {
                for (int i = 0; i < bytes.Length; i++)
                    buffer[pos + i] = bytes[bytes.Length - 1 - i];
            }
            else
            {
                Buffer.BlockCopy(bytes, 0, buffer, pos, bytes.Length);
            }

            pos += bytes.Length;
        }

        /// <summary>
        /// Unpacks data from a string of bytes into <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="format">The string defining the items of the result. See PHP manual for details.</param>
        /// <param name="data">The string of bytes to be unpacked.</param>
        /// <returns>The <see cref="PhpArray"/> containing unpacked data.</returns>
        public static PhpArray unpack(Context ctx, string format, PhpString data)
        {
            if (format == null) return null;
            byte[] buffer = data.ToBytes(ctx);

            var encoding = ctx.StringEncoding;
            byte[] reversed = new byte[4]; // used for reversing the order of bytes in buffer

            int i = 0;
            int pos = 0;
            PhpArray result = new PhpArray();

            while (i < format.Length)
            {
                string name;
                int repeater;
                char specifier;

                // parses specifier, repeater, and name from the format string:
                ParseFormatToken(format, ref i, out specifier, out repeater, out name);

                int remains = buffer.Length - pos;          // the number of bytes remaining in the buffer
                int size;                                   // a size of data to be extracted corresponding to the specifier  

                // repeater of '@' specifier has a special meaning:
                if (specifier == '@')
                {
                    if (repeater > buffer.Length || repeater == InfiniteRepeater)
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("outside_string", specifier));
                    else
                        pos = repeater;

                    continue;
                }

                // number of operations:
                int op_count;

                // gets the size of the data to read and adjust repeater:
                if (!GetSizeToUnpack(specifier, remains, repeater, out op_count, out size))
                {
                    PhpException.Throw(PhpError.Warning, LibResources.GetString("unknown_format_code", specifier));
                    return null;
                }

                // repeats operation determined by specifier "op_count" times;
                // if op_count is infinite then stops when the number of remaining characters is zero:
                for (int j = 0; j < op_count || op_count == InfiniteRepeater; j++)
                {
                    if (size > remains)
                    {
                        // infinite means "while data are available":
                        if (op_count == InfiniteRepeater) break;

                        PhpException.Throw(PhpError.Warning, LibResources.GetString("not_enought_input", specifier, size, remains));
                        return null;
                    }

                    PhpValue item;
                    switch (specifier)
                    {
                        case 'X': // decreases position, no value stored:
                            if (pos == 0)
                                PhpException.Throw(PhpError.Warning, LibResources.GetString("outside_string", specifier));
                            else
                                pos--;
                            continue;

                        case 'x': // advances position, no value stored
                            pos++;
                            continue;

                        case 'a': // NUL-padded string
                        case 'A': // SPACE-padded string 
                            {
                                byte pad = (byte)(specifier == 'a' ? 0x00 : 0x20);

                                int last = pos + size - 1;
                                while (last >= pos && buffer[last] == pad)
                                    last--;

                                item = (PhpValue)encoding.GetString(buffer, pos, last - pos + 1);
                                break;
                            }

                        case 'h': // Hex string, low/high nibble first - converts to a string, takes n hex digits from string:
                        case 'H':
                            {
                                int p = pos;
                                int nibble_shift = (specifier == 'h') ? 0 : 4;

                                var sb = StringBuilderUtilities.Pool.Get();
                                for (int k = 0; k < size; k++)
                                {
                                    const string hex_digits = "0123456789ABCDEF";

                                    sb.Append(hex_digits[(buffer[p] >> nibble_shift) & 0x0f]);

                                    // beware of odd repeaters!
                                    if (repeater == InfiniteRepeater || repeater > sb.Length)
                                    {
                                        sb.Append(hex_digits[(buffer[p] >> (4 - nibble_shift)) & 0x0f]);
                                    }
                                    p++;
                                }

                                item = StringBuilderUtilities.GetStringAndReturn(sb);
                                break;
                            }

                        case 'c': // signed char
                            item = (PhpValue)(int)unchecked((sbyte)buffer[pos]);
                            break;

                        case 'C': // unsigned char 
                            item = (PhpValue)(int)buffer[pos];
                            break;

                        case 's': // signed short (always 16 bit, machine byte order) 
                            item = (PhpValue)(int)BitConverter.ToInt16(buffer, pos);
                            break;

                        case 'S': // unsigned short (always 16 bit, machine byte order) 
                            item = (PhpValue)(int)BitConverter.ToUInt16(buffer, pos);
                            break;

                        case 'n': // unsigned short (always 16 bit, big endian byte order) 
                            if (BitConverter.IsLittleEndian)
                                item = (PhpValue)(int)BitConverter.ToUInt16(LoadReverseBuffer(reversed, buffer, pos, 2), 0);
                            else
                                item = (PhpValue)(int)BitConverter.ToUInt16(buffer, pos);
                            break;

                        case 'v': // unsigned short (always 16 bit, little endian byte order) 
                            if (!BitConverter.IsLittleEndian)
                                item = (PhpValue)(int)BitConverter.ToUInt16(LoadReverseBuffer(reversed, buffer, pos, 2), 0);
                            else
                                item = (PhpValue)(int)BitConverter.ToUInt16(buffer, pos);
                            break;

                        case 'i': // signed integer (machine dependent size and byte order - always 32 bit) 
                        case 'I': // unsigned integer (machine dependent size and byte order - always 32 bit) 
                        case 'l': // signed long (always 32 bit, machine byte order) 
                        case 'L': // unsigned long (always 32 bit, machine byte order) 
                            item = (PhpValue)BitConverter.ToInt32(buffer, pos);
                            break;

                        case 'N': // unsigned long (always 32 bit, big endian byte order) 
                            item = (PhpValue)unchecked(((int)buffer[pos] << 24) + (buffer[pos + 1] << 16) + (buffer[pos + 2] << 8) + buffer[pos + 3]);
                            break;

                        case 'V': // unsigned long (always 32 bit, little endian byte order) 
                            item = (PhpValue)unchecked(((int)buffer[pos + 3] << 24) + (buffer[pos + 2] << 16) + (buffer[pos + 1] << 8) + buffer[pos + 0]);
                            break;

                        case 'f': // float (machine dependent size and representation - size is always 4B) 
                            item = (PhpValue)(double)BitConverter.ToSingle(buffer, pos);
                            break;

                        case 'd': // double (machine dependent size and representation - size is always 8B) 
                            item = (PhpValue)BitConverter.ToDouble(buffer, pos);
                            break;

                        default:
                            Debug.Fail("Invalid specifier.");
                            return null;
                    }

                    AddValue(result, name, item, op_count, j);

                    pos += size;
                    remains -= size;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a size of data to be unpacked according to the specifier.
        /// </summary>
        static bool GetSizeToUnpack(char specifier, int remains, int repeater, out int op_count, out int size)
        {
            switch (specifier)
            {
                case '@':
                    Debug.Fail("@ specifier has already been processed");
                    size = 0;
                    op_count = repeater;
                    break;

                case 'X':
                    size = -1;
                    op_count = repeater;
                    break;

                case 'a': // NUL-padded string
                case 'A': // SPACE-padded string 
                    size = (repeater != InfiniteRepeater) ? repeater : remains;
                    op_count = 1;
                    break;

                case 'h': // Hex string, low/high nibble first - converts to a string, takes n hex digits from string:
                case 'H':
                    size = (repeater != InfiniteRepeater) ? (repeater + 1) / 2 : remains;
                    op_count = 1;
                    break;

                case 'x': // NUL
                case 'c': // signed char
                case 'C': // unsigned char 
                    size = 1;
                    op_count = repeater;
                    break;

                case 's': // signed short (always 16 bit, machine byte order) 
                case 'S': // unsigned short (always 16 bit, machine byte order) 
                case 'n': // unsigned short (always 16 bit, big endian byte order) 
                case 'v': // unsigned short (always 16 bit, little endian byte order) 
                    size = 2;
                    op_count = repeater;
                    break;

                case 'i': // signed integer (machine dependent size and byte order - always 32 bit) 
                case 'I': // signed integer (machine dependent size and byte order - always 32 bit) 
                case 'l': // signed long (always 32 bit, machine byte order) 
                case 'L': // unsigned long (always 32 bit, machine byte order) 
                case 'N': // unsigned long (always 32 bit, big endian byte order) 
                case 'V': // unsigned long (always 32 bit, little endian byte order) 
                case 'f': // float (machine dependent size and representation - size is always 4B) 
                    size = 4;
                    op_count = repeater;
                    break;

                case 'd': // double (machine dependent size and representation - size is always 8B) 
                    size = 8;
                    op_count = repeater;
                    break;

                default:
                    size = 0;
                    op_count = repeater;
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Parses format token "{specifier}{repeater}?{name}?/?"
        /// </summary>
        static private void ParseFormatToken(string format, ref int i, out char specifier, out int repeater, out string name)
        {
            Debug.Assert(i < format.Length);

            specifier = format[i++];
            repeater = ParseRepeater(format, ref i);

            if (i == format.Length)
            {
                name = "";
                return;
            }

            int slash = format.IndexOf('/', i);
            if (slash >= 0)
            {
                name = format.Substring(i, slash - i);
                i = slash + 1;
            }
            else
            {
                name = format.Substring(i);
                i = format.Length;
            }
        }

        /// <summary>
        /// Adds unpacked value to the resulting array.
        /// </summary>
        static private void AddValue(PhpArray result, string name, PhpValue value, int repeater, int index)
        {
            if (name != "")
            {
                if (repeater > 1 || repeater == InfiniteRepeater)
                    name += (index + 1);

                result[name] = value;
            }
            else
            {
                result[index + 1] = value;
            }
        }

        /// <summary>
        /// Loads reversed bytes from buffer to an array.
        /// </summary>
        static private byte[] LoadReverseBuffer(byte[] reverse, byte[] buffer, int pos, int count)
        {
            for (int i = 0; i < count; i++)
                reverse[i] = buffer[pos + count - i - 1];

            return reverse;
        }

//        #region Unit Testing
//#if DEBUG

//        public static void Test_Pack()
//        {
//            pack("ccc", -5, "0001x", "-8").Dump(Console.Out);
//            pack("c*", -5, "0001x", "-8").Dump(Console.Out);
//            pack("cCsS", 1, 1, 1, 1).Dump(Console.Out);
//            pack("nviI", 1, 1, 1, 1).Dump(Console.Out);
//            pack("lLNV", 1, 1, 1, 1).Dump(Console.Out);
//            pack("fd", 1, 1).Dump(Console.Out);
//            pack("H*", "abcde").Dump(Console.Out);
//            pack("h*", "abcde").Dump(Console.Out);
//            pack("H*", "abcd").Dump(Console.Out);
//            pack("h*", "abcd").Dump(Console.Out);
//            pack("A*", "hello").Dump(Console.Out);
//            pack("a2", "hello").Dump(Console.Out);
//            pack("a10", "hello").Dump(Console.Out);
//            pack("A10", "hello").Dump(Console.Out);
//            pack("nvc*", 0x1234, 0x5678, 65, 66).Dump(Console.Out);
//            pack("x10X5x8x1X2x1X2").Dump(Console.Out);
//            pack("@5s2c3", "+5e10", "007xasd", "-6", "49", ".1").Dump(Console.Out);
//            pack("@5f2c3", "+5e10", "007xasd", "-6", "49", ".1").Dump(Console.Out);
//            pack("a*", "ìšèøžýáíé").Dump(Console.Out);
//            pack("a0", "xxx").Dump(Console.Out);
//        }

//        public static void Test_Unpack()
//        {
//            unpack("@2/a*x", new PhpString("1234567812123456781212345678121234567812")).Dump(Console.Out);
//            unpack("@2/@100/a*x", new PhpString("1234567812123456781212345678121234567812")).Dump(Console.Out);
//            unpack("@2/X3/a*x", new PhpString("1234567812123456781212345678121234567812")).Dump(Console.Out);
//            unpack("a*x/a*y", new PhpString("1234567812123456781212345678121234567812")).Dump(Console.Out);
//            unpack("xx/a*y", new PhpString("1234567812123456781212345678121234567812")).Dump(Console.Out);
//            unpack("ca/Cb", new PhpString("\x90\x90")).Dump(Console.Out);

//            unpack("@5/s2x/c3y", Pack("@5s2c3", "+5e10", "007xasd", "-6", "49", ".1")).Dump(Console.Out);
//            unpack("na/vb/c*c", Pack("nvc*", 1234, 5678, 65, 66)).Dump(Console.Out);
//            unpack("h*", pack("h*", "ABCDEF123456")).Dump(Console.Out);
//        }

//#endif
//        #endregion
    }
}
