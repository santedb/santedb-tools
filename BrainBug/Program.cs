/*
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
using SanteDB.Core.Data.Backup;
using SharpCompress.Readers.Tar;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SanteDB;


namespace SanteDB.SDK.BrainBug
{
    /// <summary>
    /// This is the brain bug... Just like in Starship troopers is
    /// sucks the brains out of the mobile application and extracts them
    /// onto the hardrive for debugging.
    /// </summary>
    internal class Program
    {

        private static readonly Guid[] m_databaseAssets =
        {
            Guid.Parse("FB444942-4276-427C-A09C-9C65769837F0"),
            Guid.Parse("EFF684F2-7641-4697-A4A0-CA0F5171BAA7"),
            Guid.Parse("3E3C4EF4-5C64-4EC6-BB82-26719F09F8B5")
        };

        /// <summary>
        /// Suck the brains out of the app
        /// </summary>
        private static void Main(string[] args)
        {
            Console.WriteLine("SanteDB BrainBug - Android / SanteDB Extraction Tool");
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

            // Determine the file format 
            using (var fs = File.OpenRead(parameters.BackupFile))
            {
                var magicBuffer = new Byte[BackupReader.MAGIC.Length];
                fs.Read(magicBuffer, 0, magicBuffer.Length);
                if (BackupReader.MAGIC.SequenceEqual(magicBuffer))
                {
                    fs.Close();
                    ProcessSanteDBBackup(parameters);
                }
                else
                {
                    fs.Close();
                    ProcessAndroidBackup(parameters);
                }
            }

        }

        /// <summary>
        /// Process SanteDB Backup file
        /// </summary>
        private static void ProcessSanteDBBackup(ConsoleParameters parameters)
        {
            if (!Directory.Exists(parameters.ExtractDir))
            {
                Directory.CreateDirectory(parameters.ExtractDir);
            }

            using (var fs = File.OpenRead(parameters.BackupFile))
            {
                using (var br = BackupReader.Open(fs, parameters.Password))
                {
                    while (br.GetNextEntry(out var backupAsset))
                    {
                        using (var ins = backupAsset.Open())
                        {
                            string name = backupAsset.Name;
                            // For databases we skip the password 
                            if (m_databaseAssets.Contains(backupAsset.AssetClassId)) // this is actually several files
                            {
                                name = ins.ReadPascalString();

                                var lengthBuf = new byte[8];
                                ins.Read(lengthBuf, 0, 8);
                                var assetSize = BitConverter.ToInt64(lengthBuf, 0);
                                var bytesRead = 0;
                                var targetFile = Path.Combine(parameters.ExtractDir, name);
                                Console.WriteLine("Extracting {0} (type {1}) to {2}", backupAsset.Name, backupAsset.AssetClassId, targetFile);
                                using (var outs = File.Create(targetFile))
                                {
                                    while (bytesRead < assetSize)
                                    {
                                        var assetBuffer = new byte[assetSize - bytesRead > 16_384 ? 16_384 : assetSize - bytesRead];
                                        bytesRead += ins.Read(assetBuffer, 0, assetBuffer.Length);
                                        outs.Write(assetBuffer, 0, assetBuffer.Length);
                                    }
                                }
                            }
                            else
                            {
                                var targetFile = Path.Combine(parameters.ExtractDir, name);
                                Console.WriteLine("Extracting {0} (type {1}) to {2}", backupAsset.Name, backupAsset.AssetClassId, targetFile);
                                using (var outs = File.Create(targetFile))
                                {
                                    ins.CopyTo(outs);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process an adroid backup
        /// </summary>
        private static void ProcessAndroidBackup(ConsoleParameters parameters)
        {
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