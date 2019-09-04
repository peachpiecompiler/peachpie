using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Pchp.Core;
using FluentFTP;
using System.IO;

namespace Pchp.Library
{
    [PhpExtension("FTP")]
    /// <summary>
    /// Class implements client access to files servers speaking the File Transfer Protocol (FTP)
    /// </summary>
    public static class Ftp
    {

        #region Ftp(Constants)

        public const int FTP_ASCII = 1;
        public const int FTP_TEXT = FTP_ASCII; // Alias of FTP_ASCII
        public const int FTP_BINARY = 2;
        public const int FTP_IMAGE = FTP_BINARY; // Alias of FTP_BINARY
        public const string resourceType = "FTP Buffer";

        #endregion

        #region FtpResource

        /// <summary>
        /// Context of ftp session
        /// </summary>
        public class FtpResource : PhpResource
        {
            FtpClient client;

            public FtpResource(string resourceTypeName,FtpClient client) : base(resourceTypeName)
            {
                Client = client; 
            }

            public FtpClient Client { get => client; set => client = value; }
        }

        #endregion

        #region ftp_connect, ftp_login, ftp_put, ftp_close

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

            try // Try to connect, if excetion, return false
            {
                client.Connect();
            }
            catch ( FtpAuthenticationException )
            {
                return new FtpResource(resourceType, client);
            }
            catch ( Exception )
            {
                return null;
            }

            return new FtpResource(resourceType, client);
        }

        /// <summary>
        /// Logs in to the given FTP stream.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <param name="username">The username (USER).</param>
        /// <param name="password">The password (PASS).</param>
        /// <returns>Returns TRUE on success or FALSE on failure. If login fails, PHP will also throw a warning.</returns>
        public static bool ftp_login(FtpResource ftp_stream, string username, string password)
        {
            if (ftp_stream == null) // Warning, invalid type
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_argument, "ftp_stream");
                return false;
            }

            if (ftp_stream.TypeName != resourceType || !ftp_stream.IsValid ) // Warning, invalid resource
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_resource, resourceType);
                return false;
            }

            NetworkCredential credential = new NetworkCredential(username, password);
            ftp_stream.Client.Credentials = credential;

            try
            {
                ftp_stream.Client.Connect();
            }
            catch ( FluentFTP.FtpAuthenticationException ) // Warning, Wrong password or name
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.incorrect_pass_or_login, resourceType);
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
        public static bool ftp_put(FtpResource ftp_stream, string remote_file, string local_file, int mode = FTP_IMAGE, int startpos = 0)
        {
            if (ftp_stream == null) // Warning, invalid type
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_argument, "ftp_stream");
                return false;
            }

            if (ftp_stream.TypeName != resourceType || !ftp_stream.IsValid) // Warning, invalid resource
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_resource, resourceType);
                return false;
            }

            if (!File.Exists(local_file))
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, local_file);
                return false;
            }

            ftp_stream.Client.UploadDataType = (mode == 1) ? FtpDataType.ASCII : FtpDataType.Binary;
            //Startpos https://github.com/php/php-src/blob/a1479fbbd9d4ad53fb6b0ed027548ea078558f5b/ext/ftp/ftp.c??

            bool result = true;
            try
            {
                result = ftp_stream.Client.UploadFile(local_file, remote_file, FtpExists.Overwrite);
            }
            catch (FluentFTP.FtpException e)
            {
                if (e.InnerException != null)
                    PhpException.Throw(PhpError.Warning, e.InnerException.Message);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// ftp_close() closes the given link identifier and releases the resource.
        /// </summary>
        /// <param name="ftp_stream">The link identifier of the FTP connection.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool ftp_close(FtpResource ftp_stream)
        {
            if (ftp_stream == null) // Warning, invalid type
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_argument, "ftp_stream");
                return false;
            }

            if (ftp_stream.TypeName != resourceType || !ftp_stream.IsValid) // Warning, invalid resource
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_resource, resourceType);
                return false;
            }

            ftp_stream.Client.Dispose();
            ftp_stream.Dispose();

            return true;
        }

        #endregion
    }
}
