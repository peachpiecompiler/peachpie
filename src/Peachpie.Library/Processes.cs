using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Pchp.Library
{
    #region PhpProcessHandle

    internal class PhpProcessHandle : PhpResource
    {
        public Process/*!*/ Process { get { return process; } }
        private Process/*!*/ process;

        public string/*!*/ Command { get { return command; } }
        private string/*!*/ command;

        internal PhpProcessHandle(Process/*!*/ process, string/*!*/ command)
            : base("process")
        {
            Debug.Assert(process != null && command != null);
            this.process = process;
            this.command = command;
        }

        protected override void FreeManaged()
        {
            process.Dispose();
            base.FreeManaged();
        }

        internal static PhpProcessHandle Validate(PhpResource resource)
        {
            PhpProcessHandle result = resource as PhpProcessHandle;

            if (result == null || !result.IsValid)
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_process_resource);
                return null;
            }

            return result;
        }
    }

    #endregion

    [PhpExtension("standard")]
    public static class Processes
    {
        #region popen, pclose

        private sealed class ProcessWrapper : StreamWrapper
        {
            public Process/*!*/ process;

            public ProcessWrapper(Process/*!*/ process)
            {
                this.process = process;
            }

            public override bool IsUrl { get { return false; } }
            public override string Label { get { return null; } }
            public override string Scheme { get { return null; } }

            public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
            {
                return null;
            }
        }

        /// <summary>
        /// Starts a process and creates a pipe to its standard input or output.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command.</param>
        /// <param name="mode">Pipe open mode (<c>"r"</c> or <c>"w"</c>).</param>
        /// <returns>Opened pipe or <B>null</B> on error.</returns>
        public static PhpResource popen(Context ctx, string command, string mode)
        {
            if (String.IsNullOrEmpty(mode))
            {
                PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.invalid_file_mode, mode);
                return null;
            }

            bool read = mode[0] == 'r';
            bool write = mode[0] == 'w' || mode[0] == 'a' || mode[0] == 'x';

            if (!read && !write)
            {
                PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.invalid_file_mode, mode);
                return null;
            }

            Process process = CreateProcessExecutingCommand(ctx, ref command, false);
            if (process == null) return null;

            process.StartInfo.RedirectStandardOutput = read;
            process.StartInfo.RedirectStandardInput = write;

            if (!StartProcess(process, true))
                return null;

            Stream stream = (read) ? process.StandardOutput.BaseStream : process.StandardInput.BaseStream;
            StreamAccessOptions access = (read) ? StreamAccessOptions.Read : StreamAccessOptions.Write;
            ProcessWrapper wrapper = new ProcessWrapper(process);
            PhpStream php_stream = new NativeStream(ctx, stream, wrapper, access, String.Empty, StreamContext.Default);

            return php_stream;
        }

        /// <summary>
        /// Closes a pipe and a process opened by <see cref="OpenPipe"/>.
        /// </summary>
        /// <param name="pipeHandle">The pipe handle returned by <see cref="OpenPipe"/>.</param>
        /// <returns>An exit code of the process.</returns>
        public static int pclose(PhpResource pipeHandle)
        {
            PhpStream php_stream = PhpStream.GetValid(pipeHandle);
            if (php_stream == null) return -1;

            ProcessWrapper wrapper = php_stream.Wrapper as ProcessWrapper;
            if (wrapper == null) return -1;

            var code = CloseProcess(wrapper.process);
            php_stream.Dispose();
            return code;
        }

        #endregion

        #region proc_open

        /// <summary>
        /// Starts a process and otpionally redirects its input/output/error streams to specified PHP streams.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command"></param>
        /// <param name="descriptors"></param>
        /// Indexed array where the key represents the descriptor number (0 for STDIN, 1 for STDOUT, 2 for STDERR)
        /// and the value represents how to pass that descriptor to the child process. 
        /// A descriptor is either an opened file resources or an integer indexed arrays 
        /// containing descriptor name followed by options. Supported descriptors:
        /// <list type="bullet">
        /// <term><c>array("pipe",{mode})</c></term><description>Pipe is opened in the specified mode .</description>
        /// <term><c>array("file",{path},{mode})</c></term><description>The file is opened in the specified mode.</description>
        /// </list>
        /// <param name="pipes">
        /// Set to indexed array of file resources corresponding to the current process's ends of created pipes.
        /// </param>
        /// <param name="workingDirectory">
        /// Working directory.
        /// </param>
        /// <param name="envVariables"></param>
        /// <param name="options">
        /// Associative array containing following key-value pairs.
        ///   <list type="bullet">
        ///     <term>"suppress_errors"</term><description></description>
        ///   </list>
        /// </param>
        /// <returns>
        /// Resource representing the process.
        /// </returns>
        public static PhpResource proc_open(Context ctx,
            string command, PhpArray descriptors, out PhpArray pipes,
            string workingDirectory = null, PhpArray envVariables = null, PhpArray options = null)
        {
            if (descriptors == null)
            {
                PhpException.ArgumentNull("descriptors");
                pipes = null;
                return null;
            }

            pipes = new PhpArray();
            return Open(ctx, command, descriptors, pipes, workingDirectory, envVariables, options);
        }

        /// <summary>
        /// Opens a process.
        /// </summary>
        static PhpResource Open(Context ctx, string command, PhpArray/*!*/ descriptors, PhpArray/*!*/ pipes,
          string workingDirectory, PhpArray envVariables, PhpArray options)
        {
            if (descriptors == null)
                throw new ArgumentNullException("descriptors");
            if (pipes == null)
                throw new ArgumentNullException("pipes");

            bool bypass_shell = options != null && options["bypass_shell"].ToBoolean();   // quiet

            Process process = CreateProcessExecutingCommand(ctx, ref command, bypass_shell);
            if (process == null)
                return null;

            if (!SetupStreams(process, descriptors))
                return null;

            if (envVariables != null)
                SetupEnvironment(process, envVariables);

            if (workingDirectory != null)
                process.StartInfo.WorkingDirectory = workingDirectory;

            bool suppress_errors = options != null && options["suppress_errors"].ToBoolean();

            if (!StartProcess(process, !suppress_errors))
                return null;

            if (!RedirectStreams(ctx, process, descriptors, pipes))
                return null;

            return new PhpProcessHandle(process, command);
        }

        private const string CommandLineSplitterPattern = @"(?<filename>^""[^""]*""|\S*) *(?<arguments>.*)?";
        private static readonly System.Text.RegularExpressions.Regex/*!*/CommandLineSplitter = new System.Text.RegularExpressions.Regex(CommandLineSplitterPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

        private static Process CreateProcessExecutingCommand(Context ctx, ref string command, bool bypass_shell)
        {
            if (!Execution.MakeCommandSafe(ref command))
                return null;

            Process process = new Process();

            if (bypass_shell)
            {
                var match = CommandLineSplitter.Match(command);
                if (match == null || !match.Success)
                {
                    PhpException.InvalidArgument("command");
                    return null;
                }

                process.StartInfo.FileName = match.Groups["filename"].Value;
                process.StartInfo.Arguments = match.Groups["arguments"].Value;
            }
            else
            {
                process.StartInfo.FileName = "cmd.exe"; //(Environment.OSVersion.Platform != PlatformID.Win32Windows) ? "cmd.exe" : "command.com";
                process.StartInfo.Arguments = "/c " + command;
            }
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = ctx.WorkingDirectory;

            return process;
        }

        private static bool StartProcess(Process/*!*/ process, bool reportError)
        {
            try
            {
                process.Start();
                return true;
            }
            catch (Exception e)
            {
                if (reportError)
                    PhpException.Throw(PhpError.Warning, Resources.LibResources.error_starting_process, e.Message);
                return false;
            }
        }

        private static void SetupEnvironment(Process/*!*/ process, PhpArray/*!*/ envVariables)
        {
            var e = envVariables.GetFastEnumerator();
            while (e.MoveNext())
            {
                string s = e.CurrentKey.String;
                if (s != null)
                {
                    // TODO: process.StartInfo.EnvironmentVariables.Add(s, e.CurrentValue.ToString(ctx));
                }
            }
        }

        private static bool SetupStreams(Process/*!*/ process, PhpArray/*!*/ descriptors)
        {
            var e = descriptors.GetFastEnumerator();
            while (e.MoveNext())
            {
                // key must be an integer:
                if (!e.CurrentKey.IsInteger)
                {
                    PhpException.Throw(PhpError.Warning, Resources.LibResources.argument_not_integer_indexed_array, nameof(descriptors));
                    return false;
                }

                var desc_no = e.CurrentKey.Integer;

                switch (desc_no)
                {
                    case 0: process.StartInfo.RedirectStandardInput = true; break;
                    case 1: process.StartInfo.RedirectStandardOutput = true; break;
                    case 2: process.StartInfo.RedirectStandardError = true; break;
                    default:
                        PhpException.Throw(PhpError.Warning, Resources.LibResources.descriptor_unsupported, desc_no.ToString());
                        return false;
                }
            }
            return true;
        }

        private static bool RedirectStreams(Context ctx, Process/*!*/ process, PhpArray/*!*/ descriptors, PhpArray/*!*/ pipes)
        {
            var descriptors_enum = descriptors.GetFastEnumerator();
            while (descriptors_enum.MoveNext())
            {
                var desc_no = descriptors_enum.CurrentKey.Integer;

                StreamAccessOptions access;
                Stream stream;
                switch (desc_no)
                {
                    case 0: stream = process.StandardInput.BaseStream; access = StreamAccessOptions.Write; break;
                    case 1: stream = process.StandardOutput.BaseStream; access = StreamAccessOptions.Read; break;
                    case 2: stream = process.StandardError.BaseStream; access = StreamAccessOptions.Read; break;
                    default: Debug.Fail(null); return false;
                }

                var value = descriptors_enum.CurrentValue;
                PhpResource resource;
                PhpArray array;

                if ((array = value.AsArray()) != null)
                {
                    if (!array.Contains(0))
                    {
                        // value must be either a resource or an array:
                        PhpException.Throw(PhpError.Warning, Resources.LibResources.descriptor_item_missing_qualifier, desc_no.ToString());
                        return false;
                    }

                    string qualifier = array[0].ToString(ctx);

                    switch (qualifier)
                    {
                        case "pipe":
                            {
                                // mode is ignored (it's determined by the stream):
                                PhpStream php_stream = new NativeStream(ctx, stream, null, access, String.Empty, StreamContext.Default);
                                pipes.Add(desc_no, php_stream);
                                break;
                            }

                        case "file":
                            {
                                if (!array.Contains(1))
                                {
                                    PhpException.Throw(PhpError.Warning, Resources.LibResources.descriptor_item_missing_file_name, desc_no.ToString());
                                    return false;
                                }

                                if (!array.Contains(2))
                                {
                                    PhpException.Throw(PhpError.Warning, Resources.LibResources.descriptor_item_missing_mode, desc_no.ToString());
                                    return false;
                                }

                                string path = array[1].ToString(ctx);
                                string mode = array[2].ToString(ctx);

                                PhpStream php_stream = PhpStream.Open(ctx, path, mode, StreamOpenOptions.Empty, StreamContext.Default);
                                if (php_stream == null)
                                    return false;

                                //if (!ActivePipe.BeginIO(stream, php_stream, access, desc_no)) return false;
                                //break;
                                throw new NotImplementedException();
                            }

                        default:
                            PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_handle_qualifier, qualifier);
                            return false;
                    }
                }
                else if ((resource = value.AsResource()) != null)
                {
                    PhpStream php_stream = PhpStream.GetValid(resource);
                    if (php_stream == null) return false;

                    //if (!ActivePipe.BeginIO(stream, php_stream, access, desc_no)) return false;
                    throw new NotImplementedException();
                }
                else
                {
                    // value must be either a resource or an array:
                    PhpException.Throw(PhpError.Warning, Resources.LibResources.descriptor_item_not_array_nor_resource, desc_no.ToString());
                    return false;
                }
            }

            return true;
        }

        //private sealed class ActivePipe
        //{
        //    private const int BufferSize = 1024;

        //    Stream stream;
        //    StreamAccessOptions access;
        //    PhpStream phpStream;
        //    public AsyncCallback callback;
        //    public byte[] buffer;

        //    public static bool BeginIO(Stream stream, PhpStream phpStream, StreamAccessOptions access, int desc_no)
        //    {
        //        if (access == StreamAccessOptions.Read && !phpStream.CanWrite ||
        //          access == StreamAccessOptions.Write && !phpStream.CanRead)
        //        {
        //            PhpException.Throw(PhpError.Warning, Resources.LibResources.descriptor_item_invalid_mode, desc_no.ToString());
        //            return false;
        //        }

        //        ActivePipe pipe = new ActivePipe();
        //        pipe.stream = stream;
        //        pipe.phpStream = phpStream;
        //        pipe.access = access;
        //        pipe.callback = new AsyncCallback(pipe.Callback);

        //        if (access == StreamAccessOptions.Read)
        //        {
        //            var buffer = new byte[BufferSize];
        //            stream.BeginRead(buffer, 0, buffer.Length, pipe.callback, null);
        //            pipe.buffer = buffer;
        //        }
        //        else
        //        {
        //            pipe.buffer = phpStream.ReadBytes(BufferSize);
        //            if (pipe.buffer != null)
        //                stream.BeginWrite(pipe.buffer, 0, pipe.buffer.Length, pipe.callback, null);
        //            else
        //                stream.Dispose();
        //        }

        //        return true;
        //    }

        //    private void Callback(IAsyncResult ar)
        //    {
        //        if (access == StreamAccessOptions.Read)
        //        {
        //            int count = stream.EndRead(ar);
        //            if (count > 0)
        //            {
        //                if (count != buffer.Length)
        //                {
        //                    // TODO: improve streams
        //                    var buf = new byte[count];
        //                    Buffer.BlockCopy(buffer, 0, buf, 0, count);
        //                    phpStream.WriteBytes(buf);
        //                }
        //                else
        //                {
        //                    phpStream.WriteBytes(buffer);
        //                }

        //                stream.BeginRead(buffer, 0, buffer.Length, callback, ar.AsyncState);
        //            }
        //            else
        //            {
        //                stream.Dispose();
        //            }
        //        }
        //        else
        //        {
        //            buffer = phpStream.ReadBytes(BufferSize);
        //            if (buffer != null)
        //            {
        //                stream.BeginWrite(buffer, 0, buffer.Length, callback, ar.AsyncState);
        //            }
        //            else
        //            {
        //                stream.EndWrite(ar);
        //                stream.Dispose();
        //            }
        //        }
        //    }
        //}

        #endregion

        #region proc_close, proc_get_status, proc_terminate

        public static int proc_close(PhpResource process)
        {
            PhpProcessHandle handle = PhpProcessHandle.Validate(process);
            if (handle == null) return -1;

            var code = CloseProcess(handle.Process);
            handle.Dispose();
            return code;
        }

        private static int CloseProcess(Process/*!*/ process)
        {
            try
            {
                process.WaitForExit();
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.error_waiting_for_process_exit, e.Message);
                return -1;
            }

            return process.ExitCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="process"></param>
        /// <returns>
        /// <list type="bullet">
        /// <term>"command"</term><description>The command string that was passed to proc_open()</description> 
        /// <term>"pid"</term><description>process id</description>
        /// <term>"running"</term><description>TRUE if the process is still running, FALSE if it has terminated</description>  
        /// <term>"signaled"</term><description>TRUE if the child process has been terminated by an uncaught signal. Always set to FALSE on Windows.</description>
        /// <term>"stopped"</term><description>TRUE if the child process has been stopped by a signal. Always set to FALSE on Windows.</description>  
        /// <term>"exitcode"</term><description>the exit code returned by the process (which is only meaningful if running is FALSE)</description>  
        /// <term>"termsig"</term><description>the number of the signal that caused the child process to terminate its execution (only meaningful if signaled is TRUE)</description>  
        /// <term>"stopsig"</term><description>the number of the signal that caused the child process to stop its execution (only meaningful if stopped is TRUE)</description>  
        /// </list>
        /// </returns>
        public static PhpArray proc_get_status(PhpResource process)
        {
            PhpProcessHandle handle = PhpProcessHandle.Validate(process);
            if (handle == null) return null;

            var result = new PhpArray(8)
            {
                {"command", handle.Command},
                {"pid", handle.Process.Id},
                {"running", !handle.Process.HasExited},
                {"signaled", false}, // UNIX
                {"stopped", false},  // UNIX
                {"exitcode", handle.Process.HasExited? handle.Process.ExitCode : -1},
                {"termsig", 0},      // UNIX
                {"stopsig", 0},      // UNIX
            };

            return result;
        }

        public static int proc_terminate(PhpResource process, int signal = 255)
        {
            PhpProcessHandle handle = PhpProcessHandle.Validate(process);
            if (handle == null) return -1;

            try
            {
                handle.Process.Kill();
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning,
                    Resources.LibResources.error_terminating_process,
                    handle.Process.ProcessName, handle.Process.Id.ToString(), e.Message);
                return -1;
            }
            return handle.Process.ExitCode;
        }

        #endregion

        #region NS: proc_nice

        public static bool proc_nice(int priority)
        {
            PhpException.FunctionNotSupported(nameof(proc_nice));    // even in PHP for Windows, it is not available
            return false;
        }

        #endregion
    }
}
