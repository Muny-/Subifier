using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Awesomium.Core;
using System.Net;
using System.Xml;
using System.IO;
using Subifier.Notifications;
using System.Threading;

namespace Subifier
{
    public partial class HiddenForm : Form
    {
        // called when subifier is ran
        public HiddenForm()
        {
            Init(true);
        }

        // called when subifier is installed
        public HiddenForm(string InstallLocation)
        {
            Subifier.Properties.Settings.Default.InstallLocation = InstallLocation;
            Subifier.Properties.Settings.Default.Save();
            Init(false);
            Welcome.Show();
        }

        // called when subifier is update
        public HiddenForm(bool wasUpdated)
        {
            Init(false);
            Changelog.Show();
        }

        public void Init(bool cu)
        {
            instance = this;
            if (!WebCore.IsRunning)
            {
                WebCore.Initialize(new WebConfig() { RemoteDebuggingHost = Subifier.Properties.Settings.Default.RemoteDebuggingHost, RemoteDebuggingPort = Subifier.Properties.Settings.Default.RemoteDebuggingPort, LogLevel = LogLevel.None});
            }
            InitializeComponent();
            if (cu)
                CheckForUpdate(false);

            Directory.SetCurrentDirectory(Subifier.Properties.Settings.Default.InstallLocation);
        }

        Dictionary<string, Dictionary<string, Dictionary<string, string>>> subscriptionVideos;
        NotificationManager notificationManager = new NotificationManager();
        public AboutWindow About = new AboutWindow();
        public ChangelogWindow Changelog = new ChangelogWindow();
        public SettingsWindow Settings = new SettingsWindow();
        public WelcomeForm Welcome = new WelcomeForm();
        WebClient wc = new WebClient();
        public static HiddenForm instance;
        public bool ShowRestartWarning = false;
        public const double Version = 0.2;

        bool isChecking = false;

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // really, it's settings
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Settings.IsDisposed)
                Settings = new SettingsWindow();

            Settings.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (About.IsDisposed)
                About = new AboutWindow();

            About.Show();
        }

        private void HiddenForm_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            SubscriptionsCheck.Interval = Subifier.Properties.Settings.Default.CheckInterval;

            // debug
            //About.Show();
            //Changelog.Show();
            //Welcome.Show();
            //Settings.Show();
            // end debug
        }

        public void CheckForUpdate(bool userInitiated)
        {
            new System.Threading.Thread(delegate()
            {
                WebClient wc = new WebClient();
                double latest_ver = Convert.ToDouble(wc.DownloadString("http://picbox.us/program/subifier/version.php"));

                if (latest_ver > Version)
                {
                    UpdateProg();
                }
                else
                {

                }
            }).Start();
        }

        public void UpdateProg()
        {
            try
            {
                WebClient wc = new WebClient();
                DirectoryInfo di = new FileInfo(Path.GetTempFileName()).Directory;
                wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                wc.DownloadFile("http://cdn.azuru.me/apps/subifier/SubifierUpdate.exe", di.FullName + "/SubifierUpdate.exe");
                System.Diagnostics.Process.Start(di.FullName + "/SubifierUpdate.exe", "\"" + Subifier.Properties.Settings.Default.InstallLocation + "\" " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            }
            catch
            {
                MessageBox.Show("The update process was cancelled!");
            }
        }

        private void SubscriptionsCheck_Tick(object sender, EventArgs e)
        {
            if (!isChecking)
            {
                new System.Threading.Thread(delegate()
                {
                    string xml = "";
                    try
                    {
                        isChecking = true;
                        xml = wc.DownloadString("https://gdata.youtube.com/feeds/api/users/" + Subifier.Properties.Settings.Default.YouTubeUsername + "/newsubscriptionvideos");

                        Dictionary<string, Dictionary<string, Dictionary<string, string>>> tempVideos = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                        using (XmlTextReader tr = new XmlTextReader(new StringReader(xml)))
                        {
                            bool canRead = tr.Read();
                            while (canRead)
                            {
                                if (tr.Name == "entry")
                                {
                                    Dictionary<string, string> descriptors = new Dictionary<string, string>();
                                    //tr.ReadToFollowing("title");
                                    tr.ReadToDescendant("title");
                                    descriptors.Add("title", tr.ReadString());
                                    tr.ReadToNextSibling("content");
                                    descriptors.Add("description", tr.ReadString());
                                    tr.ReadToNextSibling("link");
                                    descriptors.Add("url", tr.GetAttribute("href"));
                                    tr.ReadToNextSibling("author");
                                    tr.ReadToDescendant("name");
                                    descriptors.Add("author_name", tr.ReadString());
                                    tr.ReadToFollowing("media:thumbnail");
                                    descriptors.Add("thumbnail", tr.GetAttribute("url"));
                                    tr.ReadToFollowing("entry");

                                    Dictionary<string, Dictionary<string, string>> video = new Dictionary<string, Dictionary<string, string>>();
                                    video.Add(descriptors["title"], descriptors);
                                    tempVideos.Add(descriptors["title"], video);
                                }
                                else
                                {
                                    canRead = tr.Read();
                                }
                            }

                            if (subscriptionVideos == null)
                                subscriptionVideos = tempVideos;
                            else if (tempVideos != subscriptionVideos)
                            {
                                List<KeyValuePair<string, Dictionary<string, Dictionary<string, string>>>> newVideos = tempVideos.Where(value => !subscriptionVideos.Keys.Contains(value.Key)).ToList();

                                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> video in newVideos)
                                {
                                    this.Invoke(new MethodInvoker(delegate()
                                    {
                                        notificationManager.AddNotification(video.Value[video.Value.Keys.First()]["title"], video.Value[video.Value.Keys.First()]["description"], video.Value[video.Value.Keys.First()]["thumbnail"], video.Value[video.Value.Keys.First()]["author_name"], video.Value[video.Value.Keys.First()]["url"]);
                                    }));
                                }
                                subscriptionVideos = tempVideos;
                            }
                        }
                        isChecking = false;
                    }
                    catch { isChecking = false; }
                }).Start();
            }
        }

        private void UpdateCheck_Tick(object sender, EventArgs e)
        {
            CheckForUpdate(false);
        }
    }
}
