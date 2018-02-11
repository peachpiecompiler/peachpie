using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Represents a stack trace in context of PHP application.
    /// </summary>
    /// <remarks>PHP stack trace differs from CLR stack trace in several ways:
    /// - global code shows as <c>{main}</c>
    /// - there are CLR frames that we don't want to expose in PHP (mostly annotated with [DebuggerNonUserCode])</remarks>
    [DebuggerNonUserCode]
    public sealed class PhpStackTrace
    {
        PhpStackFrame[] _frames;

        [DebuggerNonUserCode]
        public PhpStackTrace()
        {
#if NET46
            InitPhpStackFrames(new StackTrace());
#else
            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                InitPhpStackFrames(new StackTrace(ex, true));
            }
#endif
            Debug.Assert(_frames != null);
        }

        internal PhpStackTrace(StackTrace clrtrace)
        {
            InitPhpStackFrames(clrtrace);
        }

        void InitPhpStackFrames(StackTrace clrtrace)
        {
            _frames = clrtrace.GetFrames()
                .Where(IsPhpStackFrame)
                .Select(clrframe => new PhpStackFrame(clrframe))
                .ToArray();
        }

        static bool IsPhpStackFrame(StackFrame frame)
        {
            var method = frame.GetMethod();
            if (method == null)
            {
                return false;
            }

            // weird method names:
            if (!ReflectionUtils.IsAllowedPhpName(method.Name) && method.Name != ReflectionUtils.GlobalCodeMethodName)
            {
                return false;
            }

            // <Script> type
            var tinfo = method.DeclaringType.GetTypeInfo();
            if (tinfo.Name == "<Script>")
            {
                return false;
            }

            // in Peachpie assemblies (runtime, libraries) // implicitly NonUserCode
            var ass = tinfo.Assembly;
            var token = ass.GetName().GetPublicKeyToken();
            if (token != null && Utilities.StringUtils.BinToHex(token) == ReflectionUtils.PeachpieAssemblyTokenKey)
            {
                return false;
            }
            
            // [DebuggerNonUserCodeAttribute] or [DebuggerHiddenAttribute]
            if (method.GetCustomAttribute<DebuggerNonUserCodeAttribute>() != null || method.GetCustomAttribute<DebuggerHiddenAttribute>() != null ||
                tinfo.GetCustomAttribute<DebuggerNonUserCodeAttribute>() != null)
            {
                return false;
            }

            //
            return true;
        }

        /// <summary>
        /// Gets stack trace string in form of "[filename]:[pos]\nStack trace:\n#0 [filename](pos): routine\n..."
        /// </summary>
        /// <returns></returns>
        public string AsPhpExceptionTrace()
        {
            // frame position is reported on previous frame in PHP:

            var frames = _frames;
            if (frames.Length == 0)
            {
                return string.Empty;
            }

            // 

            var result = new StringBuilder();

            PhpStackFrame f0;

            for (int i = 0; i < frames.Length; i++)
            {
                var f = frames[i];

                if (i != 0)
                {
                    if (i == 1)
                    {
                        result.Append("Stack trace:\n");
                    }

                    // #order [filename](pos): routine // of previous routine
                    f0 = frames[i - 1];
                    result.AppendFormat("#{0} {1}({2},{3}): {4}{5}\n", i, f.FileName, f.Line, f.Column, f0.RoutineName, f0.RoutineParameters);
                }
                else
                {
                    // [filename]:pos
                    result.AppendFormat("{0}({1},{2})\n", f.FileName, f.Line, f.Column);
                }
            }

            f0 = frames[frames.Length - 1];
            result.AppendFormat("#{0} {1}{2}\n", frames.Length, f0.RoutineName, f0.RoutineParameters);

            //
            return result.ToString();
        }
    }

    public sealed class PhpStackFrame
    {
        const string GlobalCodeName = "{main}";
        const string UnknownFile = "<unknown>";

        readonly StackFrame _clrframe;

        /// <summary>
        /// Gets file path where the frame points to.
        /// Can be <c>null</c>.
        /// </summary>
        public string FileName => _clrframe.GetFileName();

        public int Line => _clrframe.GetFileLineNumber();

        public int Column => _clrframe.GetFileColumnNumber();

        public string RoutineName
        {
            get
            {
                var method = _clrframe.GetMethod();
                var phpt = method.DeclaringType.GetTypeInfo();

                if (method.IsStatic)
                {
                    if (method.Name == ReflectionUtils.GlobalCodeMethodName)
                    {
                        // global code
                        return GlobalCodeName;
                    }

                    if (phpt.GetCustomAttribute<ScriptAttribute>() != null)
                    {
                        // global function
                        return method.Name;
                    }
                }

                return string.Concat(
                    phpt.Name,
                    method.IsStatic ? "::" : "->",
                    method.Name);
            }
        }

        public string RoutineParameters
        {
            get
            {
                if (_clrframe.GetMethod().Name == ReflectionUtils.GlobalCodeMethodName)
                {
                    return string.Empty;
                }

                // note: parameter values not supported, user should use debugger
                var str = new StringBuilder();

                str.Append('(');

                bool first = true;

                foreach (var p in _clrframe.GetMethod().GetParameters())
                {
                    if (first && ReflectionUtils.IsImplicitParameter(p))
                    {
                        continue;
                    }

                    if (!first)
                    {
                        str.Append(", ");
                    }
                    else
                    {
                        first = false;
                    }

                    str.Append('$');
                    str.Append(p.Name);
                }

                str.Append(')');

                return str.ToString();
            }
        }

        public PhpStackFrame(StackFrame clrframe)
        {
            _clrframe = clrframe;
        }

        public override string ToString()
        {
            return string.Format("{0}:({1},{2}): {3}{4}",
                FileName ?? "<unknown>",
                Line, Column,
                RoutineName,
                RoutineParameters);
        }
    }
}
