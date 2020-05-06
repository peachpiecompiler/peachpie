using System;
using System.Net;
using Pchp.Core;
using FluentFTP;
using System.IO;
using System.Net.Sockets;
using Pchp.Library.Streams;
using System.Security.Authentication;
using Pchp.Core.Utilities;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;

namespace Pchp.Library
{
    /// <summary>
    /// Class implements client access to files servers speaking the File Transfer Protocol (FTP)
    /// </summary>
    [PhpExtension("ftp")]
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
        public const int FTP_FAILED = 0;
        public const int FTP_MOREDATA = 2;
        public const int FTP_FINISHED = 1;
        #endregion

        #region FtpResource

        /// <summary>
        /// Context of ftp session
        /// </summary>
        internal class FtpResource : PhpResource
        {
            /// <summary>
            /// Initializes the resource object.
            /// </summary>
            /// <param name="client"></param>
            /// <param name="timeout">Time is in ms</param>
            public FtpResource(FtpClient client, int timeout)
                : base("FTP Buffer")
            {
                if (client == null)
                {
                    throw new ArgumentNullException(nameof(client));
                }

                client.ReadTimeout = timeout;
                client.ConnectTimeout = client.ReadTimeout;
                client.DataConnectionConnectTimeout = client.ReadTimeout;
                client.DataConnectionReadTimeout = client.ReadTimeout;

                Client = client;
                Autoseek = true;
                TokenSource = new CancellationTokenSource();

            }

            public FtpClient Client { get; }

            public bool UsePASVAddress { get; set; } = true;

            public bool Autoseek { get; set; }

            /// <summary>
            /// Pending operation.
            /// When assigning, check the task is supported in <see cref="TasksGetInfo"/>.
            /// </summary>
            public Task PendingOperationTask { get; set; }

            public CancellationTokenSource TokenSource { get; private set; }

            public void PrepareForPendingOperation(int mode)
            {
                Client.DownloadDataType = (mode == FTP_ASCII) ? FtpDataType.ASCII : FtpDataType.Binary;

                var task = PendingOperationTask;

                // Cancel current function and start new
                if (task != null && !task.IsCompleted) // not faulted, not cancelled, not ran to finish
                {
                    // CancellationTokenSource cannot be reset and cancelled again
                    TokenSource.Cancel();

                    try
                    {
                        // Wait for task is canceled in other to dispose TokenSource
                        task.Wait();
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                    {
                    }

                    PendingOperationTask = null;
                }

                if (TokenSource != null)
                {
                    TokenSource.Dispose();
                    TokenSource = new CancellationTokenSource();
                }
            }

            protected override void FreeManaged()
            {
                TokenSource?.Dispose();
                Client.Dispose();
                base.FreeManaged();
            }
        }

        /// <summary>
        /// Gets instance of <see cref="FtpResource"/> or <c>null</c>.
        /// If given argument is not an instance of <see cref="FtpResource"/>, PHP warning is reported.
        /// </summary>
        static FtpResource ValidateFtpResource(PhpResource context)
        {
            if (context is FtpResource h && h.IsValid)
            {
                return h;
            }

            //
            PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_context_resource);
            return null;
        }

        #endregion

        private static PhpResource Connect(FtpClient client, int port, int timeout)
        {
            Debug.Assert(client != null);

            client.Port = port;
            client.Credentials = null; // Disable anonymous login
            client.DataConnectionType = FtpDataConnectionType.AutoActive;   // PHP uses active connection by default

            var resource = new FtpResource(client, timeout * 1000);

            try // Try to connect, if exception, return false
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                // ftp_connect does not throw any warnings
                /* These exception are thrown, when is problem with connection SocketException - Active refuse of connection,
                 * FtpCommandException - Server is locked, AggregateException - Unrecognised hostname */
                if (ex is SocketException || ex is FtpCommandException || ex is AggregateException || ex is TimeoutException)
                {
                    // TODO: report warning
                    resource = null;
                }
                else
                {
                    throw;
                }
            }

            //
            return resource;
        }

        /// <summary>
        /// Opens an FTP connection to the specified host.
        /// </summary>
        /// <param name="host">The FTP server address. This parameter shouldn't have any trailing slashes 
        /// and shouldn't be prefixed with ftp://.</param>
        /// <param name="port">This parameter specifies an alternate port to connect to. 
        /// If it is omitted or set to zero, then the default FTP port, 21, will be used.</param>
        /// <param name="timeout">This parameter specifies the timeout in seconds for all subsequent network operations.
        /// If omitted, the default value is 90 seconds. </param>
        /// <returns>Returns a FTP stream on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static PhpResource ftp_connect(string host, int port = 21, int timeout = 90)
        {
            var client = new FtpClient(host);

            return Connect(client, port, timeout);
        }

        /// <summary>
        /// Requests execution of a command on the FTP server.
        /// </summary>
        public static bool ftp_exec(PhpResource ftp_stream, string command)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            if (string.IsNullOrEmpty(command))
            {
                PhpException.InvalidArgument(nameof(command));
                return false;
            }

            try
            {
                var reply = resource.Client.Execute(command);
                if (reply.Success)
                {
                    return true;
                }

                PhpException.Throw(PhpError.Notice, reply.ErrorMessage);
            }
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }

            //
            return false;
        }

        /// <summary>
        /// Opens an explicit SSL-FTP connection to the specified host.
        /// </summary>
        /// <param name="host">The FTP server address. This parameter shouldn't have any trailing slashes and 
        /// shouldn't be prefixed with ftp://.</param>
        /// <param name="port">This parameter specifies an alternate port to connect to. If it is omitted or set to zero,
        /// then the default FTP port, 21, will be used.</param>
        /// <param name="timeout">This parameter specifies the timeout for all subsequent network operations. 
        /// If omitted, the default value is 90 seconds. </param>
        /// <returns>Returns a SSL-FTP stream on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static PhpResource ftp_ssl_connect(string host, int port = 21, int timeout = 90)
        {
            var client = new FtpClient(host)
            {
                // Ssl configuration
                EncryptionMode = FtpEncryptionMode.Explicit,
                SslProtocols = SslProtocols.None
            };

            /* ftp_ssl_connect() opens an explicit SSL-FTP connection to the specified host. 
             * That implies that ftp_ssl_connect() will succeed even if the server is not configured for SSL-FTP, 
             * or its certificate is invalid. */
            client.ValidateCertificate += new FtpSslValidation((FtpClient control, FtpSslValidationEventArgs e) =>
                {
                    e.Accept = true;
                });

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

            var reply = resource.Client.Execute($"USER {username}");
            if (reply.Success && reply.Type == FtpResponseType.PositiveIntermediate)
            {
                reply = resource.Client.Execute($"PASS {password}");
            }

            //

            if (reply.Success)
            {
                return true;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.incorrect_pass_or_login);
                return false;
            }
        }

        /// <summary>
        /// Stores a local file on the FTP server.
        /// </summary>
        /// <param name="context">Runtime context.</param>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="local_file">The local file path.</param>
        /// <param name="mode">The transfer mode. Must be either <see cref="FTP_ASCII"/> or <see cref="FTP_BINARY"/>.</param>
        /// <param name="startpos">Not Supported</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_put(Context context, PhpResource ftp_stream, string remote_file, string local_file, int mode = FTP_IMAGE, int startpos = 0)
        {
            return Put(context, ftp_stream, remote_file, local_file, mode = FTP_IMAGE, false, startpos);
        }

        /// <summary>
        /// Closes the given link identifier and releases the resource.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <returns>Returns <c>TRUE</c> on success or <c>FALSE</c> on failure.</returns>
        public static bool ftp_close(PhpResource ftp_stream)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            ftp_stream.Dispose();

            return true;
        }

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
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            /* Command, what uses this function, has second parameter some path to file. So Argument exception 
             can appear only when file does not exist*/
            catch (ArgumentException)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, path);
            }

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
        /// <param name="path">The directory to delete. 
        /// This must be either an absolute or relative path to an empty directory.</param>
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
            
            if (resource != null && FtpCommand(directory, path => resource.Client.CreateDirectory(path)))
            {
                return directory;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Changes the current directory to the specified one.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="directory">The target directory.</param>
        /// <returns>Returns TRUE on success or FALSE on failure. 
        /// If changing directory fails, PHP will also throw a warning.</returns>
        public static bool ftp_chdir(PhpResource ftp_stream, string directory)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            return FtpCommand(directory, resource.Client.SetWorkingDirectory);
        }

        /// <summary>
        /// Renames a file or a directory on the FTP server
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="oldname">The old file/directory name.</param>
        /// <param name="newname">The new name.</param>
        /// <returns>Returns TRUE on success or FALSE on failure. Upon failure 
        /// (such as attempting to rename a non-existent file), an E_WARNING error will be emitted.</returns>
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
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

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
        /// <param name="option">Currently, the following options are supported: FTP_TIMEOUT_SEC, 
        /// FTP_AUTOSEEK, FTP_USEPASVADDRESS</param>
        /// <returns>Returns the value on success or FALSE if the given option is not supported. In the latter case,
        /// a warning message is also thrown.</returns>
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
                    return PhpValue.Create(resource.Client.ConnectTimeout / 1000);
                /* Ignore the IP address returned by the FTP server in response to the PASV command 
                 * and instead use the IP address that was supplied in the ftp_connect() */
                case FTP_USEPASVADDRESS:
                    return resource.UsePASVAddress;
                default:
                    PhpException.Throw(PhpError.Warning, Resources.Resources.arg_invalid_value, option.ToString(), nameof(option));
                    return PhpValue.False;
            }
        }

        /// <summary>
        /// This function controls various runtime options for the specified FTP stream.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="option">Currently, the following options are supported:FTP_TIMEOUT_SEC, 
        /// FTP_AUTOSEEK, FTP_USEPASVADDRESS</param>
        /// <param name="value">This parameter depends on which option is chosen to be altered.</param>
        /// <returns>Returns TRUE if the option could be set; FALSE if not. A warning message will be thrown 
        /// if the option is not supported or the passed value doesn't match the expected value for the given option.</returns>
        public static bool ftp_set_option(PhpResource ftp_stream, int option, PhpValue value)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            switch (option)
            {
                case FTP_AUTOSEEK:
                    if (value.IsBoolean(out var result))
                    {
                        resource.Autoseek = result;
                        return true;
                    }
                    else
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.unexpected_arg_given,
                            nameof(value), PhpVariable.TypeNameBoolean, PhpVariable.GetTypeName(value));
                        return false;
                    }

                case FTP_TIMEOUT_SEC:
                    if (value.IsLong(out var time))
                    {
                        if (time <= 0)
                        {
                            PhpException.Throw(PhpError.Warning, Resources.Resources.sleep_seconds_less_zero);
                            return false;
                        }

                        resource.Client.ReadTimeout = (int)time * 1000;
                        resource.Client.ConnectTimeout = resource.Client.ReadTimeout;
                        resource.Client.DataConnectionConnectTimeout = resource.Client.ReadTimeout;
                        resource.Client.DataConnectionReadTimeout = resource.Client.ReadTimeout;
                        return true;
                    }
                    else
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.unexpected_arg_given,
                            nameof(value), PhpVariable.TypeNameInteger, PhpVariable.GetTypeName(value));
                        return false;
                    }

                case FTP_USEPASVADDRESS:
                    if (value.IsBoolean(out result))
                    {
                        resource.UsePASVAddress = result;
                        return true;
                    }
                    else
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.unexpected_arg_given,
                            nameof(value), PhpVariable.TypeNameBoolean, PhpVariable.GetTypeName(value));
                        return false;
                    }
                default:
                    PhpException.Throw(PhpError.Warning, Resources.Resources.arg_invalid_value, option.ToString(), nameof(option));
                    return false;
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
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            catch (ArgumentException)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, remote_file);
            }

            return -1;
        }

        /// <summary>
        /// Turns on or off passive mode. In passive mode, data connections are initiated by the client,
        /// rather than by the server. It may be needed if the client is behind firewall. 
        /// Can only be called after a successful login or otherwise it will fail.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="pasv">If <c>TRUE</c>, the passive mode is turned on, else it's turned off.</param>
        /// <returns>Returns <c>TRUE</c> on success or <c>FALSE</c> on failure.</returns>
        public static bool ftp_pasv(PhpResource ftp_stream, bool pasv)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            resource.Client.DataConnectionType = pasv
                ? (resource.UsePASVAddress ? FtpDataConnectionType.PASV : FtpDataConnectionType.PASVEX)
                : FtpDataConnectionType.AutoActive;

            return true;
        }

        /// <summary>
        /// Sets the permissions on the specified remote file to mode.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="mode">The new permissions, given as an octal value.</param>
        /// <param name="filename">The remote file.</param>
        /// <returns>Returns the new file permissions on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static int ftp_chmod(PhpResource ftp_stream, int mode, string filename)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
            {
                return -1;
            }

            try
            {

                // FtpClient converts given integer to string,
                // expecting it to result in unix-chmod like number.

                resource.Client.Chmod(filename, ConvertUnixModeFromInput(mode));
                return mode;
            }
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

            //
            return -1;
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
            var systype = resource.Client.SystemType.AsSpan().TrimStart();  // trim leading spaces
            var space = systype.IndexOf(' ');
            systype = space < 0 ? systype : systype.Slice(0, space);        // slice at first space separator

            return systype.ToString();
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
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

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
                return DateTimeUtils.UtcToUnixTimeStamp(resource.Client.GetModifiedTime(remote_file, FtpDate.Original).ToUniversalTime());
            }
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

            return -1;
        }

        /// <summary>
        /// This function converts string representation of octal value, which is converted to int value,
        /// to int value with same digits as string representation. This special method is provided
        /// because of FluentFTP implementation.
        /// </summary>
        /// <param name="octalMode"></param>
        /// <returns>Same value in decimal base</returns>
        private static int ConvertUnixModeFromInput(int octalMode)
        {
            return octalMode = (octalMode & 7) + (((octalMode >> 3) & 7) * 10) + (((octalMode >> 6) & 7) * 100);
        }

        /// <summary>
        /// executes the FTP LIST command, and returns the result as an array.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="directory">The directory path. May include arguments for the LIST command.</param>
        /// <param name="recursive">If set to TRUE, the issued command will be LIST -R.</param>
        /// <returns>Returns an array where each element corresponds to one line of text.
        /// Returns FALSE when passed directory is invalid.</returns>
        [return: CastToFalse]
        public static PhpArray ftp_rawlist(PhpResource ftp_stream, string directory, bool recursive = false)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            try
            {
                FtpListItem[] list;
                /* If recusrive set to TRUE, the issued command will be LIST -R otherwise is it like LIST. 
                 * Server must support it ! */
                if (resource.Client.RecursiveList)
                {
                    list = recursive
                        ? resource.Client.GetListing(directory, FtpListOption.Recursive | FtpListOption.ForceList)
                        : resource.Client.GetListing(directory);
                }
                else
                {
                    list = resource.Client.GetListing(directory, FtpListOption.ForceList);
                }

                return GetArrayOfInputRaw(list);
            }
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

            return null;
        }

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
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
            {
                return false;
            }

            // Check file resource
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return false;
            }

            resource.Client.DownloadDataType = (mode == FTP_ASCII) ? FtpDataType.ASCII : FtpDataType.Binary;

            resumepos = SetupDownloadResuming(resumepos, stream, resource.Autoseek);

            try
            {
                // Use the async version to prevent calling Write on the output stream, as it might be not allowed
                // (e.g. in Kestrel)
                return resource.Client.DownloadAsync(stream.RawStream, remotefile, resumepos).Result;
            }
            catch (FtpException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.InnerException.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

            return false;
        }

        /// <summary>
        /// Uploads the data from a file pointer to a remote file on the FTP server.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="handle">An open file pointer on the local file. Reading stops at end of file.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="startpos">Not Supported</param>
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
            {
                return false;
            }

            if (startpos != 0)
            {
                // There is no API for this parameter in FluentFTP Library. 
                PhpException.ArgumentValueNotSupported(nameof(startpos), startpos);
            }

            resource.Client.UploadDataType = (mode == FTP_ASCII) ? FtpDataType.ASCII : FtpDataType.Binary;

            try
            {
                return resource.Client.Upload(stream.RawStream, remote_file, FtpRemoteExists.Overwrite) != FtpStatus.Failed;
            }
            catch (FtpException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.InnerException.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

            return false;
        }

        /// <summary>
        /// Sends a SITE command to the server
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="command">The SITE command. Note that this parameter isn't escaped so there may be some issues
        /// with filenames containing spaces and other characters.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_site(PhpResource ftp_stream, string command)
        {
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
            {
                return false;
            }

            try
            {
                var reply = resource.Client.Execute($"SITE {command}");

                return
                    int.TryParse(reply.Code, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) &&
                    code >= 200 && code < 300;
            }
            catch (FtpException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.InnerException.Message);
            }

            return false;
        }

        /// <summary>
        /// Append the contents of a file to another file on the FTP server
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_append(Context context, PhpResource ftp_stream, string remote_file, string local_file, int mode = FTP_IMAGE)
        {
            return Put(context, ftp_stream, remote_file, local_file, mode, true, 0);
        }

        /// <summary>
        /// Loads file on server.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        private static bool Put(Context context, PhpResource ftp_stream, string remote_file, string local_file, int mode, bool append, int startpos)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            string localPath = FileSystemUtils.AbsolutePath(context, local_file);
            if (!FileSystemUtils.IsLocalFile(localPath))
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, local_file);
                return false;
            }

            // Two types of data transfer
            resource.Client.UploadDataType = (mode == FTP_ASCII) ? FtpDataType.ASCII : FtpDataType.Binary;

            if (startpos != 0) // There is no API for this parameter in FluentFTP Library.
                PhpException.ArgumentValueNotSupported(nameof(startpos), startpos);

            try
            {
                return resource.Client.UploadFile(localPath, remote_file, append ? FtpRemoteExists.Append : FtpRemoteExists.Overwrite) != FtpStatus.Failed;
            }
            /* FtpException everytime wraps other exceptions (Message from server). 
            * https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP/Client/FtpClient_HighLevelUpload.cs#L595 */
            catch (FtpException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.InnerException.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

            return false;
        }

        /// <summary>
        /// Changes to the parent directory
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_cdup(PhpResource ftp_stream)
        {
            return ftp_chdir(ftp_stream, "..");
        }

        /// <summary>
        /// Returns a list of files in the given directory
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="directory">The directory to be listed.</param>
        /// <returns>Returns an array of arrays with file infos from the specified directory on success or FALSE on error.</returns>
        [return: CastToFalse]
        public static PhpArray ftp_mlsd(PhpResource ftp_stream, string directory)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            if (!resource.Client.HasFeature(FtpCapability.MLSD))
                return null;

            try
            {
                FtpListItem[] list = resource.Client.GetListing(directory, FtpListOption.Auto);

                return GetArrayOfInputMLSD(list);
            }
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
            }

            return null;
        }

        private static PhpArray GetArrayOfInputRaw(FtpListItem[] list)
        {
            var result = new PhpArray(list.Length);

            foreach (var item in list) // NOTE: compiler makes `for` loop out of it
            {
                result.Add(item.Input);
            }

            return result;
        }

        private static PhpArray GetArrayOfInputMLSD(FtpListItem[] list)
        {
            var result = new PhpArray(list.Length);
            // For optimalization we will asume that every file has some number of properties (depends on system).
            int numberOfItems = 7;

            foreach (var item in list)
            {
                var itemArr = new PhpArray(numberOfItems);

                // name
                if (!String.IsNullOrEmpty(item.Name))
                    itemArr.Add("name", item.Name);
                // type of file
                itemArr.Add("type", item.Type.ToString());
                // modify
                if (item.Modified != null)
                {
                    itemArr.Add("modify", DateTimeUtils.UtcToUnixTimeStamp(item.Modified.ToUniversalTime()));
                }
                // chmod
                if (item.Chmod != 0)
                {
                    itemArr.Add("UNIX.mode", ConvertUnixModeFromInput(item.Chmod));
                }
                // owner perm
                if (!String.IsNullOrEmpty(item.RawOwner))
                {
                    itemArr.Add("UNIX.owner", item.RawOwner);
                }
                // group perm
                if (!String.IsNullOrEmpty(item.RawGroup))
                {
                    itemArr.Add("UNIX.group", item.RawGroup);
                }
                // size
                if (item.Size != -1)
                {
                    itemArr.Add("size", item.Size);
                }

                result.Add(itemArr);
            }

            return result;
        }

        /// <summary>
        /// Alias of ftp_close()
        /// </summary>
        /// <param name="ftp_stream"></param>
        /// <returns></returns>
        public static bool ftp_quit(PhpResource ftp_stream)
        {
            return ftp_close(ftp_stream);
        }

        /// <summary>
        /// Sends an arbitrary command to an FTP server
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>Returns the server's response as an array of strings. No parsing is performed on the response 
        /// string, nor does ftp_raw() determine if the command succeeded.</returns>
        [return: CastToFalse]
        public static PhpArray ftp_raw(PhpResource ftp_stream, string command)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return null;

            // API of Fluent FTP offers only on message in response. So in the result will be always only one item.
            int numberOfMessages = 1;

            FtpReply reply = resource.Client.Execute(command);
            PhpArray result = new PhpArray(numberOfMessages);

            result.Add($"{reply.InfoMessages} {reply.Code} {reply.Message}".Trim());

            return result;
        }

        /// <summary>
        /// Sends an ALLO command to the remote FTP server to allocate space for a file to be uploaded.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="filesize">The number of bytes to allocate.</param>
        /// <param name="result">A textual representation of the servers response will be returned by reference in 
        /// result if a variable is provided.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_alloc(PhpResource ftp_stream, int filesize, ref string result)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return false;

            FtpReply reply = resource.Client.Execute($"ALLO {filesize}");

            if (reply.Message != null)
                result = reply.Message;

            int code;
            if (int.TryParse(reply.Code, out code))
            {
                if (code < 200 || code > 300)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves a remote file from the FTP server, and saves it into a local file.
        /// </summary>
        /// <param name="ctx">Context of script</param>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="local_file">The local file path (will be overwritten if the file already exists).</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="resumepos">The position in the remote file to start downloading from.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_get(Context ctx, PhpResource ftp_stream, string local_file, string remote_file, int mode = FTP_BINARY, int resumepos = 0)
        {
            using (var stream = PhpPath.fopen(ctx, local_file, "w"))
            {
                return ftp_fget(ftp_stream, stream, remote_file, mode, resumepos);
            }
        }

        #region No-Blocking Functions

        /// <summary>
        /// Continues retrieving/sending a file non-blocking.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <returns>Returns FTP_FAILED or FTP_FINISHED or FTP_MOREDATA.</returns>
        public static int ftp_nb_continue(PhpResource ftp_stream)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return FTP_FAILED;

            return TasksGetInfo(resource);
        }

        /// <summary>
        /// Stores a file on the FTP server (non-blocking)
        /// </summary>
        /// <param name="ctx">Context of script.</param>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="local_file">The local file path.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="startpos">The position in the remote file to start uploading to.</param>
        /// <returns>Returns FTP_FAILED or FTP_FINISHED or FTP_MOREDATA.</returns>
        public static int ftp_nb_put(Context ctx, PhpResource ftp_stream, string remote_file, string local_file, int mode = FTP_BINARY, int startpos = 0)
        {
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
            {
                return FTP_FAILED;
            }

            if (startpos != 0) // There is no API for this parameter in FluentFTP Library.
                PhpException.ArgumentValueNotSupported(nameof(startpos), startpos);

            string localPath = FileSystemUtils.AbsolutePath(ctx, local_file);

            resource.PrepareForPendingOperation(mode);

            resource.PendingOperationTask = resource.Client.UploadFileAsync(localPath, remote_file, FtpRemoteExists.Overwrite, false, FtpVerify.None, null, resource.TokenSource.Token);

            return TasksGetInfo(resource);
        }

        /// <summary>
        /// Stores a file from an open file to the FTP server (non-blocking)
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="handle">An open file pointer on the local file. Reading stops at end of file.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="startpos">The position in the remote file to start uploading to.</param>
        /// <returns>Returns FTP_FAILED or FTP_FINISHED or FTP_MOREDATA.</returns>
        public static int ftp_nb_fput(PhpResource ftp_stream, string remote_file, PhpResource handle, int mode = FTP_BINARY, int startpos = 0)
        {
            // Check file resource
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return FTP_FAILED;
            }
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
            {
                return FTP_FAILED;
            }

            if (startpos != 0) // There is no API for this parameter in FluentFTP Library.
                PhpException.ArgumentValueNotSupported(nameof(startpos), startpos);

            resource.PrepareForPendingOperation(mode);

            resource.PendingOperationTask = resource.Client.UploadAsync(stream.RawStream, remote_file, FtpRemoteExists.Overwrite, false, null, resource.TokenSource.Token);

            return TasksGetInfo(resource);
        }

        /// <summary>
        /// Retrieves a file from the FTP server and writes it to a local file (non-blocking).
        /// </summary>
        /// <param name="ctx">Constext of script.</param>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="local_file">The local file path (will be overwritten if the file already exists).</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="resumepos">The position in the remote file to start downloading from.</param>
        /// <returns>Returns FTP_FAILED or FTP_FINISHED or FTP_MOREDATA.</returns>
        public static int ftp_nb_get(Context ctx, PhpResource ftp_stream, string local_file, string remote_file, int mode = FTP_BINARY, int resumepos = 0)
        {
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
            {
                return FTP_FAILED;
            }

            if (resumepos != 0) // There is no API for this parameter in FluentFTP Library.
                PhpException.ArgumentValueNotSupported(nameof(resumepos), resumepos);

            string localPath = FileSystemUtils.AbsolutePath(ctx, local_file);

            resource.PrepareForPendingOperation(mode);

            resource.PendingOperationTask = resource.Client.DownloadFileAsync(localPath, remote_file, FtpLocalExists.Overwrite, FtpVerify.None, null, resource.TokenSource.Token);

            return TasksGetInfo(resource);
        }

        /// <summary>
        /// Retrieves a file from the FTP server and writes it to a local file (non-blocking).
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="handle">An open file pointer in which we store the data.</param>
        /// <param name="remote_file">The remote file path.</param>
        /// <param name="mode">The transfer mode. Must be either FTP_ASCII or FTP_BINARY.</param>
        /// <param name="resumepos">The position in the remote file to start downloading from.</param>
        /// <returns>Returns FTP_FAILED or FTP_FINISHED or FTP_MOREDATA.</returns>
        public static int ftp_nb_fget(PhpResource ftp_stream, PhpResource handle, string remote_file, int mode = FTP_BINARY, int resumepos = 0)
        {
            // Check ftp_stream resource
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
            {
                return FTP_FAILED;
            }

            // Check file resource
            var stream = PhpStream.GetValid(handle);
            if (stream == null)
            {
                return FTP_FAILED;
            }

            resumepos = SetupDownloadResuming(resumepos, stream, resource.Autoseek);

            resource.PrepareForPendingOperation(mode);

            resource.PendingOperationTask = resource.Client.DownloadAsync(stream.RawStream, remote_file, resumepos, null, resource.TokenSource.Token);

            return TasksGetInfo(resource);
        }

        private static int SetupDownloadResuming(int resumepos, PhpStream stream, bool autoseek)
        {
            // Ignore autoresume if autoseek is switched off 
            if (autoseek && resumepos == FTP_AUTORESUME)
            {
                resumepos = 0;
            }

            if (autoseek && resumepos != 0)
            {
                if (resumepos == FTP_AUTORESUME)
                {
                    stream.Seek(0, SeekOrigin.End);
                    resumepos = stream.Tell();
                }
                else
                {
                    stream.Seek(resumepos, SeekOrigin.Begin);
                }
            }

            return resumepos;
        }

        private static int TasksGetInfo(FtpResource ftp_stream)
        {
            var task = ftp_stream.PendingOperationTask;

            if (task == null)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.ftp_error_no_nb);
                return FTP_FAILED;
            }

            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    foreach (Exception ex in task.Exception.InnerExceptions)
                    {
                        PhpException.Throw(PhpError.Warning, ex.Message);
                    }

                    return FTP_FAILED;

                case TaskStatus.Canceled:
                    return FTP_FAILED;

                case TaskStatus.RanToCompletion:

                    return task switch
                    {
                        Task<bool> booltask => booltask.Result ? FTP_FINISHED : FTP_FAILED,
                        Task<FtpStatus> ftptask => ftptask.Result != FtpStatus.Failed ? FTP_FINISHED : FTP_FAILED,
                        _ => throw new NotImplementedException(task.GetType().Name),
                    };

                default:
                    return FTP_MOREDATA;
            }
        }

        #endregion
    }
}
