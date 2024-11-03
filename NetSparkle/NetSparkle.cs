using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NetSparkle.Interfaces;

// ReSharper disable MemberCanBePrivate.Global

namespace NetSparkle;

/// <summary>
///     The operation has started
/// </summary>
/// <param name="sender">the sender</param>
public delegate void LoopStartedOperation(object sender, EventArgs e);

/// <summary>
///     The operation has ended
/// </summary>
/// <param name="sender">the sender</param>
/// <param name="updateRequired"><c>true</c> if an update is required</param>
public delegate void LoopFinishedOperation(object sender, EventArgs e, bool updateRequired);

/// <summary>
///     This delegate will be used when an update was detected to allow library
///     consumer to add own user interface capabilities.
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
public delegate void UpdateDetected(object sender, UpdateDetectedEventArgs e);

/// <summary>
///     Class to communicate with a sparkle-based appcast
/// </summary>
public sealed class Sparkle : IDisposable
{
    /// <summary>
    ///     The states of availability
    /// </summary>
    /// <paramater>UpdateAvailable</paramater>
    public enum UpdateStatus
    {
        UpdateAvailable,
        UpdateNotAvailable,
        UserSkipped,
        CouldNotDetermine
    }

    private readonly Icon _applicationIcon;
    private readonly string? _appReferenceAssembly;
    private readonly EventWaitHandle _exitHandle;
    private readonly EventWaitHandle _loopingHandle;
    private TimeSpan _checkFrequency;
    private bool _doInitialCheck;
    private string? _downloadTempFilePath;
    private bool _forceInitialCheck;
    private HttpClientDownloadWithProgress? _webDownloadClient;
    private BackgroundWorker? _worker = new();

    /// <summary>
    ///     ctor which needs the appcast url and a reference assembly
    /// </summary>
    /// <param name="appcastUrl">the URL for the appcast file</param>
    /// <param name="applicationIcon">If you're invoking this from a form, this would be this.Icon</param>
    /// <param name="referenceAssembly">the name of the assembly to use for comparison</param>
    public Sparkle(string appcastUrl, Icon applicationIcon, string? referenceAssembly = null) : this(appcastUrl,
        applicationIcon, referenceAssembly, new DefaultNetSparkleUIFactory())
    {
    }

    /// <summary>
    ///     ctor which needs the appcast url and a referenceAssembly
    /// </summary>
    /// <param name="appcastUrl">the URL for the appcast file</param>
    /// <param name="applicationIcon">If you're invoking this from a form, this would be this.Icon</param>
    /// <param name="referenceAssembly">the name of the assembly to use for comparison</param>
    /// <param name="factory">UI factory to use</param>
    // ReSharper disable once MemberCanBePrivate.Global
    public Sparkle(string appcastUrl, Icon applicationIcon, string? referenceAssembly, INetSparkleUIFactory factory)
    {
        _applicationIcon = applicationIcon;

        UIFactory = factory;

        // init UI
        UIFactory.Init();

        _appReferenceAssembly = null;

        // set the reference assembly
        if (referenceAssembly != null)
        {
            _appReferenceAssembly = referenceAssembly;
            Debug.WriteLine("Checking the following file: " + _appReferenceAssembly);
        }

        // adjust the delegates
        _worker!.WorkerReportsProgress = true;
        _worker.DoWork += OnWorkerDoWork;
        _worker.ProgressChanged += OnWorkerProgressChanged;

        // build the wait handle
        _exitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        _loopingHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        // set the url
        AppcastUrl = appcastUrl;
        Debug.WriteLine("Using the following url: " + AppcastUrl);
    }

    /// <summary>
    ///     Is called in the using context and will stop all background activities
    /// </summary>
    public void Dispose()
    {
        StopLoop();
        UnregisterEvents();
    }

    /// <summary>
    ///     Subscribe to this to get a chance to shut down gracefully before exiting
    /// </summary>
    // ReSharper disable once EventNeverSubscribedTo.Global
    public event CancelEventHandler? AboutToExitForInstallerRun;

    /// <summary>
    ///     This event will be raised when a check loop will be started
    /// </summary>
    // ReSharper disable once EventNeverSubscribedTo.Global
    public event LoopStartedOperation? CheckLoopStarted;

    /// <summary>
    ///     This event will be raised when a check loop is finished
    /// </summary>
    // ReSharper disable once EventNeverSubscribedTo.Global
    public event LoopFinishedOperation? CheckLoopFinished;

    /// <summary>
    ///     This event can be used to override the standard user interface
    ///     process when an update is detected
    /// </summary>
    public event UpdateDetected? UpdateDetected;

    /// <summary>
    ///     This event will be raised when the update window is shown to the user but they've
    ///     opted to skip the update or dismiss it.
    /// </summary>
    public event EventHandler? UpdateWindowDismissed;

    /// <summary>
    ///     The app will check once, after the app settles down.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void CheckOnFirstApplicationIdle()
    {
        Application.Idle += OnFirstApplicationIdle;
    }

    private void OnFirstApplicationIdle(object sender, EventArgs e)
    {
        Application.Idle -= OnFirstApplicationIdle;
        CheckForUpdates(true);
    }

    /// <summary>
    ///     The method starts a NetSparkle background loop
    ///     If NetSparkle is configured to check for updates on startup, proceeds to perform
    ///     the check. You should only call this function when your app is initialized and
    ///     shows its main window.
    /// </summary>
    /// <param name="doInitialCheck"><c>true</c> if this instance should do an initial check.</param>
    /// <param name="checkFrequency">the frequency between checks.</param>
    public void StartLoop(bool doInitialCheck, TimeSpan checkFrequency)
    {
        StartLoop(doInitialCheck, false, checkFrequency);
    }

    /// <summary>
    ///     The method starts a NetSparkle background loop
    ///     If NetSparkle is configured to check for updates on startup, proceeds to perform
    ///     the check. You should only call this function when your app is initialized and
    ///     shows its main window.
    /// </summary>
    /// <param name="doInitialCheck"><c>true</c> if this instance should do an initial check.</param>
    /// <param name="forceInitialCheck"><c>true</c> if this instance should force an initial check.</param>
    // ReSharper disable once UnusedMember.Global
    public void StartLoop(bool doInitialCheck, bool forceInitialCheck = false)
    {
        StartLoop(doInitialCheck, forceInitialCheck, TimeSpan.FromHours(24));
    }

    /// <summary>
    ///     The method starts a NetSparkle background loop
    ///     If NetSparkle is configured to check for updates on startup, proceeds to perform
    ///     the check. You should only call this function when your app is initialized and
    ///     shows its main window.
    /// </summary>
    /// <param name="doInitialCheck"><c>true</c> if this instance should do an initial check.</param>
    /// <param name="forceInitialCheck"><c>true</c> if this instance should force an initial check.</param>
    /// <param name="checkFrequency">the frequency between checks.</param>
    // ReSharper disable once MemberCanBePrivate.Global
    public void StartLoop(bool doInitialCheck, bool forceInitialCheck, TimeSpan checkFrequency)
    {
        // first set the event handle
        _loopingHandle.Set();

        // Start the helper thread as a background worker to 
        // get well ui interaction                        

        // store infos
        _doInitialCheck = doInitialCheck;
        _forceInitialCheck = forceInitialCheck;
        _checkFrequency = checkFrequency;

        ReportDiagnosticMessage("Starting background worker");

        // start the work
        _worker?.RunWorkerAsync();
    }

    /// <summary>
    ///     This method will stop the sparkle background loop and is called
    ///     through the disposable interface automatically
    /// </summary>
    public void StopLoop()
    {
        // ensure the work will finished
        _exitHandle.Set();
    }

    /// <summary>
    ///     UnRegisters events so that we don't have multiple items updating
    /// </summary>
    private void UnregisterEvents()
    {
        if (_worker != null)
        {
            _worker.DoWork -= OnWorkerDoWork;
            _worker.ProgressChanged -= OnWorkerProgressChanged;
            _worker.Dispose();
        }

        _worker = null;

        if (_webDownloadClient != null)
        {
            if (ProgressWindow != null)
            {
                _webDownloadClient.ProgressChanged -= ProgressWindow.OnClientDownloadProgressChanged;
            }

            _webDownloadClient.DownloadComplete -= OnDownloadComplete;

            _webDownloadClient.Dispose();
            _webDownloadClient = null;
        }

        if (UserWindow != null)
        {
            UserWindow.UserResponded -= OnUserWindowUserResponded;
            UserWindow = null;
        }

        if (ProgressWindow != null)
        {
            ProgressWindow.InstallAndRelaunch -= OnProgressWindowInstallAndRelaunch;
            ProgressWindow = null;
        }

        _loopingHandle.Dispose();
        _exitHandle.Dispose();
    }

    /// <summary>
    ///     This method updates the profile information which can be sent to the server if enabled
    /// </summary>
    /// <param name="config">the configuration</param>
    private static void UpdateSystemProfileInformation(NetSparkleConfiguration config)
    {
        // check if profile data is enabled
        if (!EnableSystemProfiling)
        {
            return;
        }

        // check if we need an update
        if (DateTime.Now - config.LastProfileUpdate < new TimeSpan(7, 0, 0, 0))
        {
            return;
        }

        // touch the profile update time
        config.TouchProfileTime();

        // start the profile thread
        var t = new Thread(ProfileDataThreadStart);
        t.Start(config);
    }

    /// <summary>
    ///     Profile data thread
    /// </summary>
    /// <param name="obj">the configuration object</param>
    private static async void ProfileDataThreadStart(object obj)
    {
        try
        {
            if (SystemProfileUrl == null)
            {
                return;
            }

            // get the config
            var config = obj as NetSparkleConfiguration;

            // collect data
            var inv = new NetSparkleDeviceInventory(config);
            inv.CollectInventory();

            // build url
            var requestUrl = inv.BuildRequestUrl(SystemProfileUrl + "?");

            // perform the request
            var client = new HttpClient();
            using var response = await client.GetAsync(requestUrl);
        }
        catch (Exception ex)
        {
            // No exception during data send 
            ReportDiagnosticMessage(ex.Message);
        }
    }

    /// <summary>
    ///     This method checks if an update is required. During this process the appcast
    ///     will be downloaded and checked against the reference assembly. Ensure that
    ///     the calling process has access to the internet and read access to the
    ///     reference assembly. This method is also called from the background loops.
    /// </summary>
    /// <param name="config">the configuration</param>
    /// <param name="latestVersion">returns the latest version</param>
    /// <returns><c>true</c> if an update is required</returns>
    private UpdateStatus GetUpdateStatus(NetSparkleConfiguration config, out NetSparkleAppCastItem? latestVersion)
    {
        // report
        ReportDiagnosticMessage("Downloading and checking appcast");

        // init the appcast
        var cast = new NetSparkleAppCast(AppcastUrl, config);

        // check if any updates are available
        try
        {
            latestVersion = cast.GetLatestVersion();
        }
        catch (Exception e)
        {
            // show the exception message
            ReportDiagnosticMessage("Error during app cast download: " + e.Message);

            // just null the version info
            latestVersion = null;
        }

        if (latestVersion == null)
        {
            ReportDiagnosticMessage("No version information in app cast found");
            return UpdateStatus.CouldNotDetermine;
        }

        ReportDiagnosticMessage("Latest version on the server is " + latestVersion.Version);

        // set the last check time
        ReportDiagnosticMessage("Touch the last check timestamp");
        config.TouchCheckTime();

        // check if the available update has to be skipped
        if (latestVersion.Version != null && latestVersion.Version.Equals(config.SkipThisVersion))
        {
            ReportDiagnosticMessage("Latest update has to be skipped (user decided to skip version " +
                                    config.SkipThisVersion + ")");
            return UpdateStatus.UserSkipped;
        }

        // check if the version will be the same then the installed version
        var v1 = new Version(config.InstalledVersion);
        var v2 = new Version(latestVersion.Version ?? throw new InvalidOperationException());

        // ReSharper disable once InvertIf
        if (v2 <= v1)
        {
            ReportDiagnosticMessage("Installed version is valid, no update needed (" + config.InstalledVersion + ")");
            return UpdateStatus.UpdateNotAvailable;
        }

        // ok we need an update
        return UpdateStatus.UpdateAvailable;
    }

    /// <summary>
    ///     This method reads the local sparkle configuration for the given
    ///     reference assembly
    /// </summary>
    /// <returns>the configuration</returns>
    private NetSparkleConfiguration GetApplicationConfig()
    {
        Configuration ??= new NetSparkleRegistryConfiguration(_appReferenceAssembly);
        Configuration.Reload();
        return Configuration;
    }

    /// <summary>
    ///     This method shows the update ui and allows to perform the
    ///     update process
    /// </summary>
    /// <param name="currentItem">the item to show the UI for</param>
    /// <param name="useNotificationToast"> </param>
    public void ShowUpdateNeededUI(NetSparkleAppCastItem? currentItem, bool useNotificationToast)
    {
        if (useNotificationToast)
        {
            UIFactory.ShowToast(currentItem, _applicationIcon, OnToastClick);
        }
        else
        {
            ShowUpdateNeededUIInner(currentItem);
        }
    }

    private void OnToastClick(object sender, EventArgs e)
    {
        ShowUpdateNeededUIInner((NetSparkleAppCastItem)((Control)sender).Tag);
    }

    private void ShowUpdateNeededUIInner(NetSparkleAppCastItem? currentItem)
    {
        UserWindow ??= UIFactory.CreateSparkleForm(currentItem, _applicationIcon);

        UserWindow.CurrentItem = currentItem;
        if (HideReleaseNotes)
        {
            UserWindow.HideReleaseNotes();
        }

        // clear if already set.
        UserWindow.UserResponded -= OnUserWindowUserResponded;
        UserWindow.UserResponded += OnUserWindowUserResponded;
        UserWindow.Show();
    }

    /// <summary>
    ///     This method reports a message in the diagnostic window
    /// </summary>
    /// <param name="message"></param>
    public static void ReportDiagnosticMessage(string message)
    {
        Debug.WriteLine("netsparkle: " + message);
    }

    /// <summary>
    ///     Starts the download process
    /// </summary>
    /// <param name="item">the appcast item to download</param>
    private void InitDownloadAndInstallProcess(NetSparkleAppCastItem item)
    {
        // get the filename of the download lin
        var segments = item.DownloadLink!.Split('/');
        var fileName = segments[^1];

        // get temp path
        _downloadTempFilePath = Environment.ExpandEnvironmentVariables("%temp%\\" + fileName);
        if (ProgressWindow == null)
        {
            ProgressWindow = UIFactory.CreateProgressWindow(item, _applicationIcon);
        }
        else
        {
            ProgressWindow.InstallAndRelaunch -= OnProgressWindowInstallAndRelaunch;
        }

        ProgressWindow.InstallAndRelaunch += OnProgressWindowInstallAndRelaunch;

        try
        {
            using var client = new HttpClientDownloadWithProgress(item.DownloadLink, _downloadTempFilePath);
            client.ProgressChanged += ProgressWindow.OnClientDownloadProgressChanged;
            client.DownloadComplete += OnDownloadComplete;

            client.StartDownload();
            ProgressWindow.ShowDialog();
        }
        catch (Exception e)
        {
            UIFactory.ShowDownloadErrorMessage(e.Message);
            ProgressWindow?.ForceClose();
        }
    }

    /// <summary>
    ///     Return installer runner command. May throw InvalidDataException
    /// </summary>
    /// <param name="downloadFilePath"></param>
    /// <returns></returns>
    private string GetInstallerCommand(string downloadFilePath)
    {
        var extension = Path.GetExtension(downloadFilePath);
        if (".exe".Equals(extension, StringComparison.CurrentCultureIgnoreCase))
        {
            return downloadFilePath + " " + CustomInstallerArguments;
        }

        if (".msi".Equals(extension, StringComparison.CurrentCultureIgnoreCase))
        {
            // build the command line
            return "msiexec /i \"" + downloadFilePath + "\"";
        }

        if (".msp".Equals(extension, StringComparison.CurrentCultureIgnoreCase))
        {
            // build the command line
            return "msiexec /p \"" + downloadFilePath + "\"";
        }

        throw new InvalidDataException("Unknown installer format");
    }

    /// <summary>
    ///     Runs the downloaded installer
    /// </summary>
    private void RunDownloadedInstaller()
    {
        // get the commandline 
        var commandLineThatLaunchTheClientApp = Environment.CommandLine;
        var workingDir = Environment.CurrentDirectory;

        // generate the batch file path
        var pathToOurInstallBatchFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cmd");
        string installerCmd;
        try
        {
            installerCmd = GetInstallerCommand(_downloadTempFilePath!);
        }
        catch (InvalidDataException)
        {
            UIFactory.ShowUnknownInstallerFormatMessage(_downloadTempFilePath!);
            return;
        }

        // generate the batch file                
        ReportDiagnosticMessage("Generating MSI batch in " + Path.GetFullPath(pathToOurInstallBatchFile));

        using (var write = new StreamWriter(pathToOurInstallBatchFile))
        {
            write.WriteLine(installerCmd);
            if (DoLaunchAfterUpdate)
            {
                write.WriteLine("cd " + workingDir);
                write.WriteLine(commandLineThatLaunchTheClientApp);
            }
        }

        // report
        ReportDiagnosticMessage("Going to execute batch: " + pathToOurInstallBatchFile);

        // start the installer helper
        var process = new Process
        {
            StartInfo = { FileName = pathToOurInstallBatchFile, WindowStyle = ProcessWindowStyle.Hidden }
        };
        process.Start();

        // quit the app
        Environment.Exit(0);
    }

    /// <summary>
    ///     Apps may need, for example, to let user save their work
    /// </summary>
    /// <returns>true if it's ok</returns>
    private bool AskApplicationToSafelyCloseUp()
    {
        if (AboutToExitForInstallerRun == null)
        {
            return true;
        }

        var args = new CancelEventArgs();
        AboutToExitForInstallerRun(this, args);
        return !args.Cancel;
    }

    /// <summary>
    ///     Check for updates, using interaction appropriate for if the user just said "check for updates"
    /// </summary>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public UpdateStatus CheckForUpdatesAtUserRequest()
    {
        Cursor.Current = Cursors.WaitCursor;
        var updateAvailable = CheckForUpdates(false /* toast not appropriate, since they just requested it */);
        Cursor.Current = Cursors.Default;

        switch (updateAvailable)
        {
            case UpdateStatus.UpdateAvailable:
                break;
            case UpdateStatus.UpdateNotAvailable:
                UIFactory.ShowVersionIsUpToDate();
                break;
            case UpdateStatus.UserSkipped:
                UIFactory.ShowVersionIsSkippedByUserRequest(); // TODO: pass skipped version no
                break;
            case UpdateStatus.CouldNotDetermine:
                UIFactory.ShowCannotDownloadAppcast();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return updateAvailable; // in this case, we've already shown UI talking about the new version
    }

    /// <summary>
    ///     Check for updates, using interaction appropriate for where the user doesn't know you're doing it, so be polite
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public UpdateStatus CheckForUpdatesQuietly()
    {
        return CheckForUpdates(true);
    }

    /// <summary>
    ///     Does a one-off check for updates
    /// </summary>
    /// <param name="useNotificationToast">
    ///     set false if you want the big dialog to open up, without the user having the chance
    ///     to ignore the popup toast notification
    /// </param>
    private UpdateStatus CheckForUpdates(bool useNotificationToast)
    {
        var config = GetApplicationConfig();
        // update profile information is needed
        UpdateSystemProfileInformation(config);

        // check if update is required
        var updateStatus = GetUpdateStatus(config, out var latestVersion);
        if (updateStatus != UpdateStatus.UpdateAvailable)
        {
            return updateStatus;
        }

        // show the update window
        ReportDiagnosticMessage("Update needed from version " + config.InstalledVersion + " to version " +
                                latestVersion?.Version);

        var ev = new UpdateDetectedEventArgs
        {
            NextAction = NextUpdateAction.ShowStandardUserInterface,
            LatestVersion = latestVersion
        };

        // if the client wants to intercept, send an event
        if (UpdateDetected != null)
        {
            UpdateDetected(this, ev);
        }
        //otherwise just go forward with the UI notification
        else
        {
            ShowUpdateNeededUI(latestVersion, useNotificationToast);
        }

        return updateStatus;
    }

    /// <summary>
    ///     Updates from appcast
    /// </summary>
    /// <param name="currentItem">the current (top-most) item in the app-cast</param>
    private void Update(NetSparkleAppCastItem? currentItem)
    {
        if (currentItem == null)
        {
            return;
        }

        // show the update ui
        if (EnableSilentMode)
        {
            InitDownloadAndInstallProcess(currentItem);
        }
        else
        {
            ShowUpdateNeededUI(currentItem, true);
        }
    }

    /// <summary>
    ///     Cancels the install
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void CancelInstall()
    {
        _webDownloadClient?.Cancel();
    }

    /// <summary>
    ///     Called when the user responds to the "skip, later, install" question.
    /// </summary>
    /// <param name="sender">not used.</param>
    /// <param name="e">not used.</param>
    private void OnUserWindowUserResponded(object sender, EventArgs e)
    {
        switch (UserWindow)
        {
            case { Result: DialogResult.No }:
                {
                    // skip this version

                    var config = GetApplicationConfig();
                    config.SetVersionToSkip(UserWindow!.CurrentItem?.Version!);

                    UpdateWindowDismissed?.Invoke(this, e);
                    break;
                }
            case { Result: DialogResult.Yes }:
                {
                    // download the binaries
                    if (UserWindow.CurrentItem != null)
                    {
                        InitDownloadAndInstallProcess(UserWindow!.CurrentItem);
                    }

                    break;
                }
            default:
                UpdateWindowDismissed?.Invoke(this, e);
                break;
        }
    }

    /// <summary>
    ///     Called when the progress bar fires the update event
    /// </summary>
    /// <param name="sender">not used.</param>
    /// <param name="e">not used.</param>
    private void OnProgressWindowInstallAndRelaunch(object sender, EventArgs e)
    {
        if (AskApplicationToSafelyCloseUp())
        {
            RunDownloadedInstaller();
        }
    }

    /// <summary>
    ///     This method will be executed as worker thread
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnWorkerDoWork(object sender, DoWorkEventArgs e)
    {
        // store the did run once feature
        var goIntoLoop = true;
        var checkTsp = true;
        var doInitialCheck = _doInitialCheck;
        var isInitialCheck = true;

        // start our lifecycle
        do
        {
            // set state
            var bUpdateRequired = false;

            // notify
            CheckLoopStarted?.Invoke(this, e);

            // report status
            if (doInitialCheck == false)
            {
                ReportDiagnosticMessage("Initial check prohibited, going to wait");
                doInitialCheck = true;
                goto WaitSection;
            }

            // report status
            ReportDiagnosticMessage("Starting update loop...");

            // read the config
            ReportDiagnosticMessage("Reading config...");
            var config = GetApplicationConfig();

            // calc CheckTsp
            var checkTspInternal = checkTsp;

            if (isInitialCheck)
            {
                checkTspInternal = !_forceInitialCheck;
            }

            // check if it's ok the recheck to software state
            if (checkTspInternal)
            {
                var csp = DateTime.Now - config.LastCheckTime;
                if (csp < _checkFrequency)
                {
                    ReportDiagnosticMessage(
                        $"Update check performed within the last {_checkFrequency.TotalMinutes} minutes!");
                    goto WaitSection;
                }
            }
            else
            {
                checkTsp = true;
            }

            // when sparkle will be deactivated wait an other cycle
            if (config.CheckForUpdate == false)
            {
                ReportDiagnosticMessage("Check for updates disabled");
                goto WaitSection;
            }

            // update the runOnce feature
            goIntoLoop = !config.DidRunOnce;

            // update profile information is needed
            UpdateSystemProfileInformation(config);

            // check if update is required
            bUpdateRequired = UpdateStatus.UpdateAvailable == GetUpdateStatus(config, out var latestVersion);
            if (!bUpdateRequired)
            {
                goto WaitSection;
            }

            // show the update window
            ReportDiagnosticMessage("Update needed from version " + config.InstalledVersion + " to version " +
                                    latestVersion?.Version);

            // send notification if needed
            var ev = new UpdateDetectedEventArgs
            {
                NextAction = NextUpdateAction.ShowStandardUserInterface,
                LatestVersion = latestVersion
            };
            UpdateDetected?.Invoke(this, ev);

            // check results
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (ev.NextAction)
            {
                case NextUpdateAction.PerformUpdateUnattended:
                    {
                        ReportDiagnosticMessage("Unattended update wished from consumer");
                        EnableSilentMode = true;
                        _worker?.ReportProgress(1, latestVersion);
                        break;
                    }
                case NextUpdateAction.ProhibitUpdate:
                    {
                        ReportDiagnosticMessage("Update prohibited from consumer");
                        break;
                    }
                default:
                    {
                        ReportDiagnosticMessage("Showing Standard Update UI");
                        _worker?.ReportProgress(1, latestVersion);
                        break;
                    }
            }

#pragma warning disable format
            WaitSection:
#pragma warning restore format
            // reset initialCheck
            isInitialCheck = false;

            // notify
            CheckLoopFinished?.Invoke(this, e, bUpdateRequired);

            // report wait statement
            ReportDiagnosticMessage(
                $"Sleeping for an other {_checkFrequency.TotalMinutes} minutes, exit event or force update check event");

            // wait for
            if (!goIntoLoop)
            {
                break;
            }

            // build the event array
            var handles = new WaitHandle[1];
            handles[0] = _exitHandle;

            // wait for any
            var i = WaitHandle.WaitAny(handles, _checkFrequency);
            if (WaitHandle.WaitTimeout == i)
            {
                ReportDiagnosticMessage($"{_checkFrequency.TotalMinutes} minutes are over");
                continue;
            }

            // check the exit handle
            if (i == 0)
            {
                ReportDiagnosticMessage("Got exit signal");
                break;
            }

            // check an other check needed
            if (i != 1)
            {
                continue;
            }

            ReportDiagnosticMessage("Got force update check signal");
            checkTsp = false;
        } while (true);

        // reset the isLooping handle
        _loopingHandle.Reset();
    }

    /// <summary>
    ///     This method will be notified
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        switch (e.ProgressPercentage)
        {
            case 1:
                Update(e.UserState as NetSparkleAppCastItem);
                break;
            case 0:
                ReportDiagnosticMessage(e.UserState?.ToString() ?? string.Empty);
                break;
        }
    }

    /// <summary>
    ///     Called when the installer is downloaded
    /// </summary>
    private void OnDownloadComplete()
    {
        // test the item for DSA signature
        var isDSAOk = false;
        ReportDiagnosticMessage("Finished downloading file to: " + _downloadTempFilePath);

        // report
        ReportDiagnosticMessage("Performing DSA check");

        // get the assembly
        if (File.Exists(_downloadTempFilePath))
        {
            // check if the file was downloaded successfully
            var absolutePath = Path.GetFullPath(_downloadTempFilePath!);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException();
            }

            if (UserWindow?.CurrentItem?.DSASignature == null)
            {
                isDSAOk = true; // REVIEW. The correct logic, seems to me, is that if the existing, running version of the app
                //had no DSA, and the appcast didn't specify one, then it's ok that the one we've just 
                //downloaded doesn't either. This may be just checking that the appcast didn't specify one. Is 
                //that really enough? If someone can change what gets downloaded, can't they also change the appcast?
            }
            else
            {
                // get the assembly reference from which we start the update progress
                // only from this trusted assembly the public key can be used
                var refAssembly = Assembly.GetEntryAssembly();
                if (refAssembly != null)
                {
                    // Check if we found the public key in our entry assembly
                    if (NetSparkleDSAVerifier.ExistsPublicKey("NetSparkle_DSA.pub"))
                    {
                        // check the DSA Code and modify the back color            
                        using var dsaVerifier = new NetSparkleDSAVerifier("NetSparkle_DSA.pub");
                        isDSAOk = dsaVerifier.VerifyDSASignature(UserWindow.CurrentItem.DSASignature,
                            _downloadTempFilePath);
                    }
                }
            }
        }

        if (EnableSilentMode)
        {
            OnProgressWindowInstallAndRelaunch(this, EventArgs.Empty);
        }

        ProgressWindow?.ChangeDownloadState(isDSAOk);
    }

    #region Properties

    /// <summary>
    ///     Enables system profiling against a profile server
    /// </summary>
    public static bool EnableSystemProfiling
    {
        get => false;
    }

    /// <summary>
    ///     Hides the release notes view when an update was found. This
    ///     mode is switched on automatically when no sparkle:releaseNotesLink
    ///     tag was found in the app cast
    /// </summary>
    public static bool HideReleaseNotes
    {
        get => false;
    }

    /// <summary>
    ///     Contains the profile url for System profiling
    /// </summary>
    public static Uri? SystemProfileUrl
    {
        get => null;
    }

    /// <summary>
    ///     This property enables the silent mode, this means
    ///     the application will be updated without user interaction
    /// </summary>
    public bool EnableSilentMode { get; set; }

    /// <summary>
    ///     If your installer launches the app when it finishes, you don't want this thing to launch it as well. Defaults to
    ///     TRUE.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public bool DoLaunchAfterUpdate { get; set; } = true;

    /// <summary>
    ///     For example, use "/qb" to skip most of the UI, such as asking them to agree to the license again. The full list is
    ///     at http://support.microsoft.com/kb/227091.
    /// </summary>
    public string CustomInstallerArguments { get; set; } = string.Empty;

    /// <summary>
    ///     This property returns true when the update loop is running
    ///     and files when the loop is not running
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public bool IsUpdateLoopRunning
    {
        get => _loopingHandle.WaitOne(0);
    }

    /// <summary>
    ///     Factory for creating UI form like progress window etc.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public INetSparkleUIFactory UIFactory { get; set; }

    /// <summary>
    ///     The user interface window that shows the release notes and
    ///     asks the user to skip, later or update
    /// </summary>
    public INetSparkleForm? UserWindow { get; set; }

    /// <summary>
    ///     The user interface window that shows a download progress bar,
    ///     and then asks to install and relaunch the application
    /// </summary>
    public INetSparkleDownloadProgress? ProgressWindow { get; set; }

    /// <summary>
    ///     The configuration.
    /// </summary>
    public NetSparkleConfiguration? Configuration { get; set; }

    /// <summary>
    ///     Gets or sets the app cast URL
    /// </summary>
    public string AppcastUrl { get; }

    #endregion
}
