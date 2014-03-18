using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace LogWatcher
{
    public class WatchingFile
    {
        //2011-08-31 15:27:11.328
        public Regex LogTime = new Regex(@"^(\d\d\d\d)-(\d\d)-(\d\d) (\d\d):(\d\d):(\d\d)\.(\d\d\d)");
        //2011/08/31 15:27:11.328
        public Regex LogTime2 = new Regex(@"^(\d\d\d\d)\/(\d\d)\/(\d\d) (\d\d):(\d\d):(\d\d)\.(\d\d\d)");

        bool lineCountMode = false;
        long lastLineCount = -1;

        private string fullName;

        public string FullName
        {
            get { return fullName; }
            set { fullName = value; }
        }

        private Encoding fileEncoding;
        public Encoding FileEncoding
        {
            get { return fileEncoding; }
            set { fileEncoding = value; }
        }

        private DateTime lastWriteTime;

        public DateTime LastWriteTime
        {
            get { return lastWriteTime; }
            set { lastWriteTime = value; }
        }

        private long length;

        public long Length
        {
            get { return length; }
            set { length = value; }
        }

        private DateTime lastReadLogTime = new DateTime();

        public WatchingFile(FileInfo fileInfo, bool lineCountMode)
        {
            this.lineCountMode = lineCountMode;

            this.Initialize(fileInfo);
        }

        public WatchingFile(FileInfo fileInfo)
        {
            this.Initialize(fileInfo);
        }

        public List<string> GetNewLines()
        {
            List<string> newLines = new List<string>();

            if (HasBeenUpdated())
            {
                FileInfo info = new FileInfo(this.fullName);

                this.lastWriteTime = info.LastWriteTime;
                this.length = info.Length;

                if (this.lineCountMode)
                {
                    if (this.lastLineCount < 0)
                    {
                        ReadLastLine();
                        return newLines;
                    }

                    try
                    {
                        newLines = ReadLogWithLineCount(true);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("can't read " + this.fullName);
                    }
                }
                else
                {
                    if (this.lastReadLogTime == new DateTime())
                    {
                        ReadLastLogTime();
                        return newLines;
                    }

                    try
                    {
                        newLines = ReadLog(true);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("can't read " + this.fullName);
                    }
                }
            }

            return newLines;
        }

        private void Initialize(FileInfo fileInfo)
        {
            this.fullName = fileInfo.FullName;
            this.lastWriteTime = fileInfo.LastWriteTime;
            this.length = fileInfo.Length;
            DetectEncoding();
            if (FileEncoding != null)
            {
                if (this.lineCountMode)
                    ReadLastLine();
                else
                    ReadLastLogTime();
            }
        }

        private void DetectEncoding()
        {
            using (FileStream fs = new FileStream(
                this.fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);

                Encoding encode = EncodingDetector.DetectEncoding(bytes);
                FileEncoding = encode;
            }
        }

        private void ReadLastLogTime()
        {
            try
            {
                ReadLog(false);
            }
            catch (Exception)
            {
                Console.WriteLine("can't open " + this.fullName + ". Failed in first reading.");
            }
        }

        private void ReadLastLine()
        {
            try
            {
                ReadLogWithLineCount(false);
            }
            catch (Exception)
            {
                Console.WriteLine("can't open " + this.fullName + ". Failed in first reading.");
            }
        }

        private List<string> ReadLog(bool needsNewLines)
        {
            List<string> newLines = new List<string>();

            DateTime aDayAgo = DateTime.Now.AddDays(-1);
            DateTime lastLogTime = new DateTime();

            using (FileStream fs = new FileStream(
                this.fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs, FileEncoding))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match match = LogTime.Match(line);
                        if (!match.Success)
                            match = LogTime2.Match(line);

                        DateTime logTime;
                        if (match.Success)
                        {
                            logTime = MatchToDateTime(match);
                            lastLogTime = logTime;

                            if (needsNewLines)
                            {
                                // Get new lines, but drop too old lines.
                                if (logTime > this.lastReadLogTime && logTime > aDayAgo)
                                {
                                    newLines.Add(line);
                                }
                            }
                        }
                    }
                }
            }

            if (lastLogTime > this.lastReadLogTime)
                this.lastReadLogTime = lastLogTime;

            return needsNewLines ? newLines : null;
        }

        private List<string> ReadLogWithLineCount(bool needsNewLines)
        {
            List<string> newLines = new List<string>();

            long lineCount = 0;

            using (FileStream fs = new FileStream(
                this.fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs, FileEncoding))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineCount++;

                        if (needsNewLines)
                        {
                            if (lineCount > this.lastLineCount)
                            {
                                newLines.Add(line);
                            }
                        }
                    }
                }
            }

            // maybe the log was rotated...
            if (needsNewLines && lineCount < this.lastLineCount)
            {
                this.lastLineCount = 0;

                using (FileStream fs = new FileStream(
                    this.fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(fs, FileEncoding))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lineCount++;

                            if (needsNewLines)
                            {
                                if (lineCount > this.lastLineCount)
                                {
                                    newLines.Add(line);
                                }
                            }
                        }
                    }
                }
            }

            this.lastLineCount = lineCount;

            return needsNewLines ? newLines : null;
        }

        private bool HasBeenUpdated()
        {
            bool ret = false;

            FileInfo info = new FileInfo(this.fullName);
            if (info.LastWriteTime > this.lastWriteTime)
            {
                ret = true;
            }

            return ret;
        }

        private DateTime MatchToDateTime(Match match)
        {
            DateTime time;
            if (match != null)
            {
                time = new DateTime(
                   int.Parse(match.Groups[1].Value),
                   int.Parse(match.Groups[2].Value),
                   int.Parse(match.Groups[3].Value),
                   int.Parse(match.Groups[4].Value),
                   int.Parse(match.Groups[5].Value),
                   int.Parse(match.Groups[6].Value),
                   int.Parse(match.Groups[7].Value));
            }
            else
            {
                time = new DateTime();
            }

            return time;
        }
    }
}
