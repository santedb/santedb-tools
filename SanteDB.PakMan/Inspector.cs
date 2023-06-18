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
 * Date: 2023-5-19
 */
using SanteDB.Core.Applets.Model;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Inspect a package
    /// </summary>
    internal class Inspector
    {
        private readonly AppletPackage m_applet;

        public Inspector(PakManParameters parameters)
        {
            if (!File.Exists(parameters.Source))
            {
                throw new FileNotFoundException(parameters.Source);
            }
            using (var fs = File.OpenRead(parameters.Source))
            {
                this.m_applet = AppletPackage.Load(fs);
            }
        }

        private Inspector(AppletPackage package)
        {
            this.m_applet = package;
        }

        /// <summary>
        /// Dump the inforation for the package
        /// </summary>
        internal int Dump()
        {
            Console.WriteLine("Package Type: {0}", this.m_applet.GetType().Name);
            Console.WriteLine("ID: {0}", this.m_applet.Meta.Id);
            Console.WriteLine("Version: {0}", this.m_applet.Meta.Version);
            Console.WriteLine("Author: {0}", this.m_applet.Meta.Author);
            Console.WriteLine("Name(s): {0}", string.Join("\r\n\t", this.m_applet.Meta.Names.Select(o => o.Value)));
            Console.WriteLine("Public Key ID: {0}", this.m_applet.Meta.PublicKeyToken);
            Console.WriteLine("Content Hash: {0}", this.m_applet.Meta.Hash.HexEncode());
            Console.WriteLine("Timestamp: {0}", this.m_applet.Meta.TimeStamp?.ToString("o") ?? "none");
            if (this.m_applet.PublicKey != null)
            {
                var cert = new X509Certificate2(this.m_applet.PublicKey);
                Console.WriteLine("=== Embedded Publisher Information ===");
                Console.WriteLine("SN: {0}", cert.Subject);
                Console.WriteLine("TUMB: {0}", cert.Thumbprint);
                Console.WriteLine("ISSUER: {0}", cert.Issuer);
                Console.WriteLine("VALIDITY: {0} THRU {1}", cert.NotBefore, cert.NotAfter);
            }

            if (this.m_applet is AppletSolution sln)
            {
                int i = 1;
                Console.WriteLine("-- INCLUDES --");
                foreach (var itm in sln.Include)
                {
                    Console.Write("\t{0} - {1} v. {2}", i++, itm.Meta.Id, itm.Meta.Version);
                    if (itm.PublicKey != null)
                    {
                        var cert = new X509Certificate2(itm.PublicKey);
                        Console.Write(", {0}", cert.Subject);
                    }
                    Console.WriteLine();

                }
            }
            else 
            {
                Console.WriteLine("-- CONTENTS --");
                Console.WriteLine("CLASS\tMime Type\t\tName");
                foreach(var itm in this.m_applet.Unpack().Assets)
                {
                    switch(itm.Content)
                    {
                        case AppletWidget w:
                            Console.Write("WIDGET");
                            break;
                        case AppletAssetHtml h:
                            Console.Write("HTML");
                            break;
                        case byte[] b:
                            Console.Write("BINARY");
                            break;
                        case AppletAssetVirtual v:
                            Console.Write("VIRTUAL");
                            break;
                        case XElement x:
                            Console.Write("XML");
                            break;
                        case string s:
                            Console.Write("TEXT");
                            break;
                    }
                    Console.WriteLine("\t{1}\t\t{2}", itm.Content.GetType().Name, itm.MimeType, itm.Name);
                }
            }
            return 0;

        }
    }
}