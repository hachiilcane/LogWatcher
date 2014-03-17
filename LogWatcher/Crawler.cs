using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace LogWatcher
{
    public class Crawler
    {
        public enum NotificationKind
        {
            DoNotCare,
            Normal,
            Error
        }

        private const string ApplicationNameForGrowl = "LogWatcher";
        private const string NotificationNameForGrowl = "notify";

        private const int LimitNotifyingCountAtOnce = 25;

        private Growl growl;
        private string targetDirs;
        private string targetFiles;
        private int watchingInterval;

        Regex errorIgnoreCaseReg;
        Regex errorReg;
        Regex normalIgnoreCaseReg;
        Regex normalReg;

        private List<WatchingFile> watchFiles = new List<WatchingFile>();

        private volatile bool isQuiting = false;

        /// <summary>
        /// main process
        /// </summary>
        public void Do()
        {
            Console.WriteLine("Started LogWatcher.");
            Console.WriteLine("Enter 'q' key to quit.");

            Initialize();
            DisplaySetting();

            SearchTarget();

            Thread t = new Thread(new ThreadStart(ConsoleInput));
            t.Start();

            // loop forever!!
            while (true)
            {
                System.Threading.Thread.Sleep(this.watchingInterval);

                if (isQuiting)
                {
                    Environment.Exit(0);
                }

                foreach (WatchingFile file in this.watchFiles)
                {
                    List<string> newLines = file.GetNewLines();
                    if (newLines.Count > 0)
                    {
                        Debug.WriteLine("File updated: " + file.FullName);
                        Console.WriteLine("Updated: " + file.FullName);

                        int notifiedCount = 0;
                        foreach (string line in newLines)
                        {
                            if (notifiedCount >= LimitNotifyingCountAtOnce)
                            {
                                NotifyFromWatcher("[ATTENTION] Too many log lines! Skipped notifying a part of created log. Please read the log file directly.");
                                break;
                            }

                            bool hasNotified = NotifyText(line);
                            if(hasNotified) notifiedCount++;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  Receive quit command. This runs as other thread.
        /// </summary>
        private void ConsoleInput()
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();

                if (key.Key == ConsoleKey.Q)
                {
                    this.isQuiting = true;
                    break;
                }
            }
        }

        private void Initialize()
        {
            Settings1 s = new Settings1();

            this.growl = new Growl(RemoveDoubleQuotations(s.GrowlNotifyPath));
            this.growl.ApplicationName = ApplicationNameForGrowl;
            this.growl.NotificationName = NotificationNameForGrowl;
            this.growl.RegisterForGrowl();

            this.targetDirs = s.TargetDirs;
            this.targetFiles = s.TargetFiles;
            this.watchingInterval = s.WatchingInterval;

            if (s.ErrorWordsIgnoreCase != string.Empty)
                errorIgnoreCaseReg = new Regex(s.ErrorWordsIgnoreCase, RegexOptions.IgnoreCase);
            if (s.ErrorWords != string.Empty)
                errorReg = new Regex(s.ErrorWords);
            if (s.NormalWordsIgnoreCase != string.Empty)
                normalIgnoreCaseReg = new Regex(s.NormalWordsIgnoreCase, RegexOptions.IgnoreCase);
            if (s.NormalWords != string.Empty)
                normalReg = new Regex(s.NormalWords, RegexOptions.IgnoreCase);
        }

        private void DisplaySetting()
        {
            Settings1 s = new Settings1();

            Console.WriteLine("Now setting is following:");
            Console.WriteLine(" + WatchingInterval: {0}", this.watchingInterval);
            Console.WriteLine(" + ErrorWordsIgnoreCase: {0}", s.ErrorWordsIgnoreCase);
            Console.WriteLine(" + ErrorWords: {0}", s.ErrorWords);
            Console.WriteLine(" + NormalWordsIgnoreCase: {0}", s.NormalWordsIgnoreCase);
            Console.WriteLine(" + NormalWords: {0}", s.NormalWords);
        }

        private void SearchTarget()
        {
            Console.WriteLine("Target files are:");

            // Directories
            if (this.targetDirs != string.Empty)
            {
                string[] paths = this.targetDirs.Split(',');

                foreach (string path in paths)
                {
                    AddDirAsTarget(RemoveDoubleQuotations(path));
                }
            }

            // Files
            if (this.targetFiles != string.Empty)
            {
                string[] paths = this.targetFiles.Split(',');

                foreach (string path in paths)
                {
                    AddFileAsTarget(RemoveDoubleQuotations(path));
                }
            }
        }

        private void AddDirAsTarget(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                DirectoryInfo dir = new DirectoryInfo(dirPath);

                FileInfo[] filesInDir = dir.GetFiles();
                foreach (FileInfo file in filesInDir)
                {
                    string filename = file.Name;

                    if (Regex.IsMatch(filename, @".+\.log$", RegexOptions.IgnoreCase))
                    {
                        WatchingFile watchFile = new WatchingFile(file);
                        Debug.WriteLine(watchFile.FullName);
                        Console.WriteLine(" + " + watchFile.FullName);
                        this.watchFiles.Add(watchFile);
                    }
                }
            }
        }

        private void AddFileAsTarget(string filePath)
        {
            FileInfo file = new FileInfo(filePath);

            if(file.Exists)
            {
                WatchingFile watchFile = new WatchingFile(file);
                Debug.WriteLine(watchFile.FullName);
                Console.WriteLine(" + " + watchFile.FullName);
                this.watchFiles.Add(watchFile);
            }
        }

        /// <summary>
        /// Send growl an message that matches keyword.
        /// </summary>
        /// <param name="text">message</param>
        /// <returns>has notified?</returns>
        private bool NotifyText(string text)
        {
            bool hasNotified = false;

            NotificationKind kind = JudgeNotificationKind(text);
            switch (kind)
            {
                case NotificationKind.Normal:
                    growl.NotifyNotSticky("Log", text);
                    hasNotified = true;
                    break;
                case NotificationKind.Error:
                    growl.NotifySticky("Log Error!!", text);
                    hasNotified = true;
                    break;
            }

            return hasNotified;
        }

        private void NotifyFromWatcher(string text)
        {
            growl.NotifySticky("LogWatcher Infomation", text);
        }

        private NotificationKind JudgeNotificationKind(string text)
        {
            NotificationKind kind = NotificationKind.DoNotCare;

            if ((errorIgnoreCaseReg != null && errorIgnoreCaseReg.IsMatch(text))
                || (errorReg != null && errorReg.IsMatch(text)))
            {
                kind = NotificationKind.Error;
            }
            else if ((normalIgnoreCaseReg != null && normalIgnoreCaseReg.IsMatch(text))
                || normalReg != null && normalReg.IsMatch(text))
            {
                kind = NotificationKind.Normal;
            }

            return kind;
        }

        private static string RemoveDoubleQuotations(string text)
        {
            return text.Replace(@"""", "");
        }
    }
}
