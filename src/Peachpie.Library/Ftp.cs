using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Pchp.Core;
using FluentFTP;
using System.IO;
using System.Net.Sockets;

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

        #endregion

        #region FtpResource

        /// <summary>
        /// Context of ftp session
        /// </summary>
        internal class FtpResource : PhpResource
        {
            public FtpResource(FtpClient client) : base("FTP Buffer")
            {
                Client = client;
            }

            public FtpClient Client { get; } //  nastaveni property samostatne settery

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
                PhpException.InvalidArgumentType(nameof(context), PhpResource.PhpTypeName);
            }

            return h;
        }

        #endregion

        #region ftp_connect, ftp_login, ftp_put, ftp_close

        [return: CastToFalse]
        /// <summary>
        /// ftp_connect() opens an FTP connection to the specified host.
        /// </summary>
        /// <param name="host">The FTP server address. This parameter shouldn't have any trailing slashes and shouldn't be prefixed with ftp://.</param>
        /// <param name="port">This parameter specifies an alternate port to connect to. If it is omitted or set to zero, then the default FTP port, 21, will be used.</param>
        /// <param name="timeout">This parameter specifies the timeout in seconds for all subsequent network operations. If omitted, the default value is 90 seconds. The timeout can be changed and queried at any time with ftp_set_option() and ftp_get_option().</param>
        /// <returns>Returns a FTP stream on success or FALSE on error.</returns>
        public static PhpResource ftp_connect(string host, int port = 21, int timeout = 90)
        {
            FtpClient client = new FtpClient(host);
            client.ConnectTimeout = timeout;
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

                throw ex;
            }

            return new FtpResource(client);
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
        /// ftp_put() stores a local file on the FTP server.
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


            if (startpos == 0) // https://github.com/php/php-src/blob/a1479fbbd9d4ad53fb6b0ed027548ea078558f5b/ext/ftp/ftp.c
                throw new NotSupportedException();

            try
            {
                return resource.Client.UploadFile(local_file, remote_file, FtpExists.Overwrite);
            }
            catch (FtpException ex)
            {
                // FtpException everytime wraps other exceptions (Message from server). https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP/Client/FtpClient_HighLevelUpload.cs#L595                    
                PhpException.Throw(PhpError.Warning, ex.InnerException.Message);
                return false;
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, ex.ParamName);
                return false;
            }
        }

        /// <summary>
        /// ftp_close() closes the given link identifier and releases the resource.
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

        #region ftp_delete, ftp_rmdir, ftp_mkdir

        private delegate void CommandFunction(string path);

        /// <summary>
        /// Execute specific command
        /// </summary>
        private static bool FtpCommand(string path, CommandFunction func)
        {
            try
            {
                func(path);
            }
            catch (FtpCommandException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return false;
            }
            catch (ArgumentException)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, path);
                return false;
            }

            return true;
        }

        /// <summary>
        /// ftp_delete() deletes the file specified by path from the FTP server.
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
        public static PhpValue ftp_mkdir(PhpResource ftp_stream, string directory)
        {
            var resource = ValidateFtpResource(ftp_stream);
            if (resource == null)
                return PhpValue.False;

            if (FtpCommand(directory, resource.Client.CreateDirectory))
                return PhpValue.Create(directory);
            else
                return PhpValue.False;
        }

        #endregion
    }
}
