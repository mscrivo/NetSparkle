﻿using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using NetSparkle.Interfaces;

namespace NetSparkle
{
    /// <summary>
    ///     A progress bar
    /// </summary>
    public partial class NetSparkleDownloadProgress : Form, INetSparkleDownloadProgress
    {
        private SizeF _currentScaleFactor = new(1f, 1f);

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="applicationIcon">Your application Icon</param>
        public NetSparkleDownloadProgress(NetSparkleAppCastItem item, Icon applicationIcon)
        {
            InitializeComponent();

            imgAppIcon.Image = applicationIcon.ToBitmap();
            Icon = applicationIcon;

            // init ui
            btnInstallAndReLaunch.Visible = false;
            lblHeader.Text = lblHeader.Text.Replace("APP", item.AppName + " " + item.Version);
            progressDownload.Maximum = 100;
            progressDownload.Minimum = 0;
            progressDownload.Step = 1;

            // show the right 
            Size = new Size(Size.Width, (int)(107 * _currentScaleFactor.Height));
            lblSecurityHint.Visible = false;
        }

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            // Record the running scale factor used
            _currentScaleFactor = new SizeF(
                    _currentScaleFactor.Width * factor.Width,
                    _currentScaleFactor.Height * factor.Height);
        }

        /// <summary>
        ///     event to fire when the form asks the application to be relaunched
        /// </summary>
        public event EventHandler? InstallAndRelaunch;

        /// <summary>
        ///     Show the UI and waits
        /// </summary>
        void INetSparkleDownloadProgress.ShowDialog()
        {
            base.ShowDialog();
        }

        /// <summary>
        ///     Update UI to show file is downloaded and signature check result
        /// </summary>
        /// <param name="signatureValid"></param>
        public void ChangeDownloadState(bool signatureValid)
        {
            progressDownload.Visible = false;
            btnInstallAndReLaunch.Visible = true;

            UpdateDownloadValid(signatureValid);
        }

        /// <summary>
        ///     Force window close
        /// </summary>
        public void ForceClose()
        {
            DialogResult = DialogResult.Abort;
            Close();
        }

        /// <summary>
        ///     Event called when the client download progress changes
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        public void OnClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressDownload.Value = e.ProgressPercentage;
        }

        /// <summary>
        ///     Updates the UI to indicate if the download is valid
        /// </summary>
        private void UpdateDownloadValid(bool signatureValid)
        {
            if (!signatureValid)
            {
                Size = new Size(Size.Width, (int)(137 * _currentScaleFactor.Height));
                lblSecurityHint.Visible = true;
                BackColor = Color.Tomato;
            }
        }

        /// <summary>
        ///     Event called when the "Install and relaunch" button is clicked
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnInstallAndReLaunchClick(object sender, EventArgs e)
        {
            InstallAndRelaunch?.Invoke(this, EventArgs.Empty);
        }
    }
}
