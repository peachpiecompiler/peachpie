#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    [PhpExtension("standard")]
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
        public static string? getenv(Context ctx, string name)
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
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>The last line of the output.</returns>
        public static string? exec(Context ctx, string command)
        {
            Execution.ShellExec(ctx, command, Execution.OutputHandling.ArrayOfLines, null, out var result);
            return result;
        }

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="output">An array where to add items of output. One item per each line of the output.</param>
        /// <returns>The last line of the output.</returns>
        public static string? exec(Context ctx, string command, ref PhpArray output) => exec(ctx, command, ref output, out _);

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="output">An array where to add items of output. One item per each line of the output.</param>
        /// <param name="exitCode">Exit code of the process.</param>
        /// <returns>The last line of the output.</returns>
        public static string? exec(Context ctx, string command, ref PhpArray output, out int exitCode)
        {
            // creates a new array if user specified variable not containing one:
            if (output == null)
            {
                output = new PhpArray();
            }

            exitCode = Execution.ShellExec(ctx, command, Execution.OutputHandling.ArrayOfLines, output, out var result);

            return result;
        }

        #endregion

        #region pasthru

        /// <summary>
        /// Executes a command and writes raw output to the output sink set on the current script context.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command.</param>
        public static void passthru(Context ctx, string command)
        {
            Execution.ShellExec(ctx, command, Execution.OutputHandling.RedirectToScriptOutput, null, out var _);
        }

        /// <summary>
        /// Executes a command and writes raw output to the output sink set on the current script context.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command.</param>
        /// <param name="exitCode">An exit code of the process.</param>
        public static void passthru(Context ctx, string command, out int exitCode)
        {
            exitCode = Execution.ShellExec(ctx, command, Execution.OutputHandling.RedirectToScriptOutput, null, out var _);
        }

        #endregion

        #region system

        /// <summary>
        /// Executes a command and writes output line by line to the output sink set on the current script context.
        /// Flushes output after each written line.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command.</param>
        /// <returns>
        /// Either the last line of the output or a <B>null</B> reference if the command fails (returns non-zero exit code).
        /// </returns>
        [return: CastToFalse]
        public static string? system(Context ctx, string command)
        {
            return system(ctx, command, out _);
        }

        /// <summary>
        /// Executes a command and writes output line by line to the output sink set on the current script context.
        /// Flushes output after each written line.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="command">The command.</param>
        /// <param name="exitCode">An exit code of the process.</param>
        /// <returns>
        /// Either the last line of the output or a <B>null</B> reference if the command fails (returns non-zero exit code).
        /// </returns>
        [return: CastToFalse]
        public static string? system(Context ctx, string command, out int exitCode)
        {
            exitCode = Execution.ShellExec(ctx, command, Execution.OutputHandling.FlushLinesToScriptOutput, null, out var result);
            return (exitCode == 0) ? result : null;
        }

        #endregion

        #region shell_exec

        public static string? shell_exec(Context ctx, string command)
        {
            Execution.ShellExec(ctx, command, Execution.OutputHandling.String, null, out var result);
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
        public static PhpArray getopt(string options, PhpArray? longopts = null)
        {
            // TODO: return FALSE on failure

            var args = System.Environment.GetCommandLineArgs();
            var result = new PhpArray();

            // process single char options
            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    char opt = options[i];
                    if (!char.IsLetterOrDigit(opt))
                        break;

                    var option = opt.ToString();

                    int ncolons = 0;
                    while (i + 1 < options.Length && options[i + 1] == ':') { ncolons++; i++; }

                    var value = ParseOption(option, false, ncolons == 1, ncolons > 1, args);
                    if (value != null)
                    {
                        var key = char.IsDigit(opt) ? new IntStringKey(opt) : new IntStringKey(option);
                        result[key] = value;
                    }
                }
            }

            // process long options
            if (longopts != null)
            {
                var enumerator = longopts.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.CurrentValue.TryToIntStringKey(out var key))
                    {
                        var option = key.ToString();
                        int ncolons = 0;

                        if (option.LastChar() == ':')
                        {
                            ncolons = option.EndsWith("::", StringComparison.Ordinal) ? 2 : 1;
                            option = option.Substring(0, option.Length - ncolons);// remove colons
                            key = Core.Convert.StringToArrayKey(option);
                        }

                        var value = ParseOption(option, true, ncolons == 1, ncolons == 2, args);
                        if (value != null)
                        {
                            result[key] = value;
                        }
                    }
                }
            }

            return result;
        }

        static string? ParseOption(string option, bool longOpt, bool valueRequired, bool valueOptional, string[] args)
        {
            string prefix = (longOpt ? "--" : "-") + option;
            bool noValue = (!valueOptional && !valueRequired);

            // find matching arg
            for (int a = 1; a < args.Length; ++a)
            {
                var arg = args[a];
                if (arg.StartsWith(prefix, StringComparison.InvariantCulture))
                {
                    if (noValue)
                    {
                        if (arg.Length == prefix.Length) return string.Empty;   // OK, no value
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

                    if (valueOptional) return string.Empty;

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

        const char Quote = '"';
        const char Backslash = '\\';

        /// <summary>Checks the string does not contains a whitespaces or a double quote.</summary>
        static bool NoWhitespacesNorQuotes(string str)
        {
            foreach (char c in str)
            {
                if (char.IsWhiteSpace(c) || c == Quote)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Safely appends an argument. Encloses in quotes if necessary.</summary>
        static void AppendArgument(StringBuilder stringBuilder, string argument)
        {
            if (stringBuilder.Length != 0)
            {
                stringBuilder.Append(' ');
            }

            if (argument.Length != 0 && NoWhitespacesNorQuotes(argument))
            {
                stringBuilder.Append(argument);
                return;
            }

            stringBuilder.Append(Quote);

            int index = 0;
            while (index < argument.Length)
            {
                var c = argument[index++];
                switch (c)
                {
                    case Backslash:
                        {
                            int slashes = 1;
                            while (index < argument.Length && argument[index] == '\\')
                            {
                                index++;
                                slashes++;
                            }
                            if (index == argument.Length)
                            {
                                stringBuilder.Append(Backslash, slashes * 2);
                            }
                            else if (argument[index] == Quote)
                            {
                                stringBuilder.Append(Backslash, slashes * 2 + 1);
                                stringBuilder.Append(Quote);
                                index++;
                            }
                            else
                            {
                                stringBuilder.Append(Backslash, slashes);
                            }
                            break;
                        }

                    case Quote:
                        stringBuilder.Append(Backslash);
                        stringBuilder.Append(Quote);
                        break;

                    default:
                        stringBuilder.Append(c);
                        break;
                }
            }
            stringBuilder.Append(Quote);
        }

        /// <summary>
        /// Executes a <c>cmd.exe</c> and passes it a specified command.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
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
        public static int ShellExec(Context ctx, string command, OutputHandling handling, PhpArray? arrayOutput, out string? stringOutput)
        {
            if (!MakeCommandSafe(ref command))
            {
                stringOutput = null;
                return -1;
            }

            using (var p = new Process())
            {
                //IdentitySection identityConfig = null;

                //try { identityConfig = WebConfigurationManager.GetSection("system.web/identity") as IdentitySection; }
                //catch { }

                //if (identityConfig != null)
                //{
                //    p.StartInfo.UserName = identityConfig.UserName;
                //    if (identityConfig.Password != null)
                //    {
                //        p.StartInfo.Password = new SecureString();
                //        foreach (char c in identityConfig.Password) p.StartInfo.Password.AppendChar(c);
                //        p.StartInfo.Password.MakeReadOnly();
                //    }
                //}

                // prepare arguments
                {
                    var arguments = StringBuilderUtilities.Pool.Get();

                    if (CurrentPlatform.IsWindows)
                    {
                        p.StartInfo.FileName = "cmd.exe";
                        AppendArgument(arguments, "/c");
                    }
                    else
                    {
                        p.StartInfo.FileName = "/bin/bash";
                        AppendArgument(arguments, "-c");
                    }

                    AppendArgument(arguments, command);

                    p.StartInfo.Arguments = StringBuilderUtilities.GetStringAndReturn(arguments);
                }

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();

                stringOutput = null;
                switch (handling)
                {
                    case OutputHandling.String:
                        stringOutput = p.StandardOutput.ReadToEnd();
                        break;

                    case OutputHandling.ArrayOfLines:
                        {
                            string line;
                            while ((line = p.StandardOutput.ReadLine()) != null)
                            {
                                arrayOutput?.Add(line);
                                stringOutput = line;
                            }
                            break;
                        }

                    case OutputHandling.FlushLinesToScriptOutput:
                        {
                            string line;
                            while ((line = p.StandardOutput.ReadLine()) != null)
                            {
                                stringOutput = line;
                                ctx.Output.WriteLine(line);
                                ctx.Output.Flush();
                            }
                            break;
                        }

                    case OutputHandling.RedirectToScriptOutput:
                        p.StandardOutput.BaseStream
                            .CopyToAsync(ctx.OutputStream)
                            .GetAwaiter()
                            .GetResult();

                        break;
                }

                p.WaitForExit();

                //
                return p.ExitCode;
            }
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
            if (string.IsNullOrEmpty(command))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(command);

            // GENERICS:
            if (CurrentPlatform.IsWindows)
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
            else
            {
                // ???
                PhpException.FunctionNotSupported(nameof(Shell.escapeshellcmd));
            }

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
        /// The resulting path is checked for invalid path characters (dotnet specific).
        /// Finally, arguments are escaped by <see cref="EscapeCommand"/> and appended to the path.
        /// If safe mode is disabled, the command remains unchanged.
        /// </remarks>
        internal static bool MakeCommandSafe(ref string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            //GlobalConfiguration global = Configuration.Global;

            //if (!global.SafeMode.Enabled) return true;

            int first_space = command.IndexOf(' ');
            if (first_space < 0) first_space = command.Length;

            if (command.IndexOf("..", 0, first_space) >= 0)
            {
                PhpException.Throw(PhpError.Warning, "dotdot_not_allowed_in_path");
                return false;
            }

            //try
            //{
            //    string file_name = Path.GetFileName(command.Substring(0, first_space));
            //    string target_path = Path.Combine(global.SafeMode.ExecutionDirectory, file_name);

            //    // <execution directory>/<file name> <escaped arguments>
            //    command = string.Concat(target_path, EscapeCommand(command.Substring(first_space)));
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
