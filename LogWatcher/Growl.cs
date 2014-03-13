using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LogWatcher
{
    public class Growl
    {
        private string applicationName = "Log Watcher";

        public string ApplicationName
        {
            get { return applicationName; }
            set { applicationName = value; }
        }
        private string notificationName = "notify";

        public string NotificationName
        {
            get { return notificationName; }
            set { notificationName = value; }
        }

        private string growlPath;


        public Growl(string growlPath)
        {
            this.growlPath = growlPath;
        }

        public void RegisterForGrowl()
        {
            ProcessStartInfo psInfo = new ProcessStartInfo();
            psInfo.FileName = this.growlPath;
            psInfo.Arguments = string.Format(@"/r:""{0}"" /a:""{1}"" reg",
                NotificationName, ApplicationName);
            Debug.WriteLine(psInfo.Arguments);
            psInfo.CreateNoWindow = true;
            psInfo.UseShellExecute = false;

            Process.Start(psInfo);
        }

        public void NotifyNotSticky(string title, string text)
        {
            NotifyWithGrowl(title, text, false);
        }

        public void NotifySticky(string title, string text)
        {
            NotifyWithGrowl(title, text, true);
        }

        private void NotifyWithGrowl(string title, string text, bool isSticky)
        {
            string stickyArg = isSticky ? "true" : "false";

            ProcessStartInfo psInfo = new ProcessStartInfo();
            psInfo.FileName = this.growlPath;
            psInfo.Arguments = string.Format(@"/t:""{0}"" /s:{1} /a:""{2}"" /n:""{3}"" ""{4}""",
                title, stickyArg, ApplicationName, NotificationName, text);
            Debug.WriteLine(psInfo.Arguments);
            psInfo.CreateNoWindow = true;
            psInfo.UseShellExecute = false;

            Process.Start(psInfo);
        }
    }
}
