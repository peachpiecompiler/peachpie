using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.NET.Sdk.Versioning
{
    /// <summary>
    /// Single composer version.
    /// </summary>
    public struct ComposerVersion : IEquatable<ComposerVersion>, IComparable<ComposerVersion>
    {
        /// <summary>
        /// Version corresponding to <c>"*"</c>.
        /// </summary>
        public static ComposerVersion Any => new ComposerVersion(Asterisk);

        private ComposerVersion(int major, int minor, int build, int parts)
        {
            if (parts < 0 || parts > 3) throw new ArgumentOutOfRangeException(nameof(parts));

            Major = major;
            Minor = minor;
            Build = build;
            Stability = null;
            PartsCount = parts;
        }

        /// <summary></summary>
        public ComposerVersion(int major, int minor, int build)
            : this(major, minor, build, 3)
        {
        }

        /// <summary></summary>
        public ComposerVersion(int major, int minor)
            : this(major, minor, Asterisk, 2)
        {
        }

        /// <summary></summary>
        public ComposerVersion(int major)
            : this(major, Asterisk, Asterisk, 1)
        {
        }

        /// <summary>
        /// Denotifies a version component that matches to anything.
        /// </summary>
        public static int Asterisk => -1;

        /// <summary>
        /// Stability flag corresponding to no version suffix (no PreRelase).
        /// </summary>
        public static string StabilityStable => "stable";

        /// <summary>
        /// Stability flag corresponding to development suffix (<c>"-dev"</c>).
        /// </summary>
        public static string StabilityDev => "dev";

        /// <summary>
        /// Gets value indicating the versions refers to a stable release (no Pre-Release).
        /// </summary>
        public bool IsStable => string.IsNullOrEmpty(Stability) || Stability.Equals(StabilityStable, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets value indicating the versions refers to a pre-release.
        /// </summary>
        public bool IsPreRelease => !IsStable;

        /// <summary>
        /// Major version component/
        /// Value of <c>-1</c> represents not specified version component.
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// Minor version component.
        /// Value of <c>-1</c> represents not specified version component.
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// Build number component.
        /// Value of <c>-1</c> represents not specified version component.
        /// </summary>
        public int Build { get; set; }

        /// <summary>
        /// Stability flag.
        /// </summary>
        public string Stability { get; set; }

        /// <summary>
        /// What parts of the version are specified.
        /// </summary>
        public int PartsCount { get; set; }

        /// <summary>
        /// Major component is any.
        /// </summary>
        public bool IsAnyMajor => Major < 0 || PartsCount < 1;

        /// <summary>
        /// Minor component is any.
        /// </summary>
        public bool IsAnyMinor => Minor < 0 || PartsCount < 2;

        /// <summary>
        /// Minor component is any.
        /// </summary>
        public bool IsAnyBuild => Build < 0 || PartsCount < 3;

        /// <summary>
        /// Gets value indicating the value is specified.
        /// </summary>
        public bool HasValue => PartsCount != 0 || Stability != null;

        /// <summary>
        /// Returns string representation of the version in format <c>0.0.0</c> or <c>*</c>, depending on <see cref="PartsCount"/>
        /// </summary>
        public override string ToString()
        {
            string majorstr = Major < 0 ? "*" : Major.ToString();
            string minorstr = Minor < 0 ? "*" : Minor.ToString();
            string buildstr = Build < 0 ? "*" : Build.ToString();

            return PartsCount switch
            {
                0 => "",
                1 => $"{majorstr}",
                2 => $"{majorstr}.{minorstr}",
                3 => $"{majorstr}.{minorstr}.{buildstr}",
                _ => throw new ArgumentException(),
            };
        }

        /// <summary>
        /// Parses version string.
        /// </summary>
        public static bool TryParse(string value, out ComposerVersion version) => TryParse(value.AsSpan(), out version);

        /// <summary>
        /// Parses version string.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> value, out ComposerVersion version)
        {
            value = value.Trim();

            if (value.IsEmpty)
            {
                version = default;
                return false;
            }

            var result = Any;

            // A.B.C-<stable>

            // [0-9]+|\*
            int ConsumeDigitsOrAsterisk(ReadOnlySpan<char> value, out int num)
            {
                num = 0;

                if (value.IsEmpty)
                {
                    return 0;
                }
                else if (char.IsDigit(value[0]))
                {
                    int i = 0;

                    for (; i < value.Length && value[i] >= '0' && value[i] <= '9'; i++)
                    {
                        num = num * 10 + (value[i] - '0');
                    }

                    return i;
                }
                else if (value[0] == '*')
                {
                    num = Asterisk;
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            bool ConsumeDot(ReadOnlySpan<char> value)
            {
                return value.Length != 0 && value[0] == '.';
            }

            int consumed;
            int n;

            // Major
            if ((consumed = ConsumeDigitsOrAsterisk(value, out n)) > 0)
            {
                result.Major = n;
                result.PartsCount = 1;
                value = value.Slice(consumed);

                // .
                if (ConsumeDot(value))
                {
                    value = value.Slice(1);

                    // Minor
                    if ((consumed = ConsumeDigitsOrAsterisk(value, out n)) > 0)
                    {
                        result.Minor = n;
                        result.PartsCount = 2;
                        value = value.Slice(consumed);

                        // .
                        if (ConsumeDot(value))
                        {
                            value = value.Slice(1);

                            // Build
                            if ((consumed = ConsumeDigitsOrAsterisk(value, out n)) > 0)
                            {
                                result.Build = n;
                                result.PartsCount = 3;
                                value = value.Slice(consumed);
                            }
                        }
                    }
                }
            }

            // -<stability>
            if (value.Length != 0 && value[0] == '-')
            {
                int i = 1;
                while (i < value.Length && !char.IsWhiteSpace(value[i])) i++;

                result.Stability = value.Slice(1, i - 1).ToString();
                value = value.Slice(i);
            }

            //
            version = result;
            return value.IsEmpty; // all consumed
        }

        /// <summary>
        /// Substitutes all non-specified components with <c>0</c>. Always returns version with 3 components.
        /// e.g.
        /// * -> 0.0.0
        /// 1.* -> 1.0.0
        /// 1.2 -> 1.2.0
        /// </summary>
        public ComposerVersion AnyToZero() =>
            new ComposerVersion(IsAnyMajor ? 0 : Major, IsAnyMinor ? 0 : Minor, IsAnyBuild ? 0 : Build)
            {
                Stability = Stability
            };

        /// <summary>
        /// Returns the same version with specified stability flag.
        /// </summary>
        public ComposerVersion WithStabilityFlag(string stability) =>
            new ComposerVersion(Major, Minor, Build, PartsCount)
            {
                Stability = stability,
            };

        /// <summary>
        /// Gets the next closest version which satisfies:
        /// value &lt; closest_higer
        /// e.g.
        /// * -> *
        /// 1.* -> 2.0
        /// 1.2 -> 1.3
        /// 1.2.3 -> 1.2.4
        /// </summary>
        public ComposerVersion GetClosestHigher()
        {
            if (IsAnyMajor) return new ComposerVersion(Asterisk) { Stability = Stability };
            if (IsAnyMinor) return new ComposerVersion(Major + 1, 0, 0) { Stability = Stability };
            if (IsAnyBuild) return new ComposerVersion(Major, Minor + 1, 0) { Stability = Stability };
            return new ComposerVersion(Major, Minor, Build + 1) { Stability = Stability };
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ComposerVersion v && Equals(v);

        /// <inheritdoc/>
        public override int GetHashCode() => Major | (Minor << 4) | (Build << 8) /*^ Stability.GetHashCode()*/;

        /// <inheritdoc/>
        public bool Equals(ComposerVersion other) => CompareTo(other) == 0;

        /// <inheritdoc/>
        public int CompareTo(ComposerVersion other)
        {
            if (IsAnyMajor || other.IsAnyMajor) return 0;
            if (Major != other.Major) return Major - other.Major;

            if (IsAnyMinor || other.IsAnyMinor) return 0;
            if (Minor != other.Minor) return Minor - other.Minor;

            if (IsAnyBuild || other.IsAnyBuild) return 0;
            if (Build != other.Build) return Build - other.Build;

            //
            return 0;
        }

        /// <summary></summary>
        public static bool operator ==(ComposerVersion a, ComposerVersion b) => a.Equals(b);

        /// <summary></summary>
        public static bool operator !=(ComposerVersion a, ComposerVersion b) => !a.Equals(b);

        /// <summary></summary>
        public static bool operator <(ComposerVersion a, ComposerVersion b) => a.CompareTo(b) < 0;

        /// <summary></summary>
        public static bool operator >(ComposerVersion a, ComposerVersion b) => a.CompareTo(b) > 0;

        /// <summary></summary>
        public static bool operator <=(ComposerVersion a, ComposerVersion b) => a.CompareTo(b) <= 0;

        /// <summary></summary>
        public static bool operator >=(ComposerVersion a, ComposerVersion b) => a.CompareTo(b) >= 0;
    }
}
