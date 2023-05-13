/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core.Applets.Model;
using SanteDB.PakMan.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SanteDB.PakMan
{
    /// <summary>
    /// A tool that composes data
    /// </summary>
    public class Composer
    {
        private PakManParameters m_parms;

        /// <summary>
        /// Creates a new composer
        /// </summary>
        /// <param name="parms"></param>
        public Composer(PakManParameters parms)
        {
            this.m_parms = parms;
        }

        /// <summary>
        /// Compose multiple PAK files into a solution
        /// </summary>
        public int Compose()
        {
            try
            {
                AppletManifest mfst = null;
                using (FileStream fs = File.OpenRead(this.m_parms.Source))
                    mfst = AppletManifest.Load(fs);

                var slnPak = mfst.CreatePackage();

                AppletSolution sln = new AppletSolution();
                sln.Meta = slnPak.Meta;
                sln.PublicKey = slnPak.PublicKey;
                sln.Manifest = slnPak.Manifest;

                if (sln.Meta.Uuid == Guid.Empty)
                    Emit.Message("WARN", "The package does not carry a UUID! You should add a UUID to your solution manifest");
                sln.Include = new List<AppletPackage>();

                foreach (var pfile in sln.Meta.Dependencies.ToArray())
                {
                    AppletPackage pkg = null;
                    if (!String.IsNullOrEmpty(pfile.Version)) // specific version
                    {
                        pkg = PackageRepositoryUtil.GetFromAny(pfile.Id, new Version(pfile.Version));
                    }
                    else if (!String.IsNullOrEmpty(m_parms.Version))
                    {
                        pkg = PackageRepositoryUtil.GetFromAny(pfile.Id, new Version(m_parms.Version))
                            ?? PackageRepositoryUtil.GetFromAny(pfile.Id, null);
                    }
                    else
                    {
                        pkg = PackageRepositoryUtil.GetFromAny(pfile.Id, null);
                    }

                    if (pkg == null)
                        throw new KeyNotFoundException($"Package {pfile.Id} ({pfile.Version ?? m_parms.Version ?? "latest"}) not found");
                    else
                    {
                        Emit.Message("INFO", "Including {0} version {1}..", pkg.Meta.Id, pkg.Meta.Version);
                        sln.Meta.Dependencies.RemoveAll(o => o.Id == pkg.Meta.Id);

                        if (this.m_parms.Sign && pkg.Meta.Signature == null)
                        {
                            Emit.Message("WARN", "Package {0} is not signed, but you're signing your package. We'll sign it using your key", pkg.Meta.Id);
                            pkg = new Signer(this.m_parms).CreateSignedPackage(pkg);
                        }
                        sln.Include.Add(pkg);
                    }
                }

                // Emit i18n file?
                if (!String.IsNullOrEmpty(this.m_parms.InternationalizationFile))
                {
                    Emit.Message("INFO", $"Writing string manifest to {this.m_parms.InternationalizationFile}");
                    using (var fs = File.Create(this.m_parms.InternationalizationFile))
                    using (var tw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                    {
                        // tx translations
                        var mfsts = sln.Include.Select(o => o.Unpack()).ToList();
                        var appletStrings = mfsts.SelectMany(o => o.Strings).ToArray();
                        var stringKeys = appletStrings.SelectMany(o => o.String).Select(o => o.Key).Distinct();
                        var langs = appletStrings.Select(o => o.Language).Distinct().ToArray();
                        tw.Write("key,");
                        tw.WriteLine(String.Join(",", langs));

                        foreach (var str in stringKeys)
                        {
                            tw.Write($"{str},");
                            foreach (var lang in langs)
                            {
                                tw.Write($"\"{appletStrings.Where(o => o.Language == lang).SelectMany(s => s.String).FirstOrDefault(o => o.Key == str)?.Value}\",");
                            }
                            tw.WriteLine();
                        }
                    }
                }

                sln.Meta.Hash = SHA256.Create().ComputeHash(sln.Include.SelectMany(o => o.Manifest).ToArray());
                // Sign the signature package
                if (this.m_parms.Sign)
                    new Signer(this.m_parms).CreateSignedSolution(sln);

                // Now save
                using (FileStream fs = File.Create(this.m_parms.Output ?? Path.ChangeExtension(sln.Meta.Id, ".sln.pak")))
                    sln.Save(fs);

                return 0;
            }
            catch (System.Exception e)
            {
                Emit.Message("ERROR", e.Message);
                //Console.Error.WriteLine("Cannot compose solution {0}: {1}", this.m_parms.Source, e);
                return -1;
            }
        }
    }
}