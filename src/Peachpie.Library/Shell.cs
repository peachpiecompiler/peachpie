using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library
{
    public static class Shell
    {
        #region getenv, putenv

        /// <summary>
        /// Gets a value of an environment variable associated with a current process.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="name">A name of the variable.</param>
        /// <returns>Current value of the variable.</returns>
        [return: CastToFalse]
        public static string getenv(Context ctx, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            PhpValue value;
            if (ctx.Server != null && ctx.Server.TryGetValue(name, out value))
            {
                return value.ToStringOrThrow(ctx);
            }
            else
            {
                return System.Environment.GetEnvironmentVariable(name);
            }
        }

        /// <summary>
        /// Sets an environment variable of the current process.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="setting">String in format "{name}={value}".</param>
        public static bool putenv(Context ctx, string setting)
        {
            if (ctx.IsWebApplication)
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.function_disallowed_in_web_context);
                return false;
            }

            if (string.IsNullOrEmpty(setting))
            {
                PhpException.InvalidArgument(nameof(setting), Resources.LibResources.arg_null_or_empty);
                return false;
            }

            int separator_pos = setting.IndexOf('=');
            if (separator_pos == -1)
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.arg_invalid_value, nameof(setting), setting);
                return false;
            }

            var name = setting.Substring(0, separator_pos);
            var value = setting.Substring(separator_pos + 1);

            try
            {
                System.Environment.SetEnvironmentVariable(name, value);
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }

            return true;
        }

        #endregion

        #region escapeshellarg, escapeshellcmd

        /// <summary>
        /// Escapes argument to be passed to shell command.
        /// </summary>
        /// <param name="arg">The argument to excape.</param>
        /// <returns>
        /// <para>
        /// On Windows platform, each occurance of double quote (") and ampersand (&amp;) 
        /// is replaced with a single space. The resulting string is then put into double quotes.
        /// </para>
        /// <para>
        /// On Unix platform, each occurance of single quote (')
        /// is replaced with characters '\'''. The resulting string is then put into single quotes.
        /// </para>
        /// </returns>
        public static string escapeshellarg(string arg)
        {
            if (arg == null || arg.Length == 0) return string.Empty;

            var sb = new StringBuilder(arg.Length + 2);
            sb.Append(' ');
            sb.Append(arg);
            sb.Replace("'", @"'\''");
            sb.Append('\'');
            sb[0] = '\'';

            return sb.ToString();
        }

        /// <summary>
        /// Escape shell metacharacters in a specified shell command.
        /// </summary>
        /// <param name="command">The command to excape.</param>
        /// <para>
        /// On Windows platform, each occurance of a character that might be used to trick a shell command
        /// is replaced with space. These characters are 
        /// <c>", ', #, &amp;, ;, `, |, *, ?, ~, &lt;, &gt;, ^, (, ), [, ], {, }, $, \, \u000A, \u00FF, %</c>.
        /// </para>
        public static string escapeshellcmd(string command)
        {
            return Execution.EscapeCommand(command);
        }

        #endregion

        #region exec

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>The last line of the output.</returns>
        public static string exec(string command)
        {
            string result;

            Execution.ShellExec(command, Execution.OutputHandling.ArrayOfLines, null, out result);
            return result;
        }

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="output">An array where to add items of output. One item per each line of the output.</param>
        /// <returns>The last line of the output.</returns>
        public static string exec(string command, ref PhpArray output)
        {
            int exit_code;
            return exec(command, ref output, out exit_code);
        }

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="output">An array where to add items of output. One item per each line of the output.</param>
        /// <param name="exitCode">Exit code of the process.</param>
        /// <returns>The last line of the output.</returns>
        public static string exec(string command, ref PhpArray output, out int exitCode)
        {
            // creates a new array if user specified variable not containing one:
            if (output == null) output = new PhpArray();

            string result;
            exitCode = Execution.ShellExec(command, Execution.OutputHandling.ArrayOfLines, output, out result);

            return result;
        }

        #endregion

        #region pasthru

        /// <summary>
        /// Executes a command and writes raw output to the output sink set on the current script context.
        /// </summary>
        /// <param name="command">The command.</param>
        public static void passthru(string command)
        {
            string dummy;
            Execution.ShellExec(command, Execution.OutputHandling.RedirectToScriptOutput, null, out dummy);
        }

        /// <summary>
        /// Executes a command and writes raw output to the output sink set on the current script context.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="exitCode">An exit code of the process.</param>
        public static void passthru(string command, out int exitCode)
        {
            string dummy;
            exitCode = Execution.ShellExec(command, Execution.OutputHandling.RedirectToScriptOutput, null, out dummy);
        }

        #endregion

        #region system

        /// <summary>
        /// Executes a command and writes output line by line to the output sink set on the current script context.
        /// Flushes output after each written line.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>
        /// Either the last line of the output or a <B>null</B> reference if the command fails (returns non-zero exit code).
        /// </returns>
        [return: CastToFalse]
        public static string system(string command)
        {
            int exit_code;
            return system(command, out exit_code);
        }

        /// <summary>
        /// Executes a command and writes output line by line to the output sink set on the current script context.
        /// Flushes output after each written line.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="exitCode">An exit code of the process.</param>
        /// <returns>
        /// Either the last line of the output or a <B>null</B> reference if the command fails (returns non-zero exit code).
        /// </returns>
        [return: CastToFalse]
        public static string system(string command, out int exitCode)
        {
            string result;
            exitCode = Execution.ShellExec(command, Execution.OutputHandling.FlushLinesToScriptOutput, null, out result);
            return (exitCode == 0) ? result : null;
        }

        #endregion

        #region shell_exec

        public static string shell_exec(string command)
        {
            string result;
            Execution.ShellExec(command, Execution.OutputHandling.String, null, out result);
            return result;
        }

        #endregion

        #region getopt

        /// <summary>
        /// Gets options from the command line argument list.
        /// </summary>
        /// <param name="options">Each character in this string will be used as option characters and matched against options passed to the script starting with a single hyphen (-).   For example, an option string "x" recognizes an option -x.   Only a-z, A-Z and 0-9 are allowed. </param>
        /// <param name="longopts">An array of options. Each element in this array will be used as option strings and matched against options passed to the script starting with two hyphens (--).   For example, an longopts element "opt" recognizes an option --opt. </param>
        /// <returns>This function will return an array of option / argument pairs or FALSE  on failure. </returns>
        [return: CastToFalse]
        public static PhpArray getopt(string options, PhpArray longopts = null)
        {
            var args = System.Environment.GetCommandLineArgs();
            var result = new PhpArray();

            // process single char options
            if (options != null)
                for (int i = 0; i < options.Length; ++i)
                {
                    char opt = options[i];
                    if (!char.IsLetterOrDigit(opt))
                        break;

                    int ncolons = 0;
                    if (i + 1 < options.Length && options[i + 1] == ':') { ++ncolons; ++i; }    // require value
                    if (i + 1 < options.Length && options[i + 1] == ':') { ++ncolons; ++i; }    // optional value

                    object value = ParseOption(opt.ToString(), false, ncolons == 1, ncolons == 2, args);
                    if (value != null)
                        result.Add(opt.ToString(), value);
                }

            // process long options
            if (longopts != null)
            {
                var enumerator = longopts.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    string str = enumerator.CurrentValue.ToStringOrNull();
                    if (str == null) continue;

                    int ncolons = 0;
                    if (str.EndsWith(":")) ncolons = (str.EndsWith("::")) ? 2 : 1;
                    str = str.Substring(0, str.Length - ncolons);// remove colons

                    object value = ParseOption(str, true, ncolons == 1, ncolons == 2, args);
                    if (value != null)
                        result.Add(str, value);
                }
            }

            return result;
        }

        static object ParseOption(string option, bool longOpt, bool valueRequired, bool valueOptional, string[] args)
        {
            string prefix = (longOpt ? "--" : "-") + option;
            bool noValue = (!valueOptional && !valueRequired);

            // find matching arg
            for (int a = 1; a < args.Length; ++a)
            {
                string arg = args[a];
                if (arg.StartsWith(prefix))
                {
                    if (noValue)
                    {
                        if (arg.Length == prefix.Length) return false;   // OK, no value
                        if (longOpt) continue; // try another arg
                        return null;    // invalid arg
                    }

                    // value is optional or required
                    // try value after the prefix
                    string value = arg.Substring(prefix.Length);

                    if (value.Length > 0)
                    {
                        bool eq = (value[0] == '=');    // '=' follows
                        if (longOpt && !eq) continue;    // long options can have value only after =
                        if (eq) value = value.Substring(1); // remove the '=' char
                        return value;   // value resolved (optional or required)
                    }

                    if (valueOptional) return false;

                    // value required
                    if (a + 1 >= args.Length) return null;  // missing value
                    return args[a + 1];
                }
            }

            // not found
            return null;
        }

        #endregion
    }

    /// <summary>
	/// Provides functionality related to process execution.
	/// </summary>
	internal static class Execution
    {
        /// <summary>
        /// How to handle external process output.
        /// </summary>
        public enum OutputHandling
        {
            /// <summary>
            /// Split the result into lines and add them to the specified collection.
            /// </summary>
            ArrayOfLines,

            /// <summary>
            /// Return entire output as a string.
            /// </summary>
            String,

            /// <summary>
            /// Write each line to the current output and flush the output after each line.
            /// </summary>
            FlushLinesToScriptOutput,

            /// <summary>
            /// Redirect all output to binary sink of the current output.
            /// </summary>
            RedirectToScriptOutput
        }

        /// <summary>
        /// Executes a <c>cmd.exe</c> and passes it a specified command.
        /// </summary>
        /// <param name="command">The command to be passed.</param>
        /// <returns>A string containing the entire output.</returns>
        /// <remarks>Implements backticks operator (i.e. <code>`command`</code>).</remarks>
        public static string ShellExec(string command)
        {
            string result;
            ShellExec(command, OutputHandling.String, null, out result);
            return result;
        }


        /// <summary>
        /// Executes a <c>cmd.exe</c> and passes it a specified command.
        /// </summary>
        /// <param name="command">The command to be passed.</param>
        /// <param name="handling">How to handle the output.</param>
        /// <param name="arrayOutput">
        /// A list where output lines will be added if <paramref name="handling"/> is <see cref="OutputHandling.ArrayOfLines"/>.
        /// </param>
        /// <param name="stringOutput">
        /// A string containing the entire output in if <paramref name="handling"/> is <see cref="OutputHandling.String"/>
        /// or the last line of the output if <paramref name="handling"/> is <see cref="OutputHandling.ArrayOfLines"/> or
        /// <see cref="OutputHandling.FlushLinesToScriptOutput"/>. 
        /// </param>
        /// <returns>Exit code of the process.</returns>
        public static int ShellExec(string command, OutputHandling handling, PhpArray arrayOutput, out string stringOutput)
        {
            if (!MakeCommandSafe(ref command))
            {
                stringOutput = "";
                return -1;
            }

            //using (Process p = new Process())
            //{
            //    IdentitySection identityConfig = null;

            //    try { identityConfig = WebConfigurationManager.GetSection("system.web/identity") as IdentitySection; }
            //    catch { }

            //    if (identityConfig != null)
            //    {
            //        p.StartInfo.UserName = identityConfig.UserName;
            //        if (identityConfig.Password != null)
            //        {
            //            p.StartInfo.Password = new SecureString();
            //            foreach (char c in identityConfig.Password) p.StartInfo.Password.AppendChar(c);
            //            p.StartInfo.Password.MakeReadOnly();
            //        }
            //    }

            //    p.StartInfo.FileName = "cmd.exe";
            //    p.StartInfo.Arguments = "/c " + command;
            //    p.StartInfo.UseShellExecute = false;
            //    p.StartInfo.CreateNoWindow = true;
            //    p.StartInfo.RedirectStandardOutput = true;
            //    p.Start();

            //    stringOutput = null;
            //    switch (handling)
            //    {
            //        case OutputHandling.String:
            //            stringOutput = p.StandardOutput.ReadToEnd();
            //            break;

            //        case OutputHandling.ArrayOfLines:
            //            {
            //                string line;
            //                while ((line = p.StandardOutput.ReadLine()) != null)
            //                {
            //                    stringOutput = line;
            //                    if (arrayOutput != null) arrayOutput.Add(line);
            //                }
            //                break;
            //            }

            //        case OutputHandling.FlushLinesToScriptOutput:
            //            {
            //                ScriptContext context = ScriptContext.CurrentContext;

            //                string line;
            //                while ((line = p.StandardOutput.ReadLine()) != null)
            //                {
            //                    stringOutput = line;
            //                    context.Output.WriteLine(line);
            //                    context.Output.Flush();
            //                }
            //                break;
            //            }

            //        case OutputHandling.RedirectToScriptOutput:
            //            {
            //                ScriptContext context = ScriptContext.CurrentContext;

            //                byte[] buffer = new byte[1024];
            //                int count;
            //                while ((count = p.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
            //                {
            //                    context.OutputStream.Write(buffer, 0, count);
            //                }
            //                break;
            //            }
            //    }

            //    p.WaitForExit();

            //    return p.ExitCode;
            //}

            throw new NotImplementedException();
        }

        /// <summary>
        /// Escape shell metacharacters in a specified shell command.
        /// </summary>
        /// <param name="command">The command to excape.</param>
        /// <para>
        /// On Windows platform, each occurance of a character that might be used to trick a shell command
        /// is replaced with space. These characters are 
        /// <c>", ', #, &amp;, ;, `, |, *, ?, ~, &lt;, &gt;, ^, (, ), [, ], {, }, $, \, \u000A, \u00FF, %</c>.
        /// </para>
        internal static string EscapeCommand(string command)
        {
            if (command == null) return String.Empty;

            StringBuilder sb = new StringBuilder(command);

            // GENERICS:
            //			if (Environment.OSVersion.Platform!=PlatformID.Unix)
            {
                for (int i = 0; i < sb.Length; i++)
                {
                    switch (sb[i])
                    {
                        case '"':
                        case '\'':
                        case '#':
                        case '&':
                        case ';':
                        case '`':
                        case '|':
                        case '*':
                        case '?':
                        case '~':
                        case '<':
                        case '>':
                        case '^':
                        case '(':
                        case ')':
                        case '[':
                        case ']':
                        case '{':
                        case '}':
                        case '$':
                        case '\\':
                        case '\u000A':
                        case '\u00FF':
                        case '%':
                            sb[i] = ' ';
                            break;
                    }
                }
            }
            //      else
            //      {
            //        // ???
            //        PhpException.FunctionNotSupported();
            //      } 

            return sb.ToString();
        }

        /// <summary>
        /// Makes command safe in similar way PHP does.
        /// </summary>
        /// <param name="command">Potentially unsafe command.</param>
        /// <returns>Safe command.</returns>
        /// <remarks>
        /// If safe mode is enabled, command is split by the first space into target path 
        /// and arguments (optionally) components. The target path must not contain '..' substring.
        /// A file name is extracted from the target path and combined with 
        /// <see cref="GlobalConfiguration.SafeModeSection.ExecutionDirectory"/>.
        /// The resulting path is checked for invalid path characters (Phalanger specific).
        /// Finally, arguments are escaped by <see cref="EscapeCommand"/> and appended to the path.
        /// If safe mode is disabled, the command remains unchanged.
        /// </remarks>
        internal static bool MakeCommandSafe(ref string command)
        {
            //if (command == null) return false;
            //GlobalConfiguration global = Configuration.Global;

            //if (!global.SafeMode.Enabled) return true;

            //int first_space = command.IndexOf(' ');
            //if (first_space == -1) first_space = command.Length;

            //if (command.IndexOf("..", 0, first_space) >= 0)
            //{
            //    PhpException.Throw(PhpError.Warning, "dotdot_not_allowed_in_path");
            //    return false;
            //}

            //try
            //{
            //    string file_name = Path.GetFileName(command.Substring(0, first_space));
            //    string target_path = Path.Combine(global.SafeMode.ExecutionDirectory, file_name);

            //    // <execution directory>/<file name> <escaped arguments>
            //    command = String.Concat(target_path, EscapeCommand(command.Substring(first_space)));
            //}
            //catch (ArgumentException)
            //{
            //    PhpException.Throw(PhpError.Warning, "path_contains_invalid_characters");
            //    return false;
            //}

            return true;
        }
    }
}
