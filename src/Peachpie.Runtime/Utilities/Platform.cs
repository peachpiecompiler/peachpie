using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Pchp.Core.Utilities
{
    #region CurrentPlatform

    /// <summary>
    /// Platform specific constants.
    /// </summary>
    public static class CurrentPlatform
    {
        static CurrentPlatform()
        {
            if (IsWindows)
            {
                DirectorySeparator = PathUtils.DirectorySeparator;
                AltDirectorySeparator = PathUtils.AltDirectorySeparator;
                PathSeparator = ';';
                PathComparer = StringComparer.OrdinalIgnoreCase;
                PathStringComparison = StringComparison.OrdinalIgnoreCase;
            }
            else
            {
                DirectorySeparator = PathUtils.AltDirectorySeparator;
                AltDirectorySeparator = PathUtils.DirectorySeparator;
                PathSeparator = ':';
                PathComparer = StringComparer.Ordinal;
                PathStringComparison = StringComparison.Ordinal;
            }
        }

        /// <summary>
        /// Gets value indicating the guest operating.
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Gets value indicating the guest operating.
        /// </summary>
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Gets value indicating the guest operating.
        /// </summary>
        public static bool IsOsx => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static readonly char DirectorySeparator;

        public static readonly char AltDirectorySeparator;

        public static readonly char PathSeparator;

        /// <summary>
        /// Gets string comparer for path comparison on current platform.
        /// </summary>
        /// <remarks>Ignore case on Windows, otherwise case-sensitive.</remarks>
        public static readonly StringComparer PathComparer;

        /// <summary>
        /// Gets string comparison method for path comparison on current platform.
        /// </summary>
        /// <remarks>Ignore case on Windows, otherwise case-sensitive.</remarks>
        public static readonly StringComparison PathStringComparison;

        /// <summary>
        /// Replaces <see cref="AltDirectorySeparator"/> to <see cref="DirectorySeparator"/>.
        /// </summary>
        public static string NormalizeSlashes(string path) => path.Replace(AltDirectorySeparator, DirectorySeparator);
    }

    #endregion

    #region WindowsPlatform

    /// <summary>
    /// Windows specific functions.
    /// </summary>
    static class WindowsPlatform
    {
        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/console/setconsolemode
        /// </summary>
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        const int STD_INPUT_HANDLE = -10;
        const int STD_OUTPUT_HANDLE = -11;
        const int STD_ERROR_HANDLE = -12;

        const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        static bool Enable_VT100(int handle_no)
        {
            var handle = GetStdHandle(handle_no);

            if (handle != IntPtr.Zero && GetConsoleMode(handle, out var mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

                return SetConsoleMode(handle, mode);
            }

            return false;
        }

        public static void Enable_VT100()
        {
            Enable_VT100(STD_OUTPUT_HANDLE);
            Enable_VT100(STD_ERROR_HANDLE);
        }
    }

    #endregion
}
