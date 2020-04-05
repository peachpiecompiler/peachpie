using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Represents a stack trace in context of PHP application.
    /// </summary>
    /// <remarks>PHP stack trace differs from CLR stack trace in several ways:
    /// - global code shown as <c>{main}</c>
    /// - there are CLR frames that we don't want to expose in PHP (mostly annotated with [DebuggerNonUserCode])
    /// - frame location specifies the called function location</remarks>
    [DebuggerNonUserCode]
    public sealed class PhpStackTrace
    {
        /// <summary>
        /// Helper class representing a single PHP stack frame information.
        /// </summary>
        public sealed class FrameLine
        {
            /// <summary>
            /// the frame with location.
            /// </summary>
            readonly PhpStackFrame _locationFrame;

            /// <summary>
            /// The frame with called routine.
            /// </summary>
            readonly PhpStackFrame _calledFrame;

            public bool HasLocation => _locationFrame != null && _locationFrame.HasLocation;

            internal FrameLine(PhpStackFrame locationFrame, PhpStackFrame calledFrame)
            {
                _locationFrame = locationFrame;
                _calledFrame = calledFrame;
            }

            public PhpArray ToUserFrame()
            {
                var item = new PhpArray();

                if (_locationFrame != null && _locationFrame.HasLocation)
                {
                    //file    string The current file name.See also __FILE__.
                    item.Add("file", _locationFrame.FileName);

                    //line integer The current line number. See also __LINE__.
                    item.Add("line", _locationFrame.Line);
                    item.Add("column", _locationFrame.Column);
                }

                if (_calledFrame != null)
                {
                    //function    string The current function name.See also __FUNCTION__.
                    item.Add("function", _calledFrame.RoutineName);

                    var tname = _calledFrame.TypeName;
                    if (tname != null)
                    {
                        //class   string The current class name. See also __CLASS__
                        item.Add("class", tname);
                        //object object The current object.
                        //type string The current call type.If a method call, "->" is returned.If a static method call, "::" is returned.If a function call, nothing is returned.
                        item.Add("type", _calledFrame.MethodOperator);
                    }
                }

                //args array   If inside a function, this lists the functions arguments.If inside an included file, this lists the included file name(s).

                return item;
            }

            public string ToStackTraceLine(int order)
            {
                var result = new StringBuilder();

                if (order >= 0)
                {
                    result.Append('#');
                    result.Append(order);
                    result.Append(' ');
                }

                if (HasLocation)
                {
                    result.AppendFormat("{0}({1},{2})", _locationFrame.FileName, _locationFrame.Line, _locationFrame.Column);
                }

                if (_calledFrame != null)
                {
                    result.Append(": ");
                    result.Append(_calledFrame.RoutineFullName);
                    result.Append(_calledFrame.RoutineParameters);
                }

                return result.ToString();
            }

            public override string ToString() => ToStackTraceLine(-1);
        }

        readonly PhpStackFrame[] _frames;

        [DebuggerNonUserCode]
        public PhpStackTrace()
            : this(new StackTrace(true))
        {
            Debug.Assert(_frames != null);
        }

        public PhpStackTrace(StackTrace/*!*/clrtrace)
        {
            // collect stack trace if possible:
            _frames = InitPhpStackFrames(clrtrace);
        }

        static PhpStackFrame[] InitPhpStackFrames(StackTrace/*!*/clrtrace)
        {
            return clrtrace.GetFrames()
                .Where(IsPhpStackFrame)
                .Select(clrframe => new PhpStackFrame(clrframe))
                .ToArray();
        }

        static bool IsPhpStackFrame(StackFrame frame)
        {
            var method = frame.GetMethod();
            if (method == null || method.DeclaringType == null)
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

            // trait implementation
            if (ReflectionUtils.IsTraitType(tinfo))
            {
                return false;
            }

            // in Peachpie assemblies (runtime, libraries) // implicitly NonUserCode
            var ass = tinfo.Assembly;
            var token = ass.GetName().GetPublicKeyToken();
            if (token != null)
            {
                var tokenkey = Utilities.StringUtils.BinToHex(token);
                if (tokenkey == ReflectionUtils.PeachpieAssemblyTokenKey)
                {
                    // but allow library functions
                    if (method.IsPublic && method.IsStatic && tinfo.IsPublic && tinfo.IsAbstract) // public static class + public static method
                    {
                        // ok
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (tokenkey == "b77a5c561934e089" || tokenkey == "b03f5f7f11d50a3a" || tokenkey == "7cec85d7bea7798e")    // System
                {
                    return false;
                }
            }

            // [DebuggerHiddenAttribute]
            // [PhpHiddenAttribute]
            // [CompilerGeneratedAttribute]
            // [DebuggerNonUserCodeAttribute]
            if (method.GetCustomAttribute<DebuggerNonUserCodeAttribute>() != null ||
                method.GetCustomAttribute<DebuggerHiddenAttribute>() != null ||
                method.GetCustomAttribute<PhpHiddenAttribute>() != null ||
                method.GetCustomAttribute<CompilerGeneratedAttribute>() != null ||
                tinfo.GetCustomAttribute<DebuggerNonUserCodeAttribute>() != null ||
                tinfo.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
            {
                return false;
            }

            //
            return true;
        }

        public string GetFilename()
        {
            var f = UserStackFrame;
            return (f != null && f.HasLocation) ? f.FileName : string.Empty;
        }

        public int GetLine()
        {
            var f = UserStackFrame;
            return (f != null && f.HasLocation) ? f.Line : 0;
        }

        public int GetColumn()
        {
            var f = UserStackFrame;
            return (f != null && f.HasLocation) ? f.Column : 0;
        }

        /// <summary>
        /// Gets first non-core-assembly stack frame or <c>null</c>.
        /// </summary>
        PhpStackFrame UserStackFrame
        {
            get
            {
                if (_frames != null)
                {
                    for (int i = 0; i < _frames.Length; i++)
                    {
                        if (!_frames[i].IsCoreAssembly)
                        {
                            return _frames[i];
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Stack trace text formatted as PHP stack trace.
        /// </summary>
        public string StackTrace
        {
            get
            {
                var lines = this.GetLines();
                var result = new StringBuilder();

                for (int i = 0; i < lines.Length; i++)
                {
                    result.AppendLine(lines[i].ToStackTraceLine(i - 1));
                }

                return result.ToString();
            }
        }

        public override string ToString() => StackTrace;

        public FrameLine[] GetLines()
        {
            var frames = _frames;
            var lines = new FrameLine[frames.Length + 1];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = new FrameLine(
                    locationFrame: (i < frames.Length) ? frames[i] : null,
                    calledFrame: (i > 0) ? frames[i - 1] : null);
            }

            return lines;
        }
    }

    internal sealed class PhpStackFrame
    {
        public const string GlobalCodeName = "{main}";
        const string UnknownFile = "<unknown>";

        readonly StackFrame _clrframe;

        /// <summary>
        /// Gets file path where the frame points to.
        /// Can be <c>null</c>.
        /// </summary>
        public string FileName => _clrframe.GetFileName();

        public bool HasLocation => FileName != null;

        public int Line => _clrframe.GetFileLineNumber();

        public int Column => _clrframe.GetFileColumnNumber() - 1;

        public string RoutineName
        {
            get
            {
                var method = _clrframe.GetMethod();
                if (method.IsStatic)
                {
                    if (method.Name == ReflectionUtils.GlobalCodeMethodName)
                    {
                        // global code
                        return GlobalCodeName;
                    }
                }

                return method.Name;
            }
        }

        public string TypeName
        {
            get
            {
                var method = _clrframe.GetMethod();
                var tinfo = method.DeclaringType.GetTypeInfo();

                if (method.IsStatic)
                {
                    if (method.Name == ReflectionUtils.GlobalCodeMethodName)
                    {
                        // global code
                        return null;
                    }

                    if (ReflectionUtils.GetScriptAttribute(tinfo) != null)
                    {
                        // global function
                        return null;
                    }

                    if (tinfo.IsPublic && tinfo.IsAbstract) // => public static
                    {
                        if (tinfo.Assembly.IsDefined(typeof(PhpExtensionAttribute)))
                        {
                            // library function
                            return null;
                        }
                    }
                }

                return tinfo.Name;
            }
        }

        public string MethodOperator => _clrframe.GetMethod().IsStatic ? "::" : "->";

        public string RoutineFullName
        {
            get
            {
                var typename = TypeName;
                if (typename != null)
                {
                    var method = _clrframe.GetMethod();
                    return string.Concat(typename, MethodOperator, method.Name);
                }
                else
                {
                    return RoutineName;
                }
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

        /// <summary>
        /// Determines if the stack frame corresponds to the core assembly.
        /// </summary>
        public bool IsCoreAssembly
        {
            get
            {
                var method = _clrframe.GetMethod();
                if (method != null)
                {
                    var token = method.DeclaringType.Assembly.GetName().GetPublicKeyToken();
                    if (token != null)
                    {
                        var tokenkey = Utilities.StringUtils.BinToHex(token);
                        return (tokenkey == ReflectionUtils.PeachpieAssemblyTokenKey);
                    }
                }

                return false;
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
