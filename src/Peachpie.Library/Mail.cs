using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    [PhpExtension("standard")]
    public static class Mail
    {
        public static bool mail(Context ctx, string to, string subject, string message, string additional_headers = null, string additional_parameters = null)
        {
            // to and subject cannot contain newlines, replace with spaces
            to = (to != null) ? to.Replace("\r\n", " ").Replace('\n', ' ') : "";
            subject = (subject != null) ? subject.Replace("\r\n", " ").Replace('\n', ' ') : "";

            Debug.WriteLine("MAILER", "mail('{0}','{1}','{2}','{3}')", to, subject, message, additional_headers);

            var config = ctx.Configuration.Core;

            // additional_parameters
            additional_parameters = config.ForceExtraMailParameters ?? additional_parameters;

            // set SMTP server we are using
            var client = new RawSmtpClient(config.SmtpServer, config.SmtpPort);

            // X-PHP-Originating-Script
            if (config.AddXHeader)
            {
                additional_headers = "X-PHP-Originating-Script: 1:" + ctx.MainScriptFile.Path + "\n" + additional_headers;
            }

            try
            {
                client.Connect();
                client.SendMessage(
                    config.DefaultFromHeader, to,
                    subject,
                    additional_headers,
                    message);
                return true;
            }
            catch (Exception e)
            {
                string error_message = e.Message;
                Exception inner = e;
                while ((inner = inner.InnerException) != null)
                    error_message += "; " + inner.Message;

                PhpException.Throw(PhpError.Warning, Resources.LibResources.cannot_send_email, error_message);
                return false;
            }
            finally
            {
                client.Disconnect();
            }
        }

        /// <summary>
		/// Counts hash value needed by EZMLM.
		/// </summary>
		/// <param name="addr">Mail address for which is hash value calculating.</param>
		/// <returns>Calculated hash value.</returns>
		public static int ezmlm_hash(string addr)
        {
            // this algorithm is assumed from PHP source code

            uint h = 5381; // must be 32-bit unsigned
            addr = addr.ToLower();

            unchecked // overflow may occur, this is OK.
            {
                for (int j = 0; j < addr.Length; j++)
                {
                    h = (h + (h << 5)) ^ (uint)addr[j];
                }
            }

            h = (h % 53);

            return (int)h;
        }

        #region Mail headers parsing

        ///// <summary>
        ///// Extracts mail headers from string <c>headers</c> and if the string contains supported headers,
        ///// appropriate fields are set to <c>MailMessage mm</c> object.
        ///// Supported headers are: Cc, Bcc, From, Priority, Content-type. Others are ignored.
        ///// </summary>
        ///// <param name="headers">String containing mail headers.</param>
        ///// <param name="mm">MailMessage object to set fields according to <c>headers</c>.</param>
        //static void SetMailHeaders(string headers, MailMessage mm)
        //{
        //    // parse additional headers
        //    Regex headerRegex = new Regex("^([^:]+):[ \t]*(.+)$");
        //    Match headerMatch;

        //    int line_begin, line_end = -1;
        //    while (true)
        //    {
        //        line_begin = line_end + 1;

        //        // search for non-empty line
        //        while (line_begin < headers.Length && (headers[line_begin] == '\n' || headers[line_begin] == '\r'))
        //            line_begin++;
        //        if (line_begin >= headers.Length)
        //            break;

        //        // find the line end
        //        line_end = line_begin + 1;
        //        while (line_end < headers.Length && headers[line_end] != '\n' && headers[line_end] != '\r')
        //            line_end++;

        //        string header = headers.Substring(line_begin, line_end - line_begin);
        //        headerMatch = headerRegex.Match(header);

        //        // ignore wrong formatted headers
        //        if (!headerMatch.Success)
        //            continue;

        //        string sw = headerMatch.Groups[1].Value.Trim().ToLower();
        //        switch (sw)
        //        {
        //            case "cc":
        //                mm.CC.Add(ExtractMailAddressesOnly(headerMatch.Groups[2].Value, Int32.MaxValue));
        //                break;
        //            case "bcc":
        //                mm.Bcc.Add(ExtractMailAddressesOnly(headerMatch.Groups[2].Value, Int32.MaxValue));
        //                break;
        //            case "from":
        //                string from = ExtractMailAddressesOnly(headerMatch.Groups[2].Value, 1);
        //                if (!string.IsNullOrEmpty(from))
        //                {
        //                    try
        //                    {
        //                        mm.From = new MailAddress(from);
        //                    }
        //                    catch (FormatException)
        //                    { }
        //                }
        //                break;
        //            case "priority":
        //                mm.Priority = ExtractPriority(headerMatch.Groups[2].Value);
        //                break;
        //            case "content-type":
        //                ExtractContentType(headerMatch.Groups[2].Value, mm);
        //                break;

        //            default:
        //                mm.Headers.Add(headerMatch.Groups[1].Value.Trim(), headerMatch.Groups[2].Value);
        //                break;
        //        }
        //    }
        //}

        ///// <summary>
        ///// Converts semicolon separated list of email addresses and names of email owners
        ///// to semicolon separated list of only email addresses.
        ///// </summary>
        ///// <param name="emails">Semicolon separated list of email addresses and names.</param>
        ///// <param name="max">Max number of emails returned.</param>
        ///// <returns>Semicolon separated list of email addresses only.</returns>
        //static string ExtractMailAddressesOnly(string emails, int max)
        //{
        //    var mailsOnly = new StringBuilder();
        //    var regWithName = new Regex("^[ \t]*([^<>]*?)[ \t]*<[ \t]*([^<>]*?)[ \t]*>[ \t]*$");
        //    var regEmail = new Regex("^[ \t]*[^@ \t<>]+@[^@ \t<>]+.[^@ \t<>]+[ \t]*$");

        //    Match m, m2;
        //    string toAppend = "";
        //    string[] mailsArray = emails.Split(';');
        //    foreach (string mail in mailsArray)
        //    {
        //        m = regWithName.Match(mail);
        //        if (m.Success) // mail with name
        //        {
        //            Group gr;
        //            for (int i = 1; i < m.Groups.Count; i++)
        //            {
        //                gr = m.Groups[i];
        //                m2 = regEmail.Match(gr.Value);
        //                if (m2.Success)
        //                {
        //                    toAppend = m2.Value;
        //                }
        //            }
        //            // if an e-mail is in <..> we forget previous email found out of <..> (the name looks like e-mail address)
        //            mailsOnly.Append(toAppend);
        //            mailsOnly.Append(';');
        //        }
        //        else
        //        {
        //            m2 = regEmail.Match(mail);
        //            if (m2.Success) // only email without name
        //            {
        //                mailsOnly.Append(m2.Value);
        //                mailsOnly.Append(';');
        //            }
        //            else
        //            {
        //                // bad e-mail address
        //                PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_email_address, mail);
        //            }
        //        }
        //    }

        //    if (mailsOnly.Length == 0)
        //        return "";

        //    // return without last semicolon
        //    return mailsOnly.ToString(0, mailsOnly.Length - 1);
        //}

        ///// <summary>
        ///// Used for converting header Priority to <c>MailPriority</c> value needed by .NET Framework mailer.
        ///// </summary>
        ///// <param name="p">"Priority:" header value.</param>
        ///// <returns><c>MailPriority</c> specified by header value.</returns>
        //static MailPriority ExtractPriority(string p)
        //{
        //    switch (p.Trim().ToLowerInvariant())
        //    {
        //        case "high":
        //            return MailPriority.High;
        //        case "low":
        //            return MailPriority.Low;
        //        case "normal":
        //        default:
        //            return MailPriority.Normal;
        //    }
        //}

        ///// <summary>
        ///// Used for converting header ContentType to <c>MailFormat</c> value and <c>Encoding</c> class.
        ///// </summary>
        ///// <param name="contentTypeHeader">"Content-type:" header value</param>
        ///// <param name="mm">Mail message instance.</param>
        //static void ExtractContentType(string contentTypeHeader, MailMessage mm)
        //{
        //    contentTypeHeader = contentTypeHeader.Trim().ToLower();

        //    // extract content-type value parts (type/subtype; parameter1=value1; parameter2=value2)
        //    string[] headerParts = contentTypeHeader.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        //    if (headerParts == null || headerParts.Length == 0)
        //        return;

        //    // process type/subtype
        //    mm.IsBodyHtml = (headerParts[0].Trim() == "text/html");

        //    for (int i = 1; i < headerParts.Length; ++i)
        //    {
        //        int asspos = headerParts[i].IndexOf('=');
        //        if (asspos < 1) continue;

        //        string propertyName = headerParts[i].Remove(asspos).Trim();
        //        string propertyValue = headerParts[i].Substring(asspos + 1).Trim(new char[] { ' ', '\t', '\"', '\'', '\n', '\r' });

        //        switch (propertyName)
        //        {
        //            case "charset":
        //                try
        //                {
        //                    mm.BodyEncoding = Encoding.GetEncoding(propertyValue);
        //                }
        //                catch (Exception)
        //                { }
        //                break;
        //            default:
        //                break;
        //        }
        //    }

        //    // add header into the mail message as it is
        //    mm.Headers.Add("content-type", contentTypeHeader);
        //}

        #endregion

        #region RawSmtpClient

        /// <summary>
        /// Raw SMTP client serving the needs of PHP mail functions. This is reimplemented mainly because .NET SmtpClient provides
        /// certain level of abstraction which is incompatible with mail function usage. Currently not as much advanced, but it can easily be.
        /// </summary>
        internal class RawSmtpClient
        {
            /// <summary>
            /// Internal exception.
            /// </summary>
            sealed class RawSmtpException : Exception
            {
                public RawSmtpException(string message)
                    : base(message)
                { }
            }

            /// <summary>
            /// Wait time for Socket.Poll - in microseconds.
            /// </summary>
            private const int _pollTime = 100000;

            /// <summary>
            /// Timeout of connection. We don't want to block for too long.
            /// </summary>
            private const int _connectionTimeout = 5000;

            /// <summary>
            /// Gets a value indicating whether this client is connected to a server.
            /// </summary>
            public bool Connected { get { return _connected; } }
            private bool _connected;

            /// <summary>
            /// Gets or sets a value indicating whether this client should implicitly use ESMTP to connect to the server.
            /// </summary>
            public bool UseExtendedSmtp { get { return _useExtendedSmtp; } }
            private bool _useExtendedSmtp;

            /// <summary>
            /// Gets host name set for this client to connect to.
            /// </summary>
            public string/*!*/HostName { get { return _hostName; } }
            private readonly string/*!*/_hostName;

            /// <summary>
            /// Gets port number set for this client to connect to.
            /// </summary>
            public int Port { get { return _port; } }
            private readonly int _port;

            /// <summary>
            /// Gets a list of SMTP extensions supported by current connection.
            /// </summary>
            public string[] Extensions { get { return _extensions; } }
            private string[] _extensions;

            private TextReader _reader;
            private TextWriter _writer;

            private Socket _socket;
            private NetworkStream _stream;

            public RawSmtpClient(string hostName)
                : this(hostName, 25)
            {
            }

            /// <summary>
            /// Initializes a new instance of AdvancedSmtp client class.
            /// </summary>
            /// <param name="hostName">Host name (IP or domain name) of the SMTP server.</param>
            /// <param name="port">Port on which SMTP server runs.</param>
            public RawSmtpClient(string hostName, int port)
            {
                _hostName = hostName ?? string.Empty;
                _port = port;
                _connected = false;
                _useExtendedSmtp = true;
            }

            /// <summary>
            /// Resets the state of this object.
            /// </summary>
            private void ResetConnection()
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }

                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                }

                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }

                if (_socket != null)
                {
                    if (_socket.Connected)
                        _socket.Shutdown(SocketShutdown.Both);
                    _socket.Dispose();
                    _socket = null;
                }

                _extensions = null;
                _connected = false;
            }

            /// <summary>
            /// Connects to the server.
            /// </summary>
            /// <remarks>Method throws an exception on any error.</remarks>
            /// <exception cref="RawSmtpException">If any error occures.</exception>
            public void Connect()
            {
                // invariant condition
                Debug.Assert(_connected == (_socket != null));

                // check whether socket is not already connected
                if (_connected)
                {
                    // check whether the socket is OK
                    bool error = _socket.Poll(_pollTime, SelectMode.SelectError);

                    if (!error)
                        // ok, we keep this connection
                        return;// true;

                    // close the socket and reset
                    ResetConnection();
                }

                // resolve host's domain
                IPAddress[] addresses = null;

                try
                {
                    addresses = System.Net.Dns.GetHostAddressesAsync(_hostName).Result;
                }
                catch (Exception e)
                {
                    // DNS error - reset and fail
                    ResetConnection();
                    throw new RawSmtpException(e.Message);
                }

                Debug.Assert(addresses != null);

                // create socket
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // connect to the remote server
                _socket.ConnectAsync(addresses, _port).Wait(_connectionTimeout);

                // if socket could not connect, reset and fail
                if (!_socket.Connected)
                {
                    ResetConnection();
                    throw new RawSmtpException("Cannot connect to " + _hostName);
                }

                // if anything inside throws exception, we were not successful
                try
                {
                    // create a stream
                    _stream = new NetworkStream(_socket);

                    // create _reader and _writer
                    _reader = new StreamReader(_stream, Encoding.ASCII);
                    _writer = new StreamWriter(_stream, Encoding.ASCII);
                    _writer.NewLine = "\r\n";

                    string line;

                    // read server welcome message
                    line = _reader.ReadLine();

                    // if there is no 220 in the beginning, this is no SMTP server
                    if (!line.StartsWith("220")) throw new RawSmtpException("Expected 220, '" + line + "' given");// return false;
                    //TODO: server name processing

                    // send ESMTP welcome message
                    if (_useExtendedSmtp)
                    {
                        Post("EHLO " + System.Net.Dns.GetHostName());

                        // read response
                        line = _reader.ReadLine();
                    }

                    if (_useExtendedSmtp && line.StartsWith("250"))
                    {
                        // this is ESMTP server

                        // ESMTP returns '-' on fourth char if there are any more lines available
                        if (line[3] == ' ')
                        {
                            // there are no extensions
                            _extensions = ArrayUtils.EmptyStrings;

                            // success
                            return;// true;
                        }
                        else if (line[3] == '-')
                        {
                            List<string> extensions = new List<string>();

                            // we do not need to read first line - there is only a welcome string

                            while (true)
                            {
                                //read new line
                                line = _reader.ReadLine();

                                if (line.StartsWith("250-"))
                                {
                                    //add new extension name
                                    extensions.Add(line.Substring(4, line.Length - 4));
                                }
                                else if (line.StartsWith("250 "))
                                {
                                    //add new extension name and finish handshake
                                    extensions.Add(line.Substring(4, line.Length - 4));
                                    _extensions = extensions.ToArray();
                                    _connected = true;
                                    return;// true;
                                }
                                else
                                {
                                    //invalid response (do not send QUIT message)
                                    break;
                                }
                            }
                        }

                        // this is not a valid ESMTP server
                    }
                    else if (line.StartsWith("500") || !_useExtendedSmtp)
                    {
                        Post("HELO " + System.Net.Dns.GetHostName());

                        if (Ack("250"))
                        {
                            _extensions = ArrayUtils.EmptyStrings;

                            // handshake complete
                            _connected = true;
                            return;// true;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new RawSmtpException(e.Message);
                } // any error is bad

                ResetConnection(); // (do not send QUIT message)

                throw new RawSmtpException("Unexpected"); //return false;
            }

            /// <summary>
            /// Disconnects the client from the server.
            /// </summary>
            public void Disconnect()
            {
                if (!_connected)
                {
                    ResetConnection();
                    return;
                }

                Post("QUIT");
                Ack("221", null, (_) => {/*incorrect response (do nothing)*/});

                //correct response
                ResetConnection();
            }

            /// <summary>
            /// Sends reset message to the server.
            /// </summary>
            private void Reset()
            {
                if (!_connected) return;

                if (_reader.Peek() != -1)
                {
                    // there is something on the input (should be empty)
                    ResetConnection();
                    return;
                }

                Post("RSET");
                Ack("250", null,
                    (_) => ResetConnection());
            }

            /// <summary>
            /// Starts mail transaction and prepares the data lines from supplied message properties.
            /// Processes provided headers to determine cc, bcc and from values.
            /// All data will be send as ASCII if possible.
            /// </summary>
            /// <param name="from">Sender of the mail.</param>
            /// <param name="to">Recipients of the mail.</param>
            /// <param name="subject">Subject of the mail.</param>
            /// <param name="headers">Additional headers.</param>
            /// <param name="body">Message body.</param>
            /// <returns>List of message body lines.</returns>
            private IEnumerable<string>/*!*/ProcessMessageHeaders(string from, string to, string subject, string headers, string body)
            {
                Dictionary<string, int> headerHashtable = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                List<KeyValuePair<string, string>> headerList = new List<KeyValuePair<string, string>>();
                List<string> recipients = new List<string>(1) { to };

                //parse headers
                if (headers != null)
                    using (StringReader reader = new StringReader(headers))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            int index = line.IndexOf(": ", StringComparison.Ordinal);

                            if (index > 0)
                            {
                                string name = line.Substring(0, index);
                                string value = line.Substring(index + 2);

                                //
                                headerHashtable[name] = headerList.Count;   // remember last position of <name> header
                                headerList.Add(new KeyValuePair<string, string>(name, value));

                                // process known headers:
                                if (from == null && name.Equals("from", StringComparison.OrdinalIgnoreCase))
                                    from = value;
                                if (name.Equals("cc", StringComparison.OrdinalIgnoreCase) || name.Equals("bcc", StringComparison.OrdinalIgnoreCase))
                                    recipients.Add(value); //PostRcptTo(value); // postponed until we are discovering from address
                            }
                        }
                    }

                // check from address:
                if (from == null)
                    throw new RawSmtpException(Resources.LibResources.smtp_sendmail_from_not_set);

                // start mail transaction:
                Post(FormatEmailAddress(from, "MAIL FROM:<{0}>"));
                Ack("250");

                for (int i = 0; i < recipients.Count; i++)
                    PostRcptTo(recipients[i]);

                // additional message lines:
                List<string> ret = new List<string>();

                // Date:
                ret.Add("Date: " + System.DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zz00", new System.Globalization.CultureInfo("en-US")));

                // From: // Only add the From: field from <from> parameter if it isn't in the custom headers:
                if (!headerHashtable.ContainsKey("from") && !string.IsNullOrEmpty(from))
                    ret.Add("From: " + from);

                // Subject:
                ret.Add("Subject: " + (subject ?? "No Subject"));

                // To: // Only add the To: field from the <to> parameter if isn't in the custom headers:
                if (!headerHashtable.ContainsKey("to") && !string.IsNullOrEmpty(to))
                    ret.Add("To: " + to);

                // add headers, ignore duplicities (only accept the last occurance):
                foreach (var headerIndex in headerHashtable.Values)
                {
                    var header = headerList[headerIndex];
                    ret.Add(string.Format("{0}: {1}", header.Key, header.Value));
                }

                ret.Add("");

                // parse the <body> into lines:
                var bodyReader = new StringReader(body);

                while (bodyReader.Peek() != -1)
                    ret.Add(bodyReader.ReadLine());

                return ret;
            }

            /// <summary>
            /// Cut out the address if contained within &lt;...&gt; characters. Otherwise take the whole <paramref name="address"/> string.
            /// The address is transformed using given <paramref name="formatString"/> format.
            /// </summary>
            /// <param name="address">Given mail address.</param>
            /// <param name="formatString">Format to be used.</param>
            /// <returns>Formatted email address.</returns>
            private static string FormatEmailAddress(string/*!*/address, string/*!*/formatString)
            {
                Debug.Assert(address != null, "address == null");
                Debug.Assert(formatString != null, "formatString == null");

                int a, b;
                if ((a = address.IndexOf('<')) >= 0 && (b = address.IndexOf('>', a)) >= 0)
                    address = address.Substring(a + 1, b - a - 1);

                return string.Format(formatString, address.Trim());
            }

            #region Post, Ack

            /// <summary>
            /// Writes <paramref name="line"/>, appends <c>CRLF</c> and flushes internal writer.
            /// </summary>
            /// <param name="line"><see cref="String"/> to be written onto the internal writer.</param>
            private void Post(string line)
            {
                this._writer.WriteLine(line);
                this._writer.Flush();
            }

            private bool Ack(string expected1)
            {
                return Ack(expected1, null,
                    (line) => ThrowExpectedResponseHelper(line, expected1));
            }

            private bool Ack(string expected1, string expected2)
            {
                return Ack(expected1, expected2,
                    (line) => ThrowExpectedResponseHelper(line, string.Format("{0} or {1}", expected1, expected2)));
            }

            private void ThrowExpectedResponseHelper(string givenResponse, string expectedStr)
            {
                Reset();
                throw new RawSmtpException(string.Format("Expected response {0}, '{1}' given.", expectedStr, givenResponse));
            }

            private bool Ack(string expected1, string expected2, Action<string>/*!*/fail)
            {
                Debug.Assert(fail != null);

                var line = _reader.ReadLine();

                if (expected1 != null && line.StartsWith(expected1, StringComparison.Ordinal))
                    return true; // ok

                if (expected2 != null && line.StartsWith(expected2, StringComparison.Ordinal))
                    return true; // ok

                fail(line);
                return false;
            }

            #endregion

            /// <summary>
            /// Send <c>RCPT TO</c> commands.
            /// </summary>
            /// <param name="recipients">List of recipients comma-separated.</param>
            private void PostRcptTo(string recipients)
            {
                if (!string.IsNullOrEmpty(recipients))
                    foreach (var rcpt in recipients.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (rcpt.StartsWith("undisclosed-recipients:", StringComparison.Ordinal))
                            continue;   // this should be specified in To: header, it is not intended for the SMTP server within RCPT TO command

                        Post(FormatEmailAddress(rcpt, "RCPT TO:<{0}>"));
                        Ack("250", "251");
                    }
            }

            /// <summary>
            /// Sends the raw message.
            /// </summary>
            /// <remarks>On eny error an exception is thrown.</remarks>
            /// <exception cref="RawSmtpException">When any error occures during the mail send.</exception>
            public void SendMessage(string from, string to, string subject, string headers, string body)
            {
                //
                // see http://email.about.com/cs/standards/a/smtp_error_code_2.htm for response codes.
                //

                if (!_connected)
                    throw new RawSmtpException("NOT CONNECTED");

                // start mail transaction and
                // process headers (may contain additional recipients and from address)
                // and prepare data that is broken up to form data lines.
                // Note ProcessMessageData may add additional recipients, so it must be called before "DATA" section.
                var dataLines = ProcessMessageHeaders(from, to, subject, headers, body);

                // send DATA
                Post("DATA");
                Ack("354");

                foreach (string dataLine in dataLines)
                {
                    // PHP implementation uses 991 line length limit (including CRLF)
                    const int maxLineLength = 989;
                    int lineStart = 0;
                    int correction = 0;

                    // if SP character is on the first place, we need to duplicate it
                    if (dataLine.Length > 0 && dataLine[0] == '.')
                        _writer.Write('.');

                    // according to MIME, the lines must not be longer than 998 characters (1000 including CRLF)
                    // so we need to break such lines using folding
                    while (dataLine.Length - lineStart > maxLineLength - correction)
                    {
                        //break the line, inserting FWS sequence
                        _writer.WriteLine(dataLine.Substring(lineStart, maxLineLength - correction));
                        _writer.Write(' ');
                        lineStart += maxLineLength - correction;

                        //make correction (whitespace on the next line)
                        correction += 1;
                    }

                    //output the rest of the line
                    _writer.WriteLine(dataLine.Substring(lineStart));

                    // flush the stream
                    _writer.Flush();
                }

                _writer.WriteLine(".");

                // flush the stream
                _writer.Flush();

                Ack("250");

                //return true; // ok
            }
        }

        #endregion
    }

    //[PhpExtension("IMAP")] // uncomment when the extension is ready
    public static class Imap
    {
        /// <summary>
        /// Parses an address string.
        /// </summary>
        [return: NotNull]
        public static PhpArray imap_rfc822_parse_adrlist(string addresses, string default_host = null)
        {
            if (string.IsNullOrEmpty(addresses))
            {
                return PhpArray.NewEmpty();
            }

            var collection = new MailAddressCollection();
            collection.Add(addresses);

            var arr = new PhpArray(collection.Count);
            foreach (var addr in collection)
            {
                var item = new PhpArray(3)
                {
                    { "mailbox", addr.User },
                    { "host", addr.Host ?? default_host },
                };

                if (addr.DisplayName != null) item["personal"] = addr.DisplayName;

                arr.Add(item.AsStdClass());
            }

            //
            return arr;
        }
    }
}
