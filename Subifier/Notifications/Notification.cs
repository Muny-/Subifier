using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Animation;

namespace Subifier.Notifications
{
    public partial class Notification : Form
    {
        public string Title = "";
        public string Description = "";
        public string Thumbnail = "";
        public string Author = "";
        public string URL = "";
        public Point TargetLocation;
        private Point startedLocation;
        private NotificationManager notificationManager;

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;

                baseParams.ExStyle |= (int)(
                  0x08000000L |
                  0x00000080L);

                return baseParams;
            }
        }

        public Notification(string Title, string Description, string Thumbnail, Point location, NotificationManager notificationManager, string Author, string URL)
        {
            this.SetStyle(ControlStyles.Selectable, false);
            this.notificationManager = notificationManager;

            this.Title = Title;
            this.Description = Description;
            this.Thumbnail = Thumbnail;
            this.Author = Author;
            this.URL = URL;

            InitializeComponent();
            this.Location = location;
            ease.EasingMode = EasingMode.EaseIn;
            ease2.EasingMode = EasingMode.EaseInOut;
            this.SetStyle(ControlStyles.Selectable, false);
            this.TopLevel = true;
            this.SetTopLevel(true);
        }

        public void CloseNotification()
        {
            hideNotificationTimer.Start();
        }

        public void MoveTo(Point point)
        {
            duration2 = 0.3;
            startedLocation = this.Location;
            TargetLocation = point;
            moveTimer.Start();
        }

        private void showNotificationTimer_Tick(object sender, EventArgs e)
        {
            if (duration > 0.01)
            {
                duration -= 0.02;
                this.Location = new Point((int)Math.Round(ease.Ease(duration) * notificationManager.rightScreen.Bounds.Right, 0) + notificationManager.rightScreen.Bounds.Right - this.Width - 10, this.Location.Y);
            }
            else
            {
                showNotificationTimer.Stop();
            }
        }

        private void hideNotificationTimer_Tick(object sender, EventArgs e)
        {
            if (this.Opacity > 0)
            {
                this.Opacity -= 0.15;
            }
            else
            {
                hideNotificationTimer.Stop();
                this.Close();
            }
        }

        ExponentialEase ease2 = new ExponentialEase();
        double duration2 = 0;

        QuinticEase ease = new QuinticEase();
        double duration = 1;

        private void moveTimer_Tick(object sender, EventArgs e)
        {
            if (duration2 < 1)
            {
                duration2 += 0.02;

                if (TargetLocation.Y > startedLocation.Y)
                {
                    this.Location = new Point(this.Location.X, (int)Math.Round(ease2.Ease(duration2) * (TargetLocation.Y - startedLocation.Y), 0) + startedLocation.Y);
                }
                else
                {
                    this.Location = new Point(this.Location.X, startedLocation.Y - (int)Math.Round(ease2.Ease(duration2) * (startedLocation.Y - TargetLocation.Y), 0));
                }
            }
            else
            {
                moveTimer.Stop();
                duration2 = 0;
            }
        }

        private void CloseNotificationTimer_Tick(object sender, EventArgs e)
        {
            CloseNotification();
        }

        private void Notification_Load(object sender, EventArgs e)
        {
            foreach (Control control in this.Controls)
            {
                if (control.Name != "label3")
                    control.Click += control_Click;
            }
            label1.Text = this.Title;
            label2.Text = this.Description;
            label4.Text = this.Author;
            if (Subifier.Properties.Settings.Default.ShowThumbnails)
            {
                new System.Threading.Thread(delegate()
                {
                getThumbnail:
                    try
                    {
                        var request = WebRequest.Create(this.Thumbnail);

                        using (var response = request.GetResponse())
                        using (var stream = response.GetResponseStream())
                        {
                            pictureBox1.BackgroundImage = Bitmap.FromStream(stream);
                        }
                    }
                    catch
                    {
                        goto getThumbnail;
                    }
                }).Start();
            }
        }

        void control_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(this.URL);
            CloseNotification();
        }

        private void label3_Click(object sender, EventArgs e)
        {
            CloseNotification();
        }
    }
}
