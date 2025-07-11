﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2023-6-21
 */
using MohawkCollege.Util.Console.Parameters;
using SharpCompress.Readers.Tar;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SanteDB.SDK.BrainBug
{
    /// <summary>
    /// This is the brain bug... Just like in Starship troopers is
    /// sucks the brains out of the mobile application and extracts them
    /// onto the hardrive for debugging.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Suck the brains out of the app
        /// </summary>
        private static void Main(string[] args)
        {
            Console.WriteLine("SanteDB BrainBug - Android Extraction Tool");
            Console.WriteLine("Version {0}", Assembly.GetEntryAssembly().GetName().Version);
            Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);

            var parameters = new ParameterParser<ConsoleParameters>().Parse(args);

            if (parameters.Help || args.Length == 0)
            {
                new ParameterParser<ConsoleParameters>().WriteHelp(Console.Out);
                return;
            }
            if (parameters.TargetFile == null && parameters.ExtractDir == null)
            {
                Console.WriteLine("Either --tar or --extract must be specified");
                return;
            }

            if (parameters.PackageId != null)
            {
                var exeFile = Path.Combine(parameters.SdkPath, "platform-tools", "adb.exe");
                StringBuilder argStr = new StringBuilder();

                if (!String.IsNullOrEmpty(parameters.DeviceId))
                {
                    argStr.AppendFormat(" -s {0} ", parameters.DeviceId);
                }

                argStr.Append("backup ");

                argStr.AppendFormat("-f \"backup.ab\"", parameters.BackupFile);

                argStr.Append(" -noapk -noobb ");
                argStr.Append(parameters.PackageId);
                Console.WriteLine("Starting {0} {1}", exeFile, argStr.ToString());
                var pi = new Process();
                pi.StartInfo.FileName = String.Format("\"{0}\"", exeFile);
                pi.StartInfo.Arguments = argStr.ToString();
                pi.StartInfo.CreateNoWindow = true;
                pi.StartInfo.RedirectStandardError = true;
                pi.StartInfo.RedirectStandardOutput = true;
                pi.StartInfo.UseShellExecute = false;
                pi.Start();
                Console.WriteLine(pi.StandardOutput.ReadToEnd());
                Console.WriteLine(pi.StandardError.ReadToEnd());
                pi.WaitForExit();

                if (File.Exists(parameters.BackupFile))
                {
                    File.Delete(parameters.BackupFile);
                }

                File.Move("backup.ab", parameters.BackupFile);
            }

            if (!File.Exists(parameters.BackupFile))
            {
                Console.WriteLine("Cannot find specified backup file!");
                return;
            }

            try
            {
                Console.WriteLine("Extracting {0}...", parameters.BackupFile);
                byte[] buffer = new byte[8096];
                using (FileStream ins = File.OpenRead(parameters.BackupFile))
                {
                    ins.Read(buffer, 0, 24);
                    String magic = System.Text.Encoding.UTF8.GetString(buffer, 0, 24);
                    //ins.Seek(24, SeekOrigin.Begin);
                    using (FileStream outs = File.Create(parameters.TargetFile))
                    {
                        using (SharpCompress.Compressors.Deflate.ZlibStream df = new SharpCompress.Compressors.Deflate.ZlibStream(ins, SharpCompress.Compressors.CompressionMode.Decompress))
                        {
                            int br = 8096;
                            while (br == 8096)
                            {
                                br = df.Read(buffer, 0, 8096);
                                outs.Write(buffer, 0, br);
                            }
                        }
                    }
                }

                // Extract
                if (parameters.ExtractDir != null)
                {
                    if (!Directory.Exists(parameters.ExtractDir))
                    {
                        Directory.CreateDirectory(parameters.ExtractDir);
                    }

                    using (var fs = File.OpenRead(parameters.TargetFile))
                    using (var tar = TarReader.Open(fs))
                    {
                        while (tar.MoveToNextEntry())
                        {
                            string outName = Path.Combine(parameters.ExtractDir, tar.Entry.Key);
                            if (!Directory.Exists(Path.GetDirectoryName(outName)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(outName));
                            }

                            Console.WriteLine("{0} > {1}", tar.Entry.Key, outName);

                            if (!tar.Entry.IsDirectory)
                            {
                                using (var ofs = File.Create(outName))
                                {
                                    tar.WriteEntryTo(ofs);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}