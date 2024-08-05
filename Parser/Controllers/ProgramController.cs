﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Parser.Localization;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Parser.Controllers
{
    public static class ProgramController
    {
        public const string AssemblyVersion = "0.1.0";
        public static readonly string Version = $"v{AssemblyVersion}";
        public const bool IsBetaVersion = false;
        public const string ParameterPrefix = "--";

        public static string ResourceDirectory;
        public static string LogLocation;

        /// <summary>
        /// Initializes the server IPs matching with the
        /// current server depending on the chosen locale
        /// and determines the newest log file if multiple
        /// server IPs are used to connect to the server
        /// </summary>
        public static void InitializeServerIp()
        {
            try
            {
                ResourceDirectory = "Not Found";
                LogLocation = $"client_resources\\aa6536182e6900af6ac8e0b4ee86e312\\.storage";

                // Return if the user has not picked
                // a RAGEMP directory path yet
                string directoryPath = Properties.Settings.Default.DirectoryPath;
                if (string.IsNullOrWhiteSpace(directoryPath)) return;

                // Get every directory in the client_resources directory found inside directoryPath
                string[] resourceDirectories = Directory.GetDirectories(directoryPath + @"\client_resources");

                // Store each GTA W .storage file path in a List (found by a tag in the .storage file)
                List<string> potentialLogs = new List<string>();
                foreach (string resourceDirectory in resourceDirectories)
                {
                    if (!File.Exists(resourceDirectory + @"\.storage"))
                        continue;

                    string log;
                    using (StreamReader sr = new StreamReader(resourceDirectory + @"\.storage"))
                    {
                        log = sr.ReadToEnd();
                    }

                    if (!Regex.IsMatch(log, "\\\"server_version\\\":\\\"GTA World[^\"]*\""))
                        continue;

                    potentialLogs.Add(resourceDirectory);
                }

                if (potentialLogs.Count == 0) return;

                // Compare the last write time on all .storage files in the List to find the latest one
                foreach (var file in potentialLogs.Select(log => new FileInfo(log + @"\.storage")))
                {
                    file.Refresh();
                }

                while (potentialLogs.Count > 1)
                {
                    potentialLogs.Remove(DateTime.Compare(File.GetLastWriteTimeUtc(potentialLogs[0] + @"\.storage"), File.GetLastWriteTimeUtc(potentialLogs[1] + @"\.storage")) > 0 ? potentialLogs[1] : potentialLogs[0]);
                }

                int finalSeparator = potentialLogs[0].LastIndexOf(@"\", StringComparison.Ordinal);
                if (finalSeparator == -1) return;

                // Finally, set the log location
                ResourceDirectory = potentialLogs[0].Substring(finalSeparator + 1, potentialLogs[0].Length - finalSeparator - 1);
                LogLocation = $"client_resources\\{ResourceDirectory}\\.storage";
            }
            catch
            {
                // Silent exception
            }
        }

        /// <summary>
        /// Parses the most recent chat log found at the
        /// selected RAGEMP directory path and returns it.
        /// Displays an error if a chat log does not
        /// exist or if it has an incorrect format
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="removeTimestamps"></param>
        /// <returns></returns>
        public static string ParseChatLog(string directoryPath, bool removeTimestamps)
        {
            try
            {
                // Read the chat log
                string log;
                using (StreamReader sr = new StreamReader(directoryPath + LogLocation))
                {
                    log = sr.ReadToEnd();
                }

                log = Regex.Match(log, "(?<=chat_log\\\":\\\")(.*?)(?=\\\\n\\\",\\\"emittersVolume)").Value;

                if (string.IsNullOrWhiteSpace(log))
                    throw new IndexOutOfRangeException();


                log = System.Net.WebUtility.HtmlDecode(log);
                log = log.Replace("\\n", "\n");

                if (removeTimestamps)
                    log = Regex.Replace(log, @"\[\d{1,2}:\d{1,2}:\d{1,2}\] ", string.Empty);

                return log;
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                MessageBox.Show(Strings.ParseError, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }
    }
}
