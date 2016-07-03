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
            _textSink = new StreamWriter(_streamSink = output ?? Stream.Null);
            IsOutputBuffered = false;
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
            => _bufferedOutput ?? (_bufferedOutput = new BufferedOutput(enableBuffering, _textSink, _streamSink, this.StringEncoding));

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
        /// Encoding used to convert between unicode strings and binary strings.
        /// </summary>
        public virtual Encoding StringEncoding => Encoding.UTF8;

        /// <summary>
        /// Flushes all remaining data from output buffers.
        /// </summary>
        internal void FinalizeBufferedOutput()
        {
            // flushes output, applies user defined output filter, and disables buffering:
            if (_bufferedOutput != null)
                _bufferedOutput.FlushAll();

            // redirects sinks:
            IsOutputBuffered = false;
            Output.Flush();
        }

        #endregion

        #region Echo

        public void Echo(object value)
        {
            if (value != null)
                Echo(value.ToString());
        }

        public void Echo(string value)
        {
            if (value != null)
                Output.Write(value);
        }

        public void Echo(PhpString value)
        {
            Echo(value.ToString(this));    // TODO: echo string builder chunks to avoid concatenation
        }

        public void Echo(PhpValue value)
        {
            Output.Write(value.ToString(this)); // TODO: echo byte[] properly
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
            Output.Write(Convert.ToString(value, this));
        }

        public void Echo(long value)
        {
            Output.Write(value);
        }

        public void Echo(int value)
        {
            Output.Write(value);
        }

        #endregion
    }
}
