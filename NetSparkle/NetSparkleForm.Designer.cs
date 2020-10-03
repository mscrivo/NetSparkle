using System.ComponentModel;
using System.Windows.Forms;

namespace NetSparkle
{
    /// <summary>
    /// The main form
    /// </summary>
    partial class NetSparkleForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetSparkleForm));
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblInfoText = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.skipButton = new System.Windows.Forms.Button();
            this.buttonRemind = new System.Windows.Forms.Button();
            this.updateButton = new System.Windows.Forms.Button();
            this.HtmlRendererContainer = new System.Windows.Forms.Panel();
            this.imgAppIcon = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.imgAppIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            resources.ApplyResources(this.lblHeader, "lblHeader");
            this.lblHeader.Name = "lblHeader";
            // 
            // lblInfoText
            // 
            resources.ApplyResources(this.lblInfoText, "lblInfoText");
            this.lblInfoText.Name = "lblInfoText";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // skipButton
            // 
            resources.ApplyResources(this.skipButton, "skipButton");
            this.skipButton.Name = "skipButton";
            this.skipButton.UseVisualStyleBackColor = true;
            this.skipButton.Click += new System.EventHandler(this.OnSkipButtonClick);
            // 
            // buttonRemind
            // 
            resources.ApplyResources(this.buttonRemind, "buttonRemind");
            this.buttonRemind.Name = "buttonRemind";
            this.buttonRemind.UseVisualStyleBackColor = true;
            this.buttonRemind.Click += new System.EventHandler(this.OnRemindClick);
            // 
            // updateButton
            // 
            resources.ApplyResources(this.updateButton, "updateButton");
            this.updateButton.Name = "updateButton";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new System.EventHandler(this.OnUpdateButtonClick);
            // 
            // HtmlRendererContainer
            // 
            resources.ApplyResources(this.HtmlRendererContainer, "HtmlRendererContainer");
            this.HtmlRendererContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.HtmlRendererContainer.Name = "HtmlRendererContainer";
            // 
            // imgAppIcon
            // 
            this.imgAppIcon.Image = global::NetSparkle.Properties.Resources.softwareupdateavailable;
            resources.ApplyResources(this.imgAppIcon, "imgAppIcon");
            this.imgAppIcon.Name = "imgAppIcon";
            this.imgAppIcon.TabStop = false;
            // 
            // NetSparkleForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HtmlRendererContainer);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.buttonRemind);
            this.Controls.Add(this.skipButton);
            this.Controls.Add(this.lblInfoText);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.imgAppIcon);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NetSparkleForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            ((System.ComponentModel.ISupportInitialize)(this.imgAppIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PictureBox imgAppIcon;
        private Label lblHeader;
        private Label lblInfoText;
        private Label label3;
        private Button skipButton;
        private Button buttonRemind;
        private Button updateButton;
        private Panel HtmlRendererContainer;
    }
}
