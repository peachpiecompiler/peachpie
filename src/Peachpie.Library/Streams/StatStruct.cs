using System;
using System.Runtime.InteropServices;

namespace Pchp.Library.Streams
{
    /// <summary>
    /// Managed equivalent of <c>stat</c> structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StatStruct
    {
        public uint st_dev;
        public ushort st_ino;
        public ushort st_mode;
        public short st_nlink;
        public short st_uid;
        public short st_gid;
        public uint st_rdev;
        public long st_size;
        public long st_atime;
        public long st_mtime;
        public long st_ctime;

        /// <summary>
        /// An invalid value.
        /// </summary>
        internal static StatStruct Invalid => new StatStruct() { st_size = -1 };

        /// <summary>
        /// 
        /// </summary>
        internal bool IsValid => st_size >= 0;
    }
}
