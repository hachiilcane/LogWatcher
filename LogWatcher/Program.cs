﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LogWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Crawler watcher = new Crawler();
            watcher.Do();
        }
    }
}
