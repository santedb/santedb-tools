/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.Applets.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.PakMan
{
    /// <summary>
    /// A class which packages files into a PAK file
    /// </summary>
    public class AppletPackager
    {

        // Manifest file
        private readonly string m_manifestFile;
        private readonly bool m_optimize;


        /// <summary>
        /// Create a new packager
        /// </summary>
        /// <param name="manifestFile">The manifest file</param>
        /// <param name="optimize">True if files should be optimized</param>
        public AppletPackager(String manifestFile, bool optimize)
        {
            this.m_manifestFile = manifestFile;
            this.m_optimize = optimize;
        }

        /// <summary>
        /// Package the manifest file into a package
        /// </summary>
        public AppletPackage Pack(string asVersion = null)
        {
            using (var fs = File.OpenRead(this.m_manifestFile))
            {
                AppletManifest mfst = AppletManifest.Load(fs);
                mfst.Assets.AddRange(this.ProcessDirectory(Path.GetDirectoryName(this.m_manifestFile), Path.GetDirectoryName(this.m_manifestFile)));
                foreach (var i in mfst.Assets)
                {
                    if (i.Name.StartsWith("/"))
                    {
                        i.Name = i.Name.Substring(1);
                    }
                }

                if (!string.IsNullOrEmpty(asVersion))
                {
                    mfst.Info.Version = asVersion;
                }
                mfst.Info.Version = PakManTool.ApplyVersion(mfst.Info.Version);
                mfst.Info.PublicKeyToken = null;
                var retVal = mfst.CreatePackage();
                retVal.Meta.Hash = SHA256.Create().ComputeHash(retVal.Manifest);
                return retVal;

            }
        }

        /// <summary>
        /// Sign the specified source package
        /// </summary>
        public AppletPackage Sign(AppletPackage sourcePackage, X509Certificate2 signCert)
        {
            if (!signCert.HasPrivateKey)
            {
                throw new InvalidOperationException($"You do not have the private key for certificiate {signCert.Subject}");
            }

            var manifest = sourcePackage.Unpack(); // Add timestamp and public key tokens to the manifest
            manifest.Info.TimeStamp = DateTime.Now; // timestamp
            manifest.Info.PublicKeyToken = signCert.Thumbprint;
            var retVal = manifest.CreatePackage();
            retVal.Meta.Hash = SHA256.Create().ComputeHash(retVal.Manifest); // Recompute hash
            retVal.Meta.PublicKeyToken = signCert.Thumbprint;
            retVal.PublicKey = signCert.Export(X509ContentType.Cert);
            RSACryptoServiceProvider rsa = signCert.PrivateKey as RSACryptoServiceProvider;
            retVal.Meta.Signature = rsa.SignData(retVal.Manifest, CryptoConfig.MapNameToOID("SHA1"));
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
            {
                if (!Path.GetFileName(dir).StartsWith("."))
                {
                    retVal.AddRange(ProcessDirectory(dir, path));
                }
                else
                {
                    Console.WriteLine("Skipping directory {0}", dir);
                }
            }

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
            {
                return null;
            }
            else
            {
                var asset = PakManTool.GetPacker(itm).Process(itm, this.m_optimize);
                asset.Name = PakManTool.TranslatePath(itm.Replace(basePath, ""));
                return asset;
            }

        }
    }
}
