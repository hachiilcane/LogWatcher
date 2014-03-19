using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Growl.Connector;

namespace LogWatcher
{
    public class GrowlGNTP
    {
        private GrowlConnector growl;
        private NotificationType notificationType;
        private Growl.Connector.Application application;

        private string applicationName = "AppName";
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

        public GrowlGNTP()
        {
        }

        public void RegisterForGrowl(string password, string hostName, string tcpPort)
        {
            this.notificationType = new NotificationType(NotificationName, NotificationName);

            int portNum;
            bool isValidPortNum = int.TryParse(tcpPort, out portNum);
            if (!string.IsNullOrEmpty(hostName) && isValidPortNum)
            {
                // use this if you want to connect to a remote Growl instance on another machine
                this.growl = new GrowlConnector(password, hostName, portNum);
            }
            else
            {
                // use this if you need to set a password - you can also pass null or an empty string to this constructor to use no password
                this.growl = new GrowlConnector(password);
            }  

            //this.growl.NotificationCallback += new GrowlConnector.CallbackEventHandler(growl_NotificationCallback);

            // set this so messages are sent in plain text (easier for debugging)
            this.growl.EncryptionAlgorithm = Cryptography.SymmetricAlgorithmType.PlainText;

            this.application = new Growl.Connector.Application(ApplicationName);

            this.growl.Register(application, new NotificationType[] { notificationType });
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

            Notification notification = new Notification(this.application.Name, this.notificationType.Name, null, title, text);
            notification.Sticky = isSticky;
            this.growl.Notify(notification);
        }
    }
}
