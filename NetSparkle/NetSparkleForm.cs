// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using MarkdownSharp;
using NetSparkle.Interfaces;

namespace NetSparkle
{
    /// <summary>
    ///     The main form
    /// </summary>
    public sealed partial class NetSparkleForm : Form, INetSparkleForm
    {
        private NetSparkleAppCastItem _currentItem;

        private readonly WebBrowser _webBrowser = new() { Dock = DockStyle.Fill };

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="applicationIcon"></param>
        public NetSparkleForm(NetSparkleAppCastItem item, Icon applicationIcon)
        {
            InitializeComponent();

            // init ui 
            HtmlRendererContainer.Controls.Add(_webBrowser);

            _currentItem = item;

            lblHeader.Text = lblHeader.Text.Replace("APP", item.AppName);
            lblInfoText.Text = lblInfoText.Text.Replace("APP", item.AppName + " " + item.Version);
            lblInfoText.Text = lblInfoText.Text.Replace("OLDVERSION", item.AppVersionInstalled);

            if (!string.IsNullOrEmpty(item.ReleaseNotesLink))
            {
                ShowMarkdownReleaseNotes(item);
            }
            else
            {
                RemoveReleaseNotesControls();
            }

            imgAppIcon.Image = applicationIcon.ToBitmap();
            Icon = applicationIcon;

            TopMost = true;
        }

        /// <summary>
        ///     Event fired when the user has responded to the
        ///     skip, later, install question.
        /// </summary>
        public event EventHandler? UserResponded;

        /// <summary>
        ///     The current item being installed
        /// </summary>
        NetSparkleAppCastItem INetSparkleForm.CurrentItem
        {
            get => _currentItem;
            set => _currentItem = value;
        }

        /// <summary>
        ///     The result of ShowDialog()
        /// </summary>
        DialogResult INetSparkleForm.Result => DialogResult;

        /// <summary>
        ///     Hides the release notes
        /// </summary>
        void INetSparkleForm.HideReleaseNotes()
        {
            RemoveReleaseNotesControls();
        }

        /// <summary>
        ///     Shows the dialog
        /// </summary>
        void INetSparkleForm.Show()
        {
            ShowDialog();
            UserResponded?.Invoke(this, new EventArgs());
        }

        private void ShowMarkdownReleaseNotes(NetSparkleAppCastItem item)
        {
            string contents = null;
            if (item.ReleaseNotesLink != null && item.ReleaseNotesLink.StartsWith("file://")) //handy for testing
            {
                contents = File.ReadAllText(item.ReleaseNotesLink.Replace("file://", ""));
            }
            else
            {
                using var webClient = new WebClient();
                if (item.ReleaseNotesLink != null)
                {
                    contents = webClient.DownloadString(item.ReleaseNotesLink);
                }
            }
            var md = new Markdown();

            if (contents != null)
            {
                _webBrowser.DocumentText = md.Transform(contents);
            }
        }

        /// <summary>
        ///     Removes the release notes control
        /// </summary>
        public void RemoveReleaseNotesControls()
        {
            if (label3.Parent == null)
            {
                return;
            }

            // calc new size
            var newSize = new Size(Size.Width, Size.Height - label3.Height - HtmlRendererContainer.Height);

            // remove the no more needed controls            
            label3.Parent.Controls.Remove(label3);
            HtmlRendererContainer.Parent.Controls.Remove(HtmlRendererContainer);

            // resize the window
            /*this.MinimumSize = newSize;
            this.Size = this.MinimumSize;
            this.MaximumSize = this.MinimumSize;*/
            Size = newSize;
        }

        /// <summary>
        ///     Event called when the skip button is clicked
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnSkipButtonClick(object sender, EventArgs e)
        {
            // set the dialog result to no
            DialogResult = DialogResult.No;

            // close the windows
            Close();
        }

        /// <summary>
        ///     Event called when the "remind me later" button is clicked
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnRemindClick(object sender, EventArgs e)
        {
            // set the dialog result ot retry
            DialogResult = DialogResult.Retry;

            // close the window
            Close();
        }

        /// <summary>
        ///     Called when the "Update button" is clicked
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnUpdateButtonClick(object sender, EventArgs e)
        {
            // set the result to yes
            DialogResult = DialogResult.Yes;

            // close the dialog
            Close();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();

                _webBrowser.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
