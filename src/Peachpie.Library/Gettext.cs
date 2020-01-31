#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using NGettext;
using NGettext.Loaders;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    [PhpExtension("gettext")]
    public static class Gettext
    {
        #region Defaults

        private const string DefaultDomain = "messages";

        private static string GetDefaultPlural(string msgid1, string msgid2, int n) => (n == 1) ? msgid1 : msgid2;

        #endregion

        #region Caching implementation

        /// <summary>
        /// Each catalog is uniquely identified by its base directory, culture and domain, we can cache them globally.
        /// </summary>
        private struct CacheKey : IEquatable<CacheKey>
        {
            public string LocaleDir;
            public CultureInfo Culture;
            public string Domain;

            public bool Equals(CacheKey other) => LocaleDir == other.LocaleDir && Culture.Equals(other.Culture) && Domain == other.Domain;

            public override bool Equals(object obj) => obj is CacheKey other && Equals(other);

            public override int GetHashCode()
            {
                var hashCode = 878358096;
                hashCode = hashCode * -1521134295 + LocaleDir.GetHashCode();
                hashCode = hashCode * -1521134295 + Culture.GetHashCode();
                hashCode = hashCode * -1521134295 + Domain.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To prevent parsing non-existing or invalid catalogs multiple times, we store the flag of validity in each
        /// loaded catalog.
        /// </summary>
        private sealed class FlaggedCatalog : Catalog
        {
            public FlaggedCatalog(string domain, string localeDir, CultureInfo cultureInfo)
                : base(cultureInfo)
            {
                IsValid = System.IO.Directory.Exists(localeDir);
                if (!IsValid)
                    return;

                try
                {
                    new MoLoader(domain, localeDir).Load(this);
                }
                catch (Exception e)
                {
                    // PHP implementation with native gettext reports only the invalid directory as an error
                    // (the only other symptom of loading error is that the catalog is empty)
                    PhpException.Throw(PhpError.Warning, e.Message);
                }
            }

            public bool IsValid { get; }
        }

        /// <summary>
        /// The current translation domain can vary by request, as well as the mapping of domains to directories.
        /// </summary>
        private sealed class TranslationContext
        {
            public string Domain = DefaultDomain;

            private Dictionary<string, string> DomainDirectoryMap = new Dictionary<string, string>();

            public void BindTextDomain(string domain, string directory) => DomainDirectoryMap[domain] = directory;

            public string? GetLocaleDir(string domain) => DomainDirectoryMap.TryGetValue(domain, out string dir) ? dir : null;
        }

        private static ReaderWriterLockSlim s_cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static Dictionary<CacheKey, FlaggedCatalog> s_catalogCache = new Dictionary<CacheKey, FlaggedCatalog>();

        private static FlaggedCatalog GetOrLoadCatalog(CacheKey key)
        {
            s_cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (s_catalogCache.TryGetValue(key, out var existingCatalog))
                {
                    return existingCatalog;
                }
                else
                {
                    s_cacheLock.EnterWriteLock();
                    try
                    {
                        var newCatalog = new FlaggedCatalog(key.Domain, key.LocaleDir, key.Culture);
                        s_catalogCache.Add(key, newCatalog);

                        return newCatalog;
                    }
                    finally
                    {
                        s_cacheLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                s_cacheLock.ExitUpgradeableReadLock();
            }
        }

        private static FlaggedCatalog? TryGetCatalog(Context ctx, string? domainOverride = null)
        {
            var translationCtx = ctx.GetStatic<TranslationContext>();
            string domain = domainOverride ?? translationCtx.Domain;
            string? localeDir = translationCtx.GetLocaleDir(domain);

            if (localeDir == null)
                return null;

            var culture = Locale.GetCulture(ctx, Locale.Category.All);
            var cacheKey = new CacheKey() { LocaleDir = localeDir, Culture = culture, Domain = domain };

            return GetOrLoadCatalog(cacheKey);
        }

        #endregion

        /// <summary>
        /// This function sets the domain to search within when calls are made to <see cref="gettext(Context, string)"/>,
        /// usually named after an application.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="text_domain">The new message domain, or <c>null</c>to get the current setting without changing it.</param>
        /// <returns>If successful, this function returns the current message domain, after possibly changing it.</returns>
        public static string textdomain(Context ctx, string? text_domain = null)
        {
            var translationCtx = ctx.GetStatic<TranslationContext>();

            if (text_domain != null)
                translationCtx.Domain = text_domain;

            return translationCtx.Domain;
        }

        /// <summary>
        /// Sets the path for a domain.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="directory">The directory path.</param>
        /// <returns>The full pathname for the <paramref name="domain"/> currently being set.</returns>
        [return: CastToFalse]
        public static string? bindtextdomain(Context ctx, string domain, string directory)
        {
            string localeDir = FileSystemUtils.AbsolutePath(ctx, directory);
            var culture = Locale.GetCulture(ctx, Locale.Category.All);

            var catalog = GetOrLoadCatalog(new CacheKey() { LocaleDir = localeDir, Culture = culture, Domain = domain });

            var translationCtx = ctx.GetStatic<TranslationContext>();
            translationCtx.BindTextDomain(domain, localeDir);

            return catalog.IsValid ? localeDir : null;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public static bool bind_textdomain_codeset(string domain, string codeset)
        {
            PhpException.FunctionNotSupported(nameof(bind_textdomain_codeset));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public static string dcgettext(Context ctx, string domain, string message, int category)
        {
            PhpException.FunctionNotSupported(nameof(dcgettext));
            return message;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public static string dcngettext(Context ctx, string domain, string msgid1, string msgid2, int n, int category)
        {
            PhpException.FunctionNotSupported(nameof(dcngettext));
            return GetDefaultPlural(msgid1, msgid2, n);
        }

        /// <summary>
        /// Overrides the domain for a single lookup.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="message">The message.</param>
        /// <returns>Returns a translated string if one is found in the translation table, or the submitted message if not found.</returns>
        public static string dgettext(Context ctx, string domain, string message) => TryGetCatalog(ctx, domain)?.GetString(message) ?? message;

        /// <summary>
        /// Overrides the domain for a single plural message lookup.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="msgid1">The singular message ID.</param>
        /// <param name="msgid2">The plural message ID.</param>
        /// <param name="n">The number (e.g. item count) to determine the translation for the respective grammatical number.</param>
        /// <returns>Returns correct plural form of message identified by <paramref name="msgid1"/> and <paramref name="msgid2"/>
        /// for count <paramref name="n"/>.</returns>
        public static string dngettext(Context ctx, string domain, string msgid1, string msgid2, int n) =>
            TryGetCatalog(ctx, domain)?.GetPluralString(msgid1, msgid2, n) ?? GetDefaultPlural(msgid1, msgid2, n);

        /// <summary>
        /// Lookup a message in the current domain
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="message">The message being translated.</param>
        /// <returns>Returns a translated string if one is found in the translation table, or the submitted message if not found.</returns>
        public static string gettext(Context ctx, string message) => TryGetCatalog(ctx)?.GetString(message) ?? message;

        /// <summary>
        /// Lookup a message in the current domain
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="message">The message being translated.</param>
        /// <returns>Returns a translated string if one is found in the translation table, or the submitted message if not found.</returns>
        public static string _(Context ctx, string message) => gettext(ctx, message);

        /// <summary>
        /// The plural version of <see cref="gettext(Context, string)"/>. Some languages have more than one form for plural messages dependent on the count.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="msgid1">The singular message ID.</param>
        /// <param name="msgid2">The plural message ID.</param>
        /// <param name="n">The number (e.g. item count) to determine the translation for the respective grammatical number.</param>
        /// <returns>Returns correct plural form of message identified by <paramref name="msgid1"/> and <paramref name="msgid2"/>
        /// for count <paramref name="n"/>.</returns>
        public static string ngettext(Context ctx, string msgid1, string msgid2, int n) =>
            TryGetCatalog(ctx)?.GetPluralString(msgid1, msgid2, n) ?? GetDefaultPlural(msgid1, msgid2, n);
    }
}
