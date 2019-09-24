using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Pchp.Core;
using FluentFTP;
using System.IO;
using System.Net.Sockets;
using Pchp.Library.Streams;

namespace Pchp.Library
{
    [PhpExtension("ftp")]
    /// <summary>
    /// Class implements client access to files servers speaking the File Transfer Protocol (FTP)
    /// </summary>
    public static class Ftp
    {
        #region Constants

        public const int FTP_ASCII = 1;
        public const int FTP_TEXT = FTP_ASCII; // Alias of FTP_ASCII
        public const int FTP_BINARY = 2;
        public const int FTP_IMAGE = FTP_BINARY; // Alias of FTP_BINARY
        public const int FTP_AUTOSEEK = 1;
        public const int FTP_TIMEOUT_SEC = 0;
        public const int FTP_USEPASVADDRESS = 2;
        public const int FTP_AUTORESUME = -1;

        #endregion

        #region FtpResource

        /// <summary>
        /// Context of ftp session
        /// </summary>
        internal class FtpResource : PhpResource
        {
            private int _timeout = 90000; // ms
            private bool _usepasvaddress = true;
            private bool _autoseek = true;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="client"></param>
            /// <param name="timeout">Time is in seconds</param>
            public FtpResource(FtpClient client, int timeout) : base("FTP Buffer")
            {
                Client = client;
                Timeout = timeout * 1000;
                Client.ReadTimeout = Timeout;
                Client.DataConnectionConnectTimeout = Timeout;
                Client.DataConnectionReadTimeout = Timeout;
            }

            public FtpClient Client { get; }
            public int Timeout
            {
                get => _timeout;
                set {
                    Client.ReadTimeout = value;
                    Client.DataConnectionConnectTimeout = value;
                    Client.DataConnectionReadTimeout = value;
                    _timeout = value;
                }
            }

            public bool UsePASVAddress { get => _usepasvaddress; set => _usepasvaddress = value; }
            public bool Autoseek { get => _autoseek; set => _autoseek = value; }

            protected override void FreeManaged()
            {
                Client.Dispose();
                base.FreeManaged();
            }
        }

        /// <summary>
        /// Gets instance of <see cref="FtpResource"/> or <c>null</c>.
        /// If given argument is not an instance of <see cref="FtpResource"/>, PHP warning is reported.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        static FtpResource ValidateFtpResource(PhpResource context)
        {
            var h = context as FtpResource;
            if (h == null || !h.IsValid)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_context_resource);
                //PhpException.InvalidArgumentType(nameof(context), PhpResource.PhpTypeName);
            }

            return h;
        }

        #endregion

        #region ftp_connect, ftp_login, ftp_put, ftp_close, ftp_ssl_connect

        private static PhpResource Connect(FtpClient client, int port = 21, int timeout = 90)
        {
            client.ConnectTimeout = timeout * 1000;
            client.Port = port;
            client.Credentials = null; // Disable anonymous login

            try // Try to connect, if excetion, return false
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                // ftp_connect does not throw any warnings
                // These exception are thrown, when is problem with connection SocketException - Active refuse of connection, FtpCommandException - Server is locked, AggregateException - Unrecognised hostname
                if (ex is SocketException || ex is FtpCommandException || ex is AggregateException || ex is TimeoutException)
                {
                    return null;
                }

                throw;
            }

            return new FtpResource(client, client.ConnectTimeout / 1000);
        }

        /// <summary>
        /// Opens an FTP connection to the specified host.
        /// </summary>
        /// <param name="host">The FTP server address. This parameter shouldn't have any trailing slashes and shouldn't be prefixed with ftp://.</param>
        /// <param name="port">This parameter specifies an alternate port to connect to. If it is omitted or set to zero, then the default FTP port, 21, will be used.</param>
        /// <param name="timeout">This parameter specifies the timeout in seconds for all subsequent network operations. If omitted, the default value is 90 seconds. The timeout can be changed and queried at any time with ftp_set_option() and ftp_get_option().</param>
        /// <returns>Returns a FTP stream on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static PhpResource ftp_connect(string host, int port = 21, int timeout = 90)
        {
            FtpClient client = new FtpClient(host);

            return Connect(client, port, timeout);
        }

        /// <summary>
        /// Opens an explicit SSL-FTP connection to the specified host. That implies that ftp_ssl_connect() will succeed even if the server is not configured for SSL-FTP, or its certificate is invalid. Only when ftp_login() is called, the client will send the appropriate AUTH FTP command, so ftp_login() will fail in the mentioned cases.
        /// </summary>
        /// <param name="host">The FTP server address. This parameter shouldn't have any trailing slashes and shouldn't be prefixed with ftp://.</param>
        /// <param name="port">This parameter specifies an alternate port to connect to. If it is omitted or set to zero, then the default FTP port, 21, will be used.</param>
        /// <param name="timeout">This parameter specifies the timeout for all subsequent network operations. If omitted, the default value is 90 seconds. The timeout can be changed and queried at any time with ftp_set_option() and ftp_get_option().</param>
        /// <returns>Returns a SSL-FTP stream on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static PhpResource ftp_ssl_connect(string host, int port = 21, int timeout = 90)
        {
            FtpClient client = new FtpClient(host);
            //Ssl configuration
            client.EncryptionMode = FtpEncryptionMode.Implicit;
            client.SslProtocols = System.Security.Authentication.SslProtocols.Ssl3;

            return Connect(client, port, timeout);
        }

        /// <summary>
        /// Logs in to the given FTP stream.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="username">The username (USER).</param>
        /// <param name="password">The password (PASS).</param>
        /// <returns>Returns TRUE on success or FALSE on failure. If login fails, PHP will also throw a warning.</returns>
        public static bool ftp_login(PhpResource ftp_stream, string username, string password)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            resource.Client.Credentials = new NetworkCredential(username, password);

            try
            {
                FtpReply reply;

                if (!(reply = resource.Client.Execute($"USER {username}")).Success)
                    throw new FtpAuthenticationException(reply);

                if (reply.Type == FtpResponseType.PositiveIntermediate && !(reply = resource.Client.Execute($"PASS {password}")).Success)
                    throw new FtpAuthenticationException(reply);
            }
            catch (FtpAuthenticationException) // Warning, Wrong password or login
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.incorrect_pass_or_login);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Stores a local file on the FTP server.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="local_file">The local file path.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="startpos">The position in the remote file to start uploading to.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_put(PhpResource ftp_stream, string remote_file, string local_file, int mode = FTP_IMAGE, int startpos = 0)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;
            if (!File.Exists(local_file))
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, local_file);
                return false;
            }

            // Two types of data transfer
            resource.Client.UploadDataType = (mode == 1) ? FtpDataType.ASCII : FtpDataType.Binary;

            if (startpos > 0) 
            {
                // Not supported because ftp library does not support this option 
                PhpException.Throw(PhpError.Warning, Resources.Resources.option_not_supported);
            }

            try
            {
                return resource.Client.UploadFile(local_file, remote_file, FtpExists.Overwrite);
            }
            // FtpException everytime wraps other exceptions (Message from server). https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP/Client/FtpClient_HighLevelUpload.cs#L595
            catch (FtpException ex) { PhpException.Throw(PhpError.Warning, ex.InnerException.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return false;
        }

        /// <summary>
        /// Closes the given link identifier and releases the resource.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_close(PhpResource ftp_stream)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            ftp_stream.Dispose();

            return true;
        }

        #endregion

        #region ftp_delete, ftp_rmdir, ftp_mkdir, ftp_chdir

        private delegate void CommandFunction(string path);

        /// <summary>
        /// Execute specific command
        /// </summary>
        private static bool FtpCommand(string path, CommandFunction func)
        {
            try
            {
                func(path);
                return true;
            }
            catch (FtpCommandException ex) { PhpException.Throw(PhpError.Warning, ex.Message); }
            catch (ArgumentException) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, path); }

            return false;
        }

        /// <summary>
        /// Deletes the file specified by path from the FTP server.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="path">The file to delete.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_delete(PhpResource ftp_stream, string path)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            return FtpCommand(path, resource.Client.DeleteFile);
        }

        /// <summary>
        /// Removes the specified directory on the FTP server.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="path">The directory to delete. This must be either an absolute or relative path to an empty directory.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_rmdir(PhpResource ftp_stream, string path)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            return FtpCommand(path, resource.Client.DeleteDirectory);
        }

        /// <summary>
        /// Creates the specified directory on the FTP server.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="directory">The name of the directory that will be created.</param>
        /// <returns>Returns the newly created directory name on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static string ftp_mkdir(PhpResource ftp_stream, string directory)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            string workingDirectory = resource.Client.GetWorkingDirectory();

            if (FtpCommand(directory, resource.Client.CreateDirectory))
                return workingDirectory == "/" ? $"/{directory}" : $"{workingDirectory}/{directory}";
            else
                return null;
        }

        /// <summary>
        /// Changes the current directory to the specified one.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="directory">The target directory.</param>
        /// <returns>Returns TRUE on success or FALSE on failure. If changing directory fails, PHP will also throw a warning.</returns>
        public static bool ftp_chdir(PhpResource ftp_stream, string directory)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            return FtpCommand(directory, resource.Client.SetWorkingDirectory);
        }

        #endregion

        #region ftp_pwd, ftp_get_option, ftp_set_option, ftp_size, ftp_pasv, ftp_chmod, ftp_rename, ftp_systype, ftp_nlist, ftp_mdtm, ftp_rawlist

        /// <summary>
        /// Renames a file or a directory on the FTP server
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="oldname">The old file/directory name.</param>
        /// <param name="newname">The new name.</param>
        /// <returns>Returns TRUE on success or FALSE on failure. Upon failure (such as attempting to rename a non-existent file), an E_WARNING error will be emitted.</returns>
        public static bool ftp_rename(PhpResource ftp_stream, string oldname, string newname)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            try
            {
                resource.Client.Rename(oldname, newname);
                return true;
            }
            catch (FtpCommandException ex) { PhpException.Throw(PhpError.Warning, ex.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return false;
        }

        /// <summary>
        /// Returns the current directory name
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <returns>Returns the current directory name or FALSE on error.</returns>
        [return: CastToFalse]
        public static string ftp_pwd(PhpResource ftp_stream)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            return resource.Client.GetWorkingDirectory();
        }

        /// <summary>
        /// This function returns the value for the requested option from the specified FTP connection.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="option">Currently, the following options are supported: FTP_TIMEOUT_SEC, FTP_AUTOSEEK, FTP_USEPASVADDRESS</param>
        /// <returns>Returns the value on success or FALSE if the given option is not supported. In the latter case, a warning message is also thrown.</returns>
        public static PhpValue ftp_get_option(PhpResource ftp_stream, int option)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return PhpValue.False;

            switch (option)
            {
                case FTP_AUTOSEEK:
                    return resource.Autoseek;
                case FTP_TIMEOUT_SEC:
                    return PhpValue.Create(resource.Client.ConnectTimeout);
                case FTP_USEPASVADDRESS: // Ignore the IP address returned by the FTP server in response to the PASV command and instead use the IP address that was supplied in the ftp_connect()
                    return resource.Client.DataConnectionType != FtpDataConnectionType.PASVEX;
                default:
                    PhpException.Throw(PhpError.Warning, Resources.Resources.unknown_option, option.ToString());
                    return PhpValue.False;
            }
        }

        /// <summary>
        /// This function controls various runtime options for the specified FTP stream.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="option">Currently, the following options are supported:FTP_TIMEOUT_SEC, FTP_AUTOSEEK, FTP_USEPASVADDRESS</param>
        /// <param name="value">Returns TRUE if the option could be set; FALSE if not. A warning message will be thrown if the option is not supported or the passed value doesn't match the expected value for the given option.</param>
        /// <returns></returns>
        public static bool ftp_set_option(PhpResource ftp_stream, int option, PhpValue value)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            switch (option)
            {
                case FTP_AUTOSEEK:
                    if (!value.IsBoolean)
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.unexpected_arg_given, "value", "Boolean", value.TypeCode.ToString());
                        return false;
                    }
                    resource.Autoseek = value.ToBoolean();
                    return true;
                case FTP_TIMEOUT_SEC:
                    if (!value.IsInteger())
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.unexpected_arg_given, "value", "int", value.TypeCode.ToString());
                        return false;
                    }
                    int timeout = value.ToInt();
                    if (timeout <= 0)
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.sleep_seconds_less_zero);
                        return false;
                    }
                    resource.Timeout = timeout;
                    return true;
                case FTP_USEPASVADDRESS:
                    if (!value.IsBoolean)
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.unexpected_arg_given, "value", "bool", value.TypeCode.ToString());
                        return false;
                    }
                    resource.UsePASVAddress = value.ToBoolean();
                    return true;
                default:
                    PhpException.Throw(PhpError.Warning, Resources.Resources.unknown_option, option.ToString());
                    return PhpValue.False;
            }
        }

        /// <summary>
        /// Returns the size of the given file in bytes.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The remote file.</param>
        /// <returns>Returns the file size on success, or -1 on error.</returns>
        public static long ftp_size(PhpResource ftp_stream, string remote_file)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return -1;

            try
            {
                return resource.Client.GetFileSize(remote_file);
            }
            catch (FtpCommandException ex) { PhpException.Throw(PhpError.Warning, ex.Message); }
            catch (ArgumentException) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, remote_file); }

            return -1;
        }

        /// <summary>
        /// Turns on or off passive mode. In passive mode, data connections are initiated by the client, rather than by the server. It may be needed if the client is behind firewall. Can only be called after a successful login or otherwise it will fail.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="pasv">If TRUE, the passive mode is turned on, else it's turned off.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_pasv(PhpResource ftp_stream, bool pasv)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            if (resource.UsePASVAddress)
                resource.Client.DataConnectionType = pasv ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.AutoActive;
            else
                resource.Client.DataConnectionType = pasv ? FtpDataConnectionType.PASVEX : FtpDataConnectionType.AutoActive;

            return true;
        }

        private static int ConvertDecToOctal(int decimalNumber)
        {
            int octalNumber = 0;
            int position = 1;
            while (decimalNumber != 0)
            {
                octalNumber += decimalNumber % 8 * position;
                position *= 10;
                decimalNumber /= 8;
            }

            return octalNumber;
        }

        /// <summary>
        /// Sets the permissions on the specified remote file to mode.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="mode">The new permissions, given as an octal value.</param>
        /// <param name="filename">The remote file.</param>
        /// <returns>Returns the new file permissions on success or FALSE on error.</returns>
        public static PhpValue ftp_chmod(PhpResource ftp_stream, int mode, string filename)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return PhpValue.False;

            try
            {
                resource.Client.Chmod(filename, ConvertDecToOctal(mode));
                return mode;
            }
            catch (FtpCommandException ex) { PhpException.Throw(PhpError.Warning, ex.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return PhpValue.False;
        }

        /// <summary>
        /// Returns the system type identifier of the remote FTP server.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <returns>Returns the remote system type, or FALSE on error.</returns>
        [return: CastToFalse]
        public static string ftp_systype(PhpResource ftp_stream)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            // Inspired by https://github.com/php/php-src/blob/a1479fbbd9d4ad53fb6b0ed027548ea078558f5b/ext/ftp/ftp.c#L411
            return resource.Client.SystemType.Split(' ')[0];
        }

        /// <summary>
        /// Returns a list of files in the given directory
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="directory">The directory to be listed.</param>
        /// <returns>Returns an array of filenames from the specified directory on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static PhpArray ftp_nlist(PhpResource ftp_stream, string directory)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            try
            {
                return new PhpArray(resource.Client.GetNameListing());
            }
            catch (FtpCommandException ex) { PhpException.Throw(PhpError.Warning, ex.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return null;
        }

        /// <summary>
        /// Returns the last modified time of the given file (does not work with directories.)
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The file from which to extract the last modification time.</param>
        /// <returns>Returns the last modified time as a Unix timestamp on success, or -1 on error.</returns>
        public static long ftp_mdtm(PhpResource ftp_stream, string remote_file)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return -1;

            try
            {
                // Unix timestamp
                System.DateTime mdDateTime = resource.Client.GetModifiedTime(remote_file,FtpDate.Original);
                System.DateTime dtDateTime = new System.DateTime(1970,1, 1, 0, 0, 0, 0, System.DateTimeKind.Unspecified);
                TimeSpan diff = mdDateTime - dtDateTime;
                return (long)diff.TotalSeconds;
            }
            catch (FtpCommandException ex) { PhpException.Throw(PhpError.Warning, ex.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return -1;
        }

        /// <summary>
        /// executes the FTP LIST command, and returns the result as an array.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="directory">The directory path. May include arguments for the LIST command.</param>
        /// <param name="recursive">If set to TRUE, the issued command will be LIST -R.</param>
        /// <returns>Returns an array where each element corresponds to one line of text. Returns FALSE when passed directory is invalid.</returns>
        [return: CastToFalse]
        public static PhpArray ftp_rawlist(PhpResource ftp_stream, string directory, bool recursive = false)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            try
            {
                FtpListItem[] list;
                if (resource.Client.RecursiveList) // If recusrive set to TRUE, the issued command will be LIST -R otherwise is it like LIST. Server must support it !
                    list = recursive ? resource.Client.GetListing(directory, FtpListOption.Recursive | FtpListOption.ForceList) : resource.Client.GetListing(directory);
                else
                    list = resource.Client.GetListing(directory, FtpListOption.ForceList);

                return new PhpArray(Getlist(list));
            }
            catch (FtpCommandException ex) { PhpException.Throw(PhpError.Warning, ex.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return null;
        }

        private static string[] Getlist(FtpListItem[] list)
        {
            string[] result = new string[list.Length];

            for (int i = 0; i < list.Length; i++)
                result[i] = $"{list[i].Input}";

            return result;
        }

        #endregion

        #region ftp_fget, ftp_fput, ftp_site

        /// <summary>
        /// Retrieves remote_file from the FTP server, and writes it to the given file pointer.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="handle">An open file pointer in which we store the data.</param>
        /// <param name="remotefile">The remote file path.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="resumepos">The position in the remote file to start downloading from.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_fget(PhpResource ftp_stream, PhpResource handle, string remotefile, int mode = FTP_IMAGE, int resumepos = 0)
        {
            // Check file resource
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return false;
            }
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            resource.Client.DownloadDataType = (mode == 1) ? FtpDataType.ASCII : FtpDataType.Binary;

            // Ignore autoresume if autoseek is switched off 
            if (resource.Autoseek && resumepos == FTP_AUTORESUME)
                resumepos = 0;

            if(resource.Autoseek && resumepos!=0)
            {
                if (resumepos == FTP_AUTORESUME)
                {
                    stream.Seek(0, SeekOrigin.End);
                    resumepos = stream.Tell();
                }
                else
                    stream.Seek(resumepos, SeekOrigin.Begin);
            }

            try
            {
                return resource.Client.Download(stream.RawStream, remotefile, resumepos);
            }
            catch (FtpException ex) { PhpException.Throw(PhpError.Warning, ex.InnerException.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return false;
        }

        /// <summary>
        /// Uploads the data from a file pointer to a remote file on the FTP server.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="handle">An open file pointer on the local file. Reading stops at end of file.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="startpos">The position in the remote file to start uploading to.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_fput(PhpResource ftp_stream, string remote_file, PhpResource handle, int mode = FTP_IMAGE, int startpos = 0)
        {
            // Check file resource
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return false;
            }
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            resource.Client.UploadDataType = (mode == 1) ? FtpDataType.ASCII : FtpDataType.Binary;

            try
            {
                if (startpos !=0)
                {
                    // Not supported because ftp library does not support this option 
                    PhpException.Throw(PhpError.Warning, Resources.Resources.option_not_supported);
                }

                return resource.Client.Upload(stream.RawStream,remote_file,FtpExists.Overwrite);
            }
            catch (FtpException ex) { PhpException.Throw(PhpError.Warning, ex.InnerException.Message); }
            catch (ArgumentException ex) { PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName); }

            return false;
        }

        /// <summary>
        /// Sends a SITE command to the server
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="command">The SITE command. Note that this parameter isn't escaped so there may be some issues with filenames containing spaces and other characters.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_site(PhpResource ftp_stream, string command)
        {
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            try
            {
                FtpReply reply = resource.Client.Execute($"SITE {command}");

                int code = System.Convert.ToInt32(reply.Code);

                return !(code < 200 || code >= 300);
            }
            catch (FtpException ex) { PhpException.Throw(PhpError.Warning, ex.InnerException.Message); }

            return false;
        }

        #endregion

    }
}
