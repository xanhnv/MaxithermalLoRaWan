﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPServerAndWebSocketClient
{
    public class Utilities
    {
        public static void WriteLogError(Exception ex)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFile.txt", true);
                sw.WriteLine(DateTime.Now.ToString("g") + ": " + ex.Source + "; " + ex.Message);
                sw.Flush();
                sw.Close();
            }
            catch
            {
                // ignored
            }
        }
        public static void WriteLog(string message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFile.txt", true);
                sw.WriteLine(DateTime.Now.ToString("g") + ": " + message);
                sw.Flush();
                sw.Close();
            }
            catch
            {
                // ignored
            }
        }
        public static void WriteLogDebug(string message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFileDebug.csv", true);
                sw.WriteLine(message);
                sw.Flush();
                sw.Close();
            }
            catch
            {
                // ignored
            }
        }
        public static void WriteLogPackType(string message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFilePacketType.csv", true);
                sw.WriteLine(message);
                sw.Flush();
                sw.Close();
            }
            catch
            {
                // ignored
            }
        }

        public static void WriteLogError(string message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFile.txt", true);
                sw.WriteLine(DateTime.Now.ToString("g") + ": " + message);
                sw.Flush();
                sw.Close();
            }
            catch
            {
                // ignored
            }
        }
    }
}
