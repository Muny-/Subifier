﻿using System;
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
    public partial class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            InitializeComponent();
        }

        private void WelcomeForm_Load(object sender, EventArgs e)
        {
            try
            {
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

        private void Awesomium_Windows_Forms_WebControl_DocumentReady(object sender, Awesomium.Core.UrlEventArgs e)
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

                programInterface.Bind("openSettings", false, (s, ee) =>
                {
                    HiddenForm.instance.Settings.Show();
                });
            }
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
