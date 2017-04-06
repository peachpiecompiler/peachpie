namespace Peachpie.Library.MySql
{
    /// <summary>
    /// Container containing global constants.
    /// </summary>
    partial class MySql
    {
        #region Connection flags

        /// <summary>
        ///  Use compression protocol.
        /// </summary>
        public const int MYSQL_CLIENT_COMPRESS = 32;

        /// <summary>
        /// Allow space after function names.
        /// Not supported (ignored).
        /// </summary>
        public const int MYSQL_CLIENT_IGNORE_SPACE = 256;

        /// <summary>
        /// Allow interactive_timeout seconds (instead of wait_timeout) of inactivity before closing the connection.
        /// Not supported (ignored).
        /// </summary>
        public const int MYSQL_CLIENT_INTERACTIVE = 1024;

        #endregion

        #region Query result array format

        /// <summary>
        /// Use SSL encryption.
        /// </summary>
        public const int MYSQL_CLIENT_SSL = 2048;

        /// <summary>
        /// Add items keyed by column names.
        /// </summary>
        public const int MYSQL_ASSOC = 1;

        /// <summary>
        /// Add items keyed by column indices.
        /// </summary>
        public const int MYSQL_NUM = 2;

        /// <summary>
        /// Add both items keyed by column names and items keyd by column indices.
        /// </summary>
        public const int MYSQL_BOTH = MYSQL_ASSOC | MYSQL_NUM;

        #endregion
    }
}
