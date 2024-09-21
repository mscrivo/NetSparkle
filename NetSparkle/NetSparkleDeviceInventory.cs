using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace NetSparkle;

internal class NetSparkleDeviceInventory(NetSparkleConfiguration? config)
{
    private bool X64System { get; set; }
    private string? OsVersion { get; set; }

    public void CollectInventory()
    {
        // x64
        CollectProcessorBitness();

        // windows
        CollectWindowsVersion();
    }

    public string BuildRequestUrl(string baseRequestUrl)
    {
        var retValue = baseRequestUrl;

        // x64 
        retValue += "cpu64bit=" + (X64System ? "1" : "0") + "&";

        // Application name (as indicated by CFBundleName)
        retValue += "appName=" + config?.ApplicationName + "&";

        // Application version (as indicated by CFBundleVersion)
        retValue += "appVersion=" + config?.InstalledVersion + "&";

        // User’s preferred language
        retValue += "lang=" + Thread.CurrentThread.CurrentUICulture + "&";

        // Windows version
        retValue += "osVersion=" + OsVersion + "&";

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

    private void CollectProcessorBitness()
    {
        X64System = Marshal.SizeOf<nint>() == 8;
    }
}
