/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
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
 * DatERROR: 2021-8-27
 */
using SanteDB.PakMan.Repository;
using SanteDB.Core.Applets.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Represents a class which creates PAK files
    /// </summary>
    public class Packer
    {
        private PakManParameters m_parms;

        /// <summary>
        /// Creates a new packager
        /// </summary>
        public Packer(PakManParameters parms)
        {
            this.m_parms = parms;
        }

        /// <summary>
        /// Compile
        /// </summary>
        public int Compile()
        {
            int retVal = 0;
            // First is there a Manifest.xml?
            if (!Path.IsPathRooted(this.m_parms.Source))
                this.m_parms.Source = Path.Combine(Environment.CurrentDirectory, this.m_parms.Source);


            Console.WriteLine("Processing {0}...", this.m_parms.Source);

            String manifestFile = this.m_parms.Source;
            if (!File.Exists(manifestFile) && Directory.Exists(manifestFile))
                manifestFile = Path.Combine(this.m_parms.Source, "manifest.xml");

            if (!File.Exists(manifestFile))
                throw new InvalidOperationException($"Directory {this.m_parms.Source} must have manifest.xml");
            else
            {
                Console.WriteLine("\t Reading Manifest...", manifestFile);

                using (var fs = File.OpenRead(manifestFile))
                {
                    AppletManifest mfst = AppletManifest.Load(fs);
                    mfst.Assets.AddRange(this.ProcessDirectory(Path.GetDirectoryName(manifestFile), Path.GetDirectoryName(manifestFile)));
                    foreach (var i in mfst.Assets)
                        if (i.Name.StartsWith("/"))
                            i.Name = i.Name.Substring(1);

                    if (!string.IsNullOrEmpty(this.m_parms.Version))
                        mfst.Info.Version = this.m_parms.Version;
                    mfst.Info.Version = PakManTool.ApplyVersion(mfst.Info.Version);

                    if (!Directory.Exists(Path.GetDirectoryName(this.m_parms.Output)) && !String.IsNullOrEmpty(Path.GetDirectoryName(this.m_parms.Output)))
                        Directory.CreateDirectory(Path.GetDirectoryName(this.m_parms.Output));

                    AppletPackage pkg = null;

                    // Is there a signature?
                    if (this.m_parms.Sign)
                    {
                        pkg = new Signer(this.m_parms).CreateSignedPackage(mfst);
                        if (pkg == null) return -102;
                    }
                    else
                    {
                        Emit.Message("WARN", "THIS PACKAGE IS NOT SIGNED - MOST OPEN IZ TOOLS WILL NOT LOAD IT");
                        mfst.Info.PublicKeyToken = null;
                        pkg = mfst.CreatePackage();
                        //pkg.Meta.PublicKeyToken = null;
                    }
                    pkg.Meta.Hash = SHA256.Create().ComputeHash(pkg.Manifest);

                    var outFile = this.m_parms.Output ?? mfst.Info.Id + ".pak";
                    using (var ofs = File.Create(outFile))
                        pkg.Save(ofs);

                    if (this.m_parms.Install)
                    {
                        Emit.Message("INFO", "INSTALLING PACKAGE {0}", pkg.Meta.Id);
                        PackageRepositoryUtil.InstallCache(pkg);
                    }
                    if (this.m_parms.Publish)
                    {
                        try
                        {
                            Emit.Message("INFO", "PUBLISHING PACKAGE TO {0}", this.m_parms.PublishServer);
                            PackageRepositoryUtil.Publish(this.m_parms.PublishServer, pkg);
                        }
                        catch (Exception e)
                        {
                            Emit.Message("ERROR", "ERROR PUBLISHING PACKAGE - {0}", e.Message);
                        }
                    }
                }
            }

            return retVal;
        }


        /// <summary>
        /// Process the specified directory
        /// </summary>
        public IEnumerable<AppletAsset> ProcessDirectory(string source, String path)
        {
            List<AppletAsset> retVal = new List<AppletAsset>();
            foreach (var itm in Directory.GetFiles(source))
            {
                if (Path.GetFileName(itm).StartsWith("."))
                {
                    Console.WriteLine("\t Skipping {0}...", itm);
                    continue;
                }
                retVal.Add(this.ProcessFile(itm, path));
            }

            // Process sub directories
            foreach (var dir in Directory.GetDirectories(source))
                if (!Path.GetFileName(dir).StartsWith("."))
                    retVal.AddRange(ProcessDirectory(dir, path));
                else
                    Console.WriteLine("Skipping directory {0}", dir);

            return retVal.OfType<AppletAsset>();
        }

        /// <summary>
        /// Process the specified file
        /// </summary>
        /// <param name="itm"></param>
        /// <returns></returns>
        private AppletAsset ProcessFile(string itm, String basePath)
        {
            if (Path.GetFileName(itm).ToLower() == "manifest.xml")
                return null;
            else
            {
                Emit.Message("INFO", " Processing file {0}...", itm);
                var asset = PakManTool.GetPacker(itm).Process(itm, this.m_parms.Optimize);
                asset.Name = PakManTool.TranslatePath(itm.Replace(basePath, ""));
                return asset;
            }

        }


    }
}
