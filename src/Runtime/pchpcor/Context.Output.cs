using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        #region Output Buffering

        /// <summary>
        /// Initialize output of this context.
        /// To be used by the context constructor.
        /// </summary>
        /// <param name="output"></param>
        protected void InitOutput(Stream output)
        {
            // setups Output and OutputStream
            this.Output = new StreamWriter(this.OutputStream = output ?? Stream.Null);
        }

        /// <summary>
        /// Textual sink where buffered unicode output is flushed.
        /// </summary>
        TextWriter _textSink;

        /// <summary>
        /// Byte stream sink where buffered binary output is flushed.
        /// </summary>
        Stream _streamSink;

        /// <summary>
        /// Buffered output associated with the context.
        /// </summary>
        public BufferedOutput/*!*/BufferedOutput => EnsureBufferedOutput(false);    // Initialize lazily as not buffered output by default.
        
        /// <remarks>Is <c>null</c> reference until it is not used for the first time.</remarks>
        BufferedOutput _bufferedOutput;

        BufferedOutput EnsureBufferedOutput(bool enableBuffering)
            => _bufferedOutput ?? (_bufferedOutput = new BufferedOutput(enableBuffering, _textSink, _streamSink, this.OutputEncoding));

        /// <summary>
        /// Stream where text output will be sent.
        /// </summary>
        public TextWriter Output
        {
            get
            {
                return _output;
            }
            set
            {
                _textSink = value;

                if (_bufferedOutput != null)
                    _bufferedOutput.CharSink = value;

                if (!IsOutputBuffered)
                    _output = value;
            }
        }
        TextWriter _output;

        /// <summary>
        /// Stream where binary output will be sent.
        /// </summary>
        public Stream OutputStream
        {
            get
            {
                return _binaryOutput;
            }
            set
            {
                _streamSink = value;

                if (_bufferedOutput != null)
                    _bufferedOutput.ByteSink = value;

                if (_bufferedOutput == null || _binaryOutput != _bufferedOutput.Stream)        // if output is not buffered
                    _binaryOutput = value;
            }
        }
        internal Stream _binaryOutput;

        /// <summary>
        /// Specifies whether script output is passed through <see cref="BufferedOutput"/>.
        /// </summary>
        public bool IsOutputBuffered
        {
            get
            {
                return _output == _bufferedOutput;
            }
            set
            {
                if (value)
                {
                    _output = EnsureBufferedOutput(true);
                    _binaryOutput = _bufferedOutput.Stream;
                }
                else
                {
                    _output = _textSink;
                    _binaryOutput = _streamSink;
                }
            }
        }

        /// <summary>
        /// Encoding used internally for byte array to string conversion.
        /// </summary>
        protected virtual Encoding OutputEncoding => Encoding.UTF8;

        #endregion

        // TOOD: CreateConsoleOutputFilter
        // TODO: echo using current output filter

        #region Echo

        public void Echo(object value)
        {
            if (value != null)
                Echo(value.ToString());
        }

        public void Echo(string value)
        {
            if (value != null)
                ConsoleImports.Write(value);
        }

        public void Echo(PhpString value)
        {
            Echo(value.ToString(this));    // TODO: echo string builder chunks to avoid concatenation
        }

        public void Echo(PhpValue value)
        {
            ConsoleImports.Write(value.ToString(this));
        }

        public void Echo(PhpNumber value)
        {
            if (value.IsLong)
                Echo(value.Long);
            else
                Echo(value.Double);
        }

        public void Echo(double value)
        {
            ConsoleImports.Write(Convert.ToString(value, this));
        }

        public void Echo(long value)
        {
            ConsoleImports.Write(value.ToString());
        }

        public void Echo(int value)
        {
            ConsoleImports.Write(value.ToString());
        }

        #endregion
    }
}
