using System;
using System.Runtime.InteropServices;

namespace Pchp.Library.Streams
{
    /// <summary>
    /// Managed equivalent of <c>stat</c> structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct StatStruct
    {
        /// <summary>device number</summary>
        public readonly uint st_dev;
        /// <summary>inode number</summary>
        public readonly ushort st_ino;
        /// <summary>inode protection mode</summary>
        public readonly ushort st_mode;
        /// <summary>number of links</summary>
        public readonly short st_nlink;
        /// <summary>userid of owner</summary>
        public readonly short st_uid;
        /// <summary>groupid of owner</summary>
        public readonly short st_gid;
        /// <summary>device type, if inode device -1</summary>
        public readonly uint st_rdev;
        /// <summary>size in bytes</summary>
        public readonly long st_size;
        /// <summary>time of last access (unix timestamp)</summary>
        public readonly long st_atime;
        /// <summary>time of last modification (unix timestamp)</summary>
        public readonly long st_mtime;
        /// <summary>time of last change (unix timestamp)</summary>
        public readonly long st_ctime;

        /// <param name="st_dev">device number</param>
        /// <param name="st_ino">inode number</param>
        /// <param name="st_mode">inode protection mode</param>
        /// <param name="st_nlink">number of links</param>
        /// <param name="st_uid">userid of owner</param>
        /// <param name="st_gid">groupid of owner</param>
        /// <param name="st_rdev">device type, if inode device -1</param>
        /// <param name="st_size">size in bytes</param>
        /// <param name="st_atime">time of last access (unix timestamp)</param>
        /// <param name="st_mtime">time of last modification (unix timestamp)</param>
        /// <param name="st_ctime">time of last change (unix timestamp)</param>
        public StatStruct(
            uint st_dev = 0,
            ushort st_ino = 0,
            FileModeFlags st_mode = default,
            short st_nlink = 1,
            short st_uid = 0,
            short st_gid = 0,
            uint st_rdev = 0,
            long st_size = 0,
            long st_atime = 0,
            long st_mtime = 0,
            long st_ctime = 0)
        {
            this.st_dev = st_dev;
            this.st_ino = st_ino;
            this.st_mode = (ushort)st_mode;
            this.st_nlink = st_nlink;
            this.st_uid = st_uid;
            this.st_gid = st_gid;
            this.st_rdev = st_rdev;
            this.st_size = st_size;
            this.st_atime = st_atime;
            this.st_mtime = st_mtime;
            this.st_ctime = st_ctime;
        }

        /// <summary>
        /// An invalid value.
        /// </summary>
        internal static StatStruct Invalid => new StatStruct(st_size: -1);

        /// <summary>
        /// Gets value indicating the stat is valid (not <see cref="Invalid"/>).
        /// </summary>
        internal bool IsValid => st_size >= 0;
    }
}
