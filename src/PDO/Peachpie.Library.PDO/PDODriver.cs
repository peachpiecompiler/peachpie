﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Text;
using Peachpie.Library.PDO.Utilities;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDO driver base class
    /// </summary>
    [PhpHidden]
    public abstract class PDODriver
    {
        /// <summary>
        /// Gets the driver name (used in DSN)
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the client version.
        /// </summary>
        /// <value>
        /// The client version.
        /// </value>
        public virtual string ClientVersion
        {
            get
            {
                var version = this.DbFactory.GetType().Assembly.GetName().Version;
                if (version != null)
                {
                    return version.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <inheritDoc />
        public abstract DbProviderFactory DbFactory { get; }

        /// <summary>
        /// Builds the connection string.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        protected abstract string BuildConnectionString(ReadOnlySpan<char> dsn, string? user, string? password, PhpArray options);

        /// <summary>
        /// Opens a new database connection.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public virtual DbConnection OpenConnection(ReadOnlySpan<char> dsn, string? user, string? password, PhpArray options)
        {
            var connection = this.DbFactory.CreateConnection();
            connection.ConnectionString = this.BuildConnectionString(dsn, user, password, options);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Gets the methods added to the PDO instance when this driver is used.
        /// Returns <c>null</c> if the method is not defined.
        /// </summary>
        public virtual ExtensionMethodDelegate? TryGetExtensionMethod(string name) => null;

        /// <summary>
        /// Gets the last insert identifier.
        /// </summary>
        /// <param name="pdo">Reference to corresponding <see cref="PDO"/> instance.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public abstract string GetLastInsertId(PDO pdo, string? name);

        /// <summary>
        /// Sets <see cref="PDO.PDO_ATTR.ATTR_STRINGIFY_FETCHES"/> attribute value.
        /// </summary>
        /// <param name="pdo">Containing <see cref="PDO"/> object reference.</param>
        /// <param name="stringify">Whether to stringify fetched values.</param>
        /// <returns>Value indicating the attribute was set successfully.</returns>
        public virtual bool TrySetStringifyFetches(PDO pdo, bool stringify)
        {
            // NOTE: this method should be removed and stringify handled when actually used in PdoResultResource.GetValues()

            pdo.Stringify = stringify;
            return true;
        }

        /// <summary>
        /// Tries to set a driver specific attribute value.
        /// </summary>
        /// <param name="pdo">Containing <see cref="PDO"/> object reference.</param>
        /// <param name="attributes">The current attributes collection.</param>
        /// <param name="attribute">The attribute to set.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if value is valid, or false if value can't be set.</returns>
        public virtual bool TrySetAttribute(PDO pdo, Dictionary<PDO.PDO_ATTR, PhpValue> attributes, int attribute, PhpValue value)
        {
            return false;
        }

        /// <summary>
        /// Quotes a string for use in a query.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="param">The parameter.</param>
        /// <returns></returns>
        public virtual string Quote(string str, PDO.PARAM param)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Preprocesses the command for use with parameters.
        /// Returns updated query string, optionally creates the <paramref name="bound_param_map"/> with parameter name mapping.
        /// Most of ADO.NET drivers does not allow to use unnamed parameters - we'll rewrite them to named parameters.
        /// </summary>
        /// <param name="queryString">The original command text.</param>
        /// <param name="options">Custom options.</param>
        /// <param name="bound_param_map">Will be set to <c>null</c> or a map of user-provided names to rewritten parameter names.</param>
        public virtual string RewriteCommand(string queryString, PhpArray options, out Dictionary<IntStringKey, IntStringKey>? bound_param_map)
        {
            using (var rewriter = new StatementStringRewriter() { TranslateNamedParams = true, })
            {
                rewriter.ParseString(queryString);

                bound_param_map = rewriter.BoundParamMap;
                queryString = rewriter.RewrittenQueryString;
            }

            //
            return queryString;
        }

        private class StatementStringRewriter : StatementStringParser, IDisposable
        {
            readonly StringBuilder _stringBuilder = StringBuilderUtilities.Pool.Get();
            int _unnamedParamIndex = 0;

            public string RewrittenQueryString => _stringBuilder.ToString();

            public Dictionary<IntStringKey, IntStringKey>? BoundParamMap { get; private set; }

            /// <summary>
            /// Translate `:name` syntax to `@name` syntax.
            /// Add `name` to <see cref="BoundParamMap"/>.
            /// </summary>
            public bool TranslateNamedParams { get; set; }

            protected override void Next(Tokens token, string text, int start, int length)
            {
                string mappedParamName;

                switch (token)
                {
                    case Tokens.UnnamedParameter:

                        mappedParamName = "@_" + _unnamedParamIndex;
                        _stringBuilder.Append(mappedParamName);

                        BoundParamMap ??= new Dictionary<IntStringKey, IntStringKey>();
                        BoundParamMap[_unnamedParamIndex++] = mappedParamName;

                        break;

                    case Tokens.NamedParameter:

                        if (TranslateNamedParams && text[start] == ':')
                        {
                            // :name => @name
                            var paramName = text.Substring(start + 1, length - 1);
                            mappedParamName = "@" + paramName;
                            _stringBuilder.Append(mappedParamName);

                            // BoundParamMap["name"] = "@name"
                            // BoundParamMap[":name"] = "@name"
                            BoundParamMap ??= new Dictionary<IntStringKey, IntStringKey>();
                            BoundParamMap[paramName] = mappedParamName;
                            BoundParamMap[":" + paramName] = mappedParamName;

                            break;
                        }
                        else
                        {
                            goto default;
                        }

                    default:
                        _stringBuilder.Append(text, start, length);
                        break;
                }
            }

            void IDisposable.Dispose()
            {
                StringBuilderUtilities.Pool.Return(_stringBuilder);
            }
        }

        /// <summary>
        /// Processes DB exception and returns corresponding error info.
        /// </summary>
        public virtual void HandleException(Exception ex, out PDO.ErrorInfo errorInfo)
        {
            if (ex is Pchp.Library.Spl.Exception pex)
            {
                errorInfo = PDO.ErrorInfo.Create(string.Empty, pex.getCode().ToString(), pex.Message);
            }
            else
            {
                errorInfo = PDO.ErrorInfo.Create(string.Empty, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Gets the driver specific attribute value
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        public virtual PhpValue GetAttribute(PDO pdo, int attribute)
        {
            return PhpValue.Null;
        }

        /// <summary>
        /// Opens a DataReader.
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="cmd">The command.</param>
        /// <param name="cursor">The cursor configuration.</param>
        /// <returns></returns>
        public virtual DbDataReader OpenReader(PDO pdo, DbCommand cmd, PDO.PDO_CURSOR cursor)
        {
            return cmd.ExecuteReader();
        }
    }
}
