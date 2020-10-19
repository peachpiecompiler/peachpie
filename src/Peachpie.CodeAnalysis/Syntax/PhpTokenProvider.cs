using System;
using System.Collections.Generic;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Text;
using Devsense.PHP.Utilities;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis;
using Peachpie.CodeAnalysis.Utilities;
using TSpan = Devsense.PHP.Text.Span;
using TValue = Devsense.PHP.Syntax.SemanticValueType;

namespace Peachpie.CodeAnalysis.Syntax
{
    /// <summary>
    /// Wrapping token provider that buffers and allows for token lookup.
    /// </summary>
    sealed class PhpTokenProvider : ITokenProvider<TValue, TSpan>, IDisposable
    {
        readonly ITokenProvider<TValue, TSpan> _provider;
        readonly PhpSourceUnit _sourceunit;

        StringTable _strings;
        PHPDocBlock _docblock;

        /// <summary>
        /// Buffered tokens.
        /// </summary>
        readonly List<CompleteToken> _buffer = new List<CompleteToken>();
        int _bufferidx = 0;

        public PhpTokenProvider(ITokenProvider<TValue, TSpan> provider, PhpSourceUnit sourceunit)
        {
            _provider = provider ?? throw ExceptionUtilities.ArgumentNull();
            _sourceunit = sourceunit ?? throw ExceptionUtilities.ArgumentNull();
            _strings = StringTable.GetInstance();
        }

        #region ITokenProvider

        public Tokens Token => (_bufferidx < _buffer.Count) ? _buffer[_bufferidx].Token : default;

        public TValue TokenValue
        {
            get => (_bufferidx < _buffer.Count) ? _buffer[_bufferidx].TokenValue : _provider.TokenValue;
            set
            {
                if (_bufferidx < _buffer.Count)
                {
                    var previous = _buffer[_bufferidx];
                    _buffer[_bufferidx] = new CompleteToken(previous.Token, value, previous.TokenPosition, previous.TokenText);
                }
                else
                {
                    _provider.TokenValue = value;
                }
            }
        }

        public TSpan TokenPosition => (_bufferidx < _buffer.Count) ? _buffer[_bufferidx].TokenPosition : _provider.TokenPosition;

        public string TokenText
        {
            get
            {
                if (_bufferidx < _buffer.Count)
                {
                    var tinfo = _buffer[_bufferidx];
                    if (tinfo.TokenText == null)
                    {
                        _buffer[_bufferidx] = tinfo = tinfo.WithTokenText(_strings.Add(_sourceunit.GetSourceCode(TokenPosition)));
                    }

                    return tinfo.TokenText;
                }
                else
                {
                    return _provider.TokenText;
                }
            }
        }

        public PHPDocBlock DocBlock
        {
            get => _docblock;
            set => _docblock = value;
        }

        public int GetNextToken()
        {
            // pop the previous token
            _bufferidx++;

            if (_bufferidx >= _buffer.Count)
            {
                _bufferidx = 0;
                _buffer.Clear();

                // filter more tokens
                BufferTokens();
            }

            //
            var token = _buffer[_bufferidx].Token;
            if (token == Tokens.T_DOC_COMMENT)
            {
                DocBlock = _provider.DocBlock;
            }

            //
            return (int)token;
        }

        void BufferTokens(int count = 1)
        {
            Tokens t;
            do
            {
                // fetch next token
                t = (Tokens)_provider.GetNextToken();

                // add to buffer
                _buffer.Add(new CompleteToken(t, _provider.TokenValue, _provider.TokenPosition, null));

            } while (t != Tokens.END && _buffer.Count < count);
        }

        void ITokenProvider<TValue, TSpan>.ReportError(string[] expectedTokens) => _provider.ReportError(expectedTokens);

        #endregion

        void IDisposable.Dispose()
        {
            if (_strings != null)
            {
                _strings.Free();
                _strings = null;
            }

            (_provider as IDisposable)?.Dispose();
        }

        public void UpdateToken(CompleteToken t)
        {
            _buffer[_bufferidx] = t;
        }

        /// <summary>
        /// Gets token information.
        /// </summary>
        public CompleteToken Lookup(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException();

            if (index >= _buffer.Count)
            {
                BufferTokens(index + 1);
            }

            if (index < _buffer.Count)
            {
                return _buffer[index];
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Replaces range of tokens with whitespace.
        /// We need to keep the spans of whitespaces for PHPDoc resolution.
        /// </summary>
        public void Remove(int start, int count)
        {
            Lookup(start + count);

            for (int i = 0; i < count; i++)
            {
                var idx = start + i;
                _buffer[idx] = _buffer[idx].WithToken(Tokens.T_WHITESPACE);
            }
        }

        public CompleteToken WithTokenText(CompleteToken token)
        {
            if (token.TokenText == null)
            {
                token = token.WithTokenText(_strings.Add(_sourceunit.GetSourceCode(token.TokenPosition)));
            }

            return token;
        }
    }
}
