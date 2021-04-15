
using System;
using Pchp.Core;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library.DateTime
{
    internal sealed class DateConfiguration : IPhpConfiguration
    {
        public DateConfiguration()
        {
            Context.RegisterConfiguration(this);

            Register<DateConfiguration>("date.timezone", ExtensionName,
                (config) => config.TimeZoneInfo?.Id ?? string.Empty, // TODO: this is not PHP name
                (config, value) =>
                {
                    if (value.IsString(out var zoneName))
                    {
                        var zone = PhpTimeZone.GetTimeZone(zoneName);
                        if (zone != null)
                        {
                            config.TimeZoneInfo = zone;
                        }
                        else
                        {
                            PhpException.Throw(PhpError.Notice, Resources.LibResources.unknown_timezone, zoneName);
                        }
                    }
                    else
                    {
                        PhpException.InvalidArgumentType(nameof(value), PhpVariable.TypeNameString);
                    }
                });
        }

        public DateConfiguration(DateConfiguration other)
        {
            _TimeZoneInfo = other._TimeZoneInfo;
        }

        public static DateConfiguration GetConfiguration(Context ctx) => ctx.Configuration.Get<DateConfiguration>();

        public string ExtensionName => PhpExtensionAttribute.KnownExtensionNames.Date;

        public TimeZoneInfo TimeZoneInfo
        {
            get
            {
                if (_TimeZoneInfo == null)
                {
                    _TimeZoneInfo = PhpTimeZone.DetermineDefaultTimeZone();
                }

                return _TimeZoneInfo;
            }
            set
            {
                _TimeZoneInfo = value;
            }
        }

        TimeZoneInfo _TimeZoneInfo;

        IPhpConfiguration IPhpConfiguration.Copy() => new DateConfiguration(this);
    }
}
