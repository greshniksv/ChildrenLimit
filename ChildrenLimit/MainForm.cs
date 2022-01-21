using System;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChildrenLimit.Properties;
using Microsoft.Win32;
using NLog;

namespace ChildrenLimit
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private Timer timer;
        private MenuItem menuItem;
        private int time;
        private string pass;
        private TimeSpan activeTime;
        private int retryCount = 5;
        private bool showMessage;
        private bool needSaveSession;

        private bool disconnectSession;
        private bool logOff;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public MainForm()
        {
            InitializeComponent();
            lblTimeNotSpent.Visible = false;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

        //[DllImport("Kernel32.dll", SetLastError = true)]
        //static extern WTSGetActiveConsoleSessionId();

        const int WTS_CURRENT_SESSION = -1;
        static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        public static bool WindowsLogOff()
        {
            return ExitWindowsEx(0, 0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblNext.Visible = false;
            Log.Info("Start application");
            try
            {
                Session session = new Session();

                SystemEvents.SessionSwitch += (o, args) =>
                {
                    if (args.Reason != SessionSwitchReason.SessionUnlock)
                    {
                        return;
                    }

                    needSaveSession = true;
                    activeTime = TimeSpan.FromMinutes(time);
                    var sessionItem = session.LoadSessions().FirstOrDefault();
                    if (!sessionItem.Equals(default(DateTime)))
                    {
                        if (DateTime.Now - sessionItem < TimeSpan.FromMinutes(40))
                        {
                            lblTimeNotSpent.Visible = true;
                            activeTime = TimeSpan.FromMinutes(1);
                            needSaveSession = false;

                            var nextTime = sessionItem.AddMinutes(40);
                            lblNext.Text = $"{nextTime.Hour:00}:{nextTime.Minute:00}";
                            lblNext.Visible = true;
                        }
                    }

                    lblTimer.Text = $"{(int)activeTime.TotalMinutes}:{activeTime.Seconds:00}";
                    timer.Enabled = true;
                    Show();
                };

                needSaveSession = true;
                InputBox.SetLanguage(InputBox.Language.English);
                pass = ConfigurationManager.AppSettings["pass"];
                disconnectSession = ConfigurationManager.AppSettings["DisconnectSession"] == "true";
                logOff = ConfigurationManager.AppSettings["LogOff"] == "true";

                time = int.Parse(ConfigurationManager.AppSettings["limit"]);
                menuItem = new MenuItem($"Wait: {time} minutes");
                activeTime = TimeSpan.FromMinutes(time);

                var sessionItem1 = session.LoadSessions().FirstOrDefault();
                if (!sessionItem1.Equals(default(DateTime)))
                {
                    if (DateTime.Now - sessionItem1 < TimeSpan.FromMinutes(40))
                    {
                        lblTimeNotSpent.Visible = true;
                        activeTime = TimeSpan.FromMinutes(1);
                        needSaveSession = false;

                        var nextTime = sessionItem1.AddMinutes(40);
                        lblNext.Text = $"{nextTime.Hour:00}:{nextTime.Minute:00}";
                        lblNext.Visible = true;
                    }
                }

                lblTimer.Text = $"{(int)activeTime.TotalMinutes}:{activeTime.Seconds:00}";

                trayIcon = new NotifyIcon()
                {
                    Icon = Resources.toys,
                    ContextMenu = new ContextMenu(new [] {
                    new MenuItem("Exit", Exit),
                    new MenuItem("Open", (o, args) => { Visible = true;  }),
                    //new MenuItem("Start", (o, args) => {
                    //    if (!VerifyPass()) { return; }
                    //    activeTime = TimeSpan.FromMinutes(time);
                    //    timer.Enabled = true;
                    //}),

                    new MenuItem("ReStart", (o, args) => {
                        if (!VerifyPass()) { return; }
                        activeTime = TimeSpan.FromMinutes(time);
                        timer.Enabled = true;
                    }),

                    new MenuItem("Stop", (o, args) => {
                        activeTime = TimeSpan.FromMinutes(1);
                        session.SaveSession();
                        timer.Enabled = false;
                        LogOut();
                    }),

                    new MenuItem("Add time", (o, args) =>
                    {
                        if (!VerifyPass()) { return; }
                        InputBox.ResultValue = string.Empty;
                        try
                        {
                            var dialog = InputBox.ShowDialog("Add time (minutes)", "Add more time",
                                InputBox.Icon.Question, InputBox.Buttons.YesNo,
                                InputBox.Type.TextBox);
                            if (dialog == DialogResult.Yes)
                            {
                                Log.Info("Add time " + InputBox.ResultValue);
                                activeTime += TimeSpan.FromMinutes(int.Parse(InputBox.ResultValue));
                                needSaveSession = true;
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show("Error add time!" + exception.Message);
                        }
                    }),

                    menuItem
                }),
                    Visible = true
                };

                timer = new Timer()
                {
                    Enabled = true,
                    Interval = 1000
                };

                Log.Info("Start");

                timer.Tick += (o, args) =>
                {
                    activeTime -= TimeSpan.FromMilliseconds(timer.Interval);
                    menuItem.Text = $"Wait: {(int)activeTime.TotalMinutes}:{activeTime.Seconds:00} minutes";
                    lblTimer.Text = $"{(int)activeTime.TotalMinutes}:{activeTime.Seconds:00}";
                    lblStatus.Text = timer.Enabled ? "Enabled" : "Disabled";
                    if (activeTime <= TimeSpan.FromMilliseconds(1000))
                    {
                        activeTime += TimeSpan.FromMinutes(1);
                        if (needSaveSession)
                        {
                            session.SaveSession();
                        }

                        timer.Enabled = false;
                        LogOut();
                    }

                    if (activeTime < TimeSpan.FromMinutes(5) && !showMessage)
                    {
                        showMessage = true;
                        //MessageBox.Show("Осталось 5 минут!");
                    }

                    if (activeTime > TimeSpan.FromMinutes(6) && showMessage)
                    {
                        showMessage = false;
                    }

                    if (activeTime > TimeSpan.FromMinutes(1))
                    {
                        lblTimeNotSpent.Visible = false;
                    }
                };
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            if (VerifyPass())
            {
                trayIcon.Visible = false;
                Application.Exit();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private bool VerifyPass()
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    InputBox.ResultValue = string.Empty;
                    var dialog = InputBox.ShowDialog($"Please set password ({retryCount})",
                        "Require passwords", InputBox.Icon.Information, InputBox.Buttons.OkCancel,
                        InputBox.Type.Password);
                    if (dialog == DialogResult.Cancel)
                    {
                        return false;
                    }

                    if (InputBox.ResultValue != pass)
                    {
                        --retryCount;
                        Log.Info("Invalid password");

                        if (retryCount <= 0)
                        {
                            retryCount = 5;
                            LogOut();
                        }
                    }
                    else
                    {
                        retryCount = 5;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return false;
        }

        private void LogOut()
        {
            Log.Info("Log out");
            //MessageBox.Show("LOGOUT");

            if (logOff)
            {
                WindowsLogOff();
            }

            if (disconnectSession)
            {
                WTSDisconnectSession(WTS_CURRENT_SERVER_HANDLE, WTS_CURRENT_SESSION, false);
            }
        }
    }
}
