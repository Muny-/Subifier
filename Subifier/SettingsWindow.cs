using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Awesomium.Core;
using Awesomium.Core.Data;
using Microsoft.Win32;

namespace Subifier
{
    public partial class SettingsWindow : Form
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void SettingsWindow_Load(object sender, EventArgs e)
        {
            try
            {
                using (JSObject programInterface = webControl1.CreateGlobalJavascriptObject("programInterface"))
                {
                    programInterface.Bind("openURL", false, (s, ee) =>
                    {
                        string browserPath = GetBrowserPath();
                        if (browserPath == string.Empty)
                            browserPath = "iexplore";
                        Process process = new Process();
                        process.StartInfo = new ProcessStartInfo(browserPath);
                        process.StartInfo.Arguments = "\"" + Uri.EscapeUriString(ee.Arguments[0]) + "\"";
                        process.Start();
                    });

                    programInterface.Bind("getVersion", true, (s, ee) =>
                    {
                        ee.Result = HiddenForm.Version.ToString();
                    });

                    programInterface.Bind("updateProgram", false, (s, ee) =>
                    {
                        HiddenForm.instance.CheckForUpdate(true);
                    });

                    programInterface.Bind("copyToClipboard", false, (s, ee) =>
                    {
                        Clipboard.SetText(ee.Arguments[0]);
                    });

                    programInterface.Bind("closeForm", false, (s, ee) =>
                    {
                        this.Close();
                    });

                    programInterface.Bind("getBrowserVersion", true, (s, ee) =>
                    {
                        ee.Result = WebCore.Version.Major + "." + WebCore.Version.Minor + "." + WebCore.Version.Build + " rev" + WebCore.Version.Revision;
                    });

                    programInterface.Bind("getUA", true, (s, ee) =>
                    {
                        ee.Result = WebCore.Configuration.UserAgent;
                    });

                    programInterface.Bind("getDebugEnabled", true, (s, ee) =>
                    {
                        ee.Result = (bool)(Subifier.Properties.Settings.Default.RemoteDebuggingPort != 0);
                    });

                    programInterface.Bind("getDebugHost", true, (s, ee) =>
                    {
                        ee.Result = Subifier.Properties.Settings.Default.RemoteDebuggingHost;
                    });

                    programInterface.Bind("getDebugPort", true, (s, ee) =>
                    {
                        ee.Result = Subifier.Properties.Settings.Default.RemoteDebuggingPort;
                    });

                    programInterface.Bind("setDebugEnabled", false, (s, ee) =>
                    {
                        HiddenForm.instance.ShowRestartWarning = true;
                        if (((bool)ee.Arguments[0]) == true)
                        {
                            Subifier.Properties.Settings.Default.RemoteDebuggingHost = "127.0.0.1";
                            Subifier.Properties.Settings.Default.RemoteDebuggingPort = 7779;
                        }
                        else
                        {
                            Subifier.Properties.Settings.Default.RemoteDebuggingHost = "0";
                            Subifier.Properties.Settings.Default.RemoteDebuggingPort = 0;
                        }
                    });

                    programInterface.Bind("setDebugHost", false, (s, ee) =>
                    {
                        HiddenForm.instance.ShowRestartWarning = true;
                        Subifier.Properties.Settings.Default.RemoteDebuggingHost = ee.Arguments[0];
                    });

                    programInterface.Bind("setDebugPort", false, (s, ee) =>
                    {
                        HiddenForm.instance.ShowRestartWarning = true;
                        try
                        {
                            Subifier.Properties.Settings.Default.RemoteDebuggingPort = Convert.ToInt32((string)ee.Arguments[0]);
                        }
                        catch
                        {
                            Subifier.Properties.Settings.Default.RemoteDebuggingPort = 7779;
                        }
                    });

                    programInterface.Bind("getUsername", true, (s, ee) =>
                    {
                        ee.Result = Subifier.Properties.Settings.Default.YouTubeUsername;
                    });

                    programInterface.Bind("setUsername", false, (s, ee) =>
                    {
                        Subifier.Properties.Settings.Default.YouTubeUsername = ee.Arguments[0];
                    });

                    programInterface.Bind("getShowThumbnails", true, (s, ee) =>
                    {
                        ee.Result = Subifier.Properties.Settings.Default.ShowThumbnails;
                    });

                    programInterface.Bind("setShowThumbnails", false, (s, ee) =>
                    {
                        Subifier.Properties.Settings.Default.ShowThumbnails = (bool)ee.Arguments[0];
                    });

                    programInterface.Bind("getShowRestartWarning", true, (s, ee) =>
                    {
                        ee.Result = HiddenForm.instance.ShowRestartWarning;
                    });

                    programInterface.Bind("setNotificationTimeout", false, (s, ee) =>
                    {
                        Subifier.Properties.Settings.Default.NotificationTimeout = Convert.ToInt32((string)ee.Arguments[0]) * 1000;
                    });

                    programInterface.Bind("getNotificationTimeout", true, (s, ee) =>
                    {
                        ee.Result = Subifier.Properties.Settings.Default.NotificationTimeout / 1000;
                    });

                    programInterface.Bind("setCheckInterval", false, (s, ee) =>
                    {
                        Subifier.Properties.Settings.Default.CheckInterval = Convert.ToInt32((string)ee.Arguments[0]) * 1000;
                        HiddenForm.instance.SubscriptionsCheck.Interval = Subifier.Properties.Settings.Default.CheckInterval;
                    });

                    programInterface.Bind("getCheckInterval", true, (s, ee) =>
                    {
                        ee.Result = Subifier.Properties.Settings.Default.CheckInterval / 1000;
                    });

                    programInterface.Bind("saveSettings", false, (s, ee) =>
                    {
                        Subifier.Properties.Settings.Default.Save();
                        this.Close();
                    });
                }

                webControl1.WebSession.AddDataSource("ui", new DirectoryDataSource("ui"));
                webControl1.ConsoleMessage += webControl1_ConsoleMessage;
                webControl1.Reload(true);
            }
            catch { }
        }

        void webControl1_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            MessageBox.Show(e.EventType + "\n" + e.Message + "\n" + e.Source + ":" + e.LineNumber);
        }

        private static string GetBrowserPath()
        {
            string browser = string.Empty;
            RegistryKey key = null;

            try
            {
                // try location of default browser path in XP
                key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false);

                // try location of default browser path in Vista
                if (key == null)
                {
                    key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http", false); ;
                }

                if (key != null)
                {
                    //trim off quotes
                    browser = key.GetValue(null).ToString().ToLower().Replace("\"", "");
                    if (!browser.EndsWith("exe"))
                    {
                        //get rid of everything after the ".exe"
                        browser = browser.Substring(0, browser.LastIndexOf(".exe") + 4);
                    }

                    key.Close();
                }
            }
            catch
            {
                return string.Empty;
            }

            return browser;
        }
    }
}
