using System;
using System.Windows.Forms;

namespace NetSparkle
{
    /// <summary>
    ///     Like a notification balloon, but more reliable "toast" because it slowly goes up, then down.
    ///     Subscribe to the Click even to know if the user clicked on it.
    /// </summary>s
    public partial class ToastNotifier : Form
    {
        private readonly System.Windows.Forms.Timer _goDownTimer;
        private readonly System.Windows.Forms.Timer _goUpTimer;
        private readonly System.Windows.Forms.Timer _pauseTimer;
        private int _startPosX;
        private int _startPosY;

        /// <summary>
        ///     constructor
        /// </summary>
        public ToastNotifier()
        {
            InitializeComponent();
            // We want our window to be the top most
            TopMost = true;
            // Pop doesn't need to be shown in task bar
            ShowInTaskbar = false;
            // Create and run timer for animation
            _goUpTimer = new System.Windows.Forms.Timer { Interval = 25 };
            _goUpTimer.Tick += GoUpTimerTick;
            _goDownTimer = new System.Windows.Forms.Timer { Interval = 25 };
            _goDownTimer.Tick += GoDownTimerTick;
            _pauseTimer = new System.Windows.Forms.Timer { Interval = 15000 };
            _pauseTimer.Tick += PauseTimerTick;
        }

        /// <summary>
        ///     The user clicked on the toast popup
        /// </summary>
        public event EventHandler? ToastClicked;

        private void PauseTimerTick(object sender, EventArgs e)
        {
            _pauseTimer.Stop();
            _goDownTimer.Start();
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Move window out of screen
            _startPosX = Screen.PrimaryScreen.WorkingArea.Width - Width;
            _startPosY = Screen.PrimaryScreen.WorkingArea.Height;
            SetDesktopLocation(_startPosX, _startPosY);
            base.OnLoad(e);
            // Begin animation
            _goUpTimer.Start();
        }

        private void GoUpTimerTick(object sender, EventArgs e)
        {
            //Lift window by 5 pixels
            _startPosY -= 5;
            //If window is fully visible stop the timer
            if (_startPosY < Screen.PrimaryScreen.WorkingArea.Height - Height)
            {
                _goUpTimer.Stop();
                _pauseTimer.Start();
            }
            else
            {
                SetDesktopLocation(_startPosX, _startPosY);
            }
        }

        private void GoDownTimerTick(object sender, EventArgs e)
        {
            //Lower window by 5 pixels
            _startPosY += 5;
            //If window is fully visible stop the timer
            if (_startPosY > Screen.PrimaryScreen.WorkingArea.Height)
            {
                _goDownTimer.Stop();
                Hide();
            }
            else
            {
                SetDesktopLocation(_startPosX, _startPosY);
                SendToBack();
            }
        }

        private void ToastNotifier_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
            var handler = ToastClicked;
            handler?.Invoke(this, e);
        }

        /// <summary>
        ///     Show the toast
        /// </summary>
        /// <param name="message"></param>
        /// <param name="callToAction">Text of the hyperlink </param>
        /// <param name="seconds">How long to show before it goes back down</param>
        public void Show(string message, string callToAction, int seconds)
        {
            _message.Text = message;
            _callToAction.Text = callToAction;
            _pauseTimer.Interval = 1000 * seconds;
            Show();
        }

        private void CallToAction_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ToastNotifier_Click(sender, e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();

                _pauseTimer.Dispose();
                _goDownTimer.Dispose();
                _goUpTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
