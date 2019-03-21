using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace NetSparkle
{
    internal class NetSparkleDeviceInventory
    {
        private readonly NetSparkleConfiguration _config;

        public NetSparkleDeviceInventory(NetSparkleConfiguration config)
        {
            _config = config;
        }

        public bool x64System { get; set; }
        public uint ProcessorSpeed { get; set; }
        public long MemorySize { get; set; }
        public string OsVersion { get; set; }
        public int CPUCount { get; set; }

        public void CollectInventory()
        {
            // x64
            CollectProcessorBitnes();

            // windows
            CollectWindowsVersion();
        }

        public string BuildRequestUrl(string baseRequestUrl)
        {
            var retValue = baseRequestUrl;

            // x64 
            retValue += "cpu64bit=" + (x64System ? "1" : "0") + "&";

            // cpu speed
            retValue += "cpuFreqMHz=" + ProcessorSpeed + "&";

            // ram size
            retValue += "ramMB=" + MemorySize + "&";

            // Application name (as indicated by CFBundleName)
            retValue += "appName=" + _config.ApplicationName + "&";

            // Application version (as indicated by CFBundleVersion)
            retValue += "appVersion=" + _config.InstalledVersion + "&";

            // User’s preferred language
            retValue += "lang=" + Thread.CurrentThread.CurrentUICulture + "&";

            // Windows version
            retValue += "osVersion=" + OsVersion + "&";

            // CPU type/subtype (see mach/machine.h for decoder information on this data)
            // ### TODO: cputype, cpusubtype ###

            // Mac model
            // ### TODO: model ###

            // Number of CPUs (or CPU cores, in the case of something like a Core Duo)
            // ### TODO: ncpu ###
            retValue += "ncpu=" + CPUCount + "&";

            // sanitize url
            retValue = retValue.TrimEnd('&');

            // go ahead
            return retValue;
        }

        private void CollectWindowsVersion()
        {
            var osInfo = Environment.OSVersion;
            OsVersion = $"{osInfo.Version.Major}.{osInfo.Version.Minor}.{osInfo.Version.Build}.{osInfo.Version.Revision}";
        }

        private void CollectProcessorBitnes()
        {
            x64System = Marshal.SizeOf(typeof (IntPtr)) == 8;
        }
    }
}