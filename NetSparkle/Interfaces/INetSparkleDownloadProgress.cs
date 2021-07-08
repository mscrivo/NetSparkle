using System;
using System.Net;

namespace NetSparkle.Interfaces
{
    /// <summary>
    ///     Interface for UI element that shows the progress bar
    ///     and a method to install and relaunch the appliction
    /// </summary>
    public interface INetSparkleDownloadProgress
    {
        /// <summary>
        ///     event to fire when the form asks the application to be relaunched
        /// </summary>
        event EventHandler InstallAndRelaunch;

        /// <summary>
        ///     Show the UI and waits
        /// </summary>
        void ShowDialog();

        /// <summary>
        ///     Called when the download progress changes
        /// </summary>
        void OnClientDownloadProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        /// <summary>
        ///     Force window close
        /// </summary>
        void ForceClose();

        /// <summary>
        ///     Update UI to show file is downloaded and signature check result
        /// </summary>
        /// <param name="signatureValid"></param>
        void ChangeDownloadState(bool signatureValid);
    }
}
