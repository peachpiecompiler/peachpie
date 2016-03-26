using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Pchp.Core
{
    public static class Console
    {
        #region Structure Declarations

        // Standard structures used for interop with kernel32
        [StructLayout(LayoutKind.Sequential)]
        struct COORD
        {
            public short x;
            public short y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public int wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CONSOLE_CURSOR_INFO
        {
            public int dwSize;
            public bool bVisible;
        }

        [Flags]
        public enum InputModeFlags
        {
            ENABLE_PROCESSED_INPUT = 0x01,
            ENABLE_LINE_INPUT = 0x02,
            ENABLE_ECHO_INPUT = 0x04,
            ENABLE_WINDOW_INPUT = 0x08,
            ENABLE_MOUSE_INPUT = 0x10
        }

        [Flags]
        public enum OutputModeFlags
        {
            ENABLE_PROCESSED_OUTPUT = 0x01,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x02
        }

        #endregion

        #region DllImport

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern int GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleScreenBufferInfo", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern int GetConsoleScreenBufferInfo(int hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", EntryPoint = "ReadConsole", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool ReadConsole(int hConsoleInput, StringBuilder buf, int nNumberOfCharsToRead, ref int lpNumberOfCharsRead, int lpReserved);

        [DllImport("kernel32.dll", EntryPoint = "SetConsoleMode", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool SetConsoleMode(int hConsoleHandle, int dwMode);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleMode", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool GetConsoleMode(int hConsoleHandle, ref int dwMode);

        [DllImport("kernel32.dll")]
        static extern bool WriteConsole(int hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);

        #endregion

        #region Constants

        private const int INVALID_HANDLE_VALUE = -1;
        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        
        #endregion
        
        private static int hConsoleOutput;  // handle to output buffer
        private static int hConsoleInput;   // handle to input buffer
        
        static Console()
        {
            // Grab input and output buffer handles
            hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);
            hConsoleInput = GetStdHandle(STD_INPUT_HANDLE);

            //// Get information about the console window characteristics.
            //ConsoleInfo = new CONSOLE_SCREEN_BUFFER_INFO();
            //ConsoleOutputLocation = new COORD();
            //GetConsoleScreenBufferInfo(hConsoleOutput, ref ConsoleInfo);
            //OriginalConsolePen = ConsoleInfo.wAttributes;

            // Disable wrapping at the end of a line (ENABLE_WRAP_AT_EOL_INPUT); this enables rectangles 
            // to be drawn that fill the screen without the window scrolling.
            SetConsoleMode(hConsoleOutput, (int)OutputModeFlags.ENABLE_PROCESSED_OUTPUT);
        }
        
        public static int Write(string value)
        {
            uint written;
            if (!WriteConsole(hConsoleOutput, value, (uint)value.Length, out written, IntPtr.Zero))
                return -1;

            return (int)written;
        }

        /// <summary>
        /// Read a single character from the input buffer. Unlike Console.Read(), which 
        /// only reads from the buffer when the read operation has terminated (e.g. by
        /// pressing Enter), this method reads as soon as the character has been entered.
        /// </summary>
        /// <returns>The character read by the system</returns>
        public static int ReadChar()
        {
            // Temporarily disable character echo (ENABLE_ECHO_INPUT) and line input
            // (ENABLE_LINE_INPUT) during this operation
            SetConsoleMode(hConsoleInput, (int)(InputModeFlags.ENABLE_PROCESSED_INPUT | InputModeFlags.ENABLE_WINDOW_INPUT | InputModeFlags.ENABLE_MOUSE_INPUT));

            int lpNumberOfCharsRead = 0;
            StringBuilder buf = new StringBuilder(1);

            bool success = ReadConsole(hConsoleInput, buf, 1, ref lpNumberOfCharsRead, 0);

            // Reenable character echo and line input
            SetConsoleMode(hConsoleInput,
                (int)(InputModeFlags.ENABLE_PROCESSED_INPUT |
                InputModeFlags.ENABLE_ECHO_INPUT |
                InputModeFlags.ENABLE_LINE_INPUT |
                InputModeFlags.ENABLE_WINDOW_INPUT |
                InputModeFlags.ENABLE_MOUSE_INPUT));

            if (success)
                return (int)buf[0];

            return -1;
        }
    }
}