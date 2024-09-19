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
 */
using SanteDB.Core.Applets.Model;
using SanteDB.PakMan.Repository;
using System;
using System.IO;

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
            {
                this.m_parms.Source = Path.Combine(Environment.CurrentDirectory, this.m_parms.Source);
            }

            Console.WriteLine("Processing {0}...", this.m_parms.Source);

            String manifestFile = this.m_parms.Source;
            if (!File.Exists(manifestFile) && Directory.Exists(manifestFile))
            {
                manifestFile = Path.Combine(this.m_parms.Source, "manifest.xml");
            }

            if (!File.Exists(manifestFile))
            {
                throw new InvalidOperationException($"Directory {this.m_parms.Source} must have manifest.xml");
            }
            else
            {
                var packer = new AppletPackager(manifestFile, this.m_parms.Optimize);
                AppletPackage pkg = packer.Pack(this.m_parms.Version);
                if (this.m_parms.Sign)
                {
                    pkg = packer.Sign(pkg, this.m_parms.GetSigningCert());
                }
                else
                {
                    Emit.Message("WARN", "This package is not signed, production release tools may not load it!");
                }

                if (!Directory.Exists(Path.GetDirectoryName(this.m_parms.Output)) && !String.IsNullOrEmpty(Path.GetDirectoryName(this.m_parms.Output)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(this.m_parms.Output));
                }

                var outFile = this.m_parms.Output ?? pkg.Meta.Id + ".pak";
                using (var ofs = File.Create(outFile))
                {
                    pkg.Save(ofs);
                }

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

            return retVal;
        }




    }
}
