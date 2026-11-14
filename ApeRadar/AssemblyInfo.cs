using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ApeRadar.Tests")]

// The modernised build dropped the original repository's XmlConfigurator attribute,
// so log4net never read log4net.config, no appender was registered, and every
// LogUtils.Write* call was silently discarded. Update failures were therefore
// invisible in the field. Restore the attribute so logging works again.
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
