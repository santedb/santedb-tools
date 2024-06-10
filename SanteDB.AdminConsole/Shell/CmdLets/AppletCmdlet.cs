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
using MohawkCollege.Util.Console.Parameters;
using SanteDB.AdminConsole.Attributes;
using SanteDB.AdminConsole.Util;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Messaging.AMI.Client;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace SanteDB.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Applet commands
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class AppletCmdlet
    {

        /// <summary>
        /// Upload applet parameter
        /// </summary>
        public class AppletUploadParameter
        {
            [Parameter("file")]
            [Description("The applet file to upload to the server")]
            public StringCollection AppletFile { get; set; }
        }

        /// <summary>
        /// Represents an applet parameter
        /// </summary>
        public class AppletParameter
        {

            /// <summary>
            /// Applet identifier
            /// </summary>
            [Parameter("*")]
            [Description("The identifier for the applet to stat")]
            public StringCollection AppletId { get; set; }

        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        static AppletCmdlet()
        {
            m_client.Client.ProgressChanged += (o, e) =>
            {
                Console.CursorLeft = 1;
                Console.Write("{0:%}", e.Progress);
            };
        }

        /// <summary>
        /// List applets
        /// </summary>
        [AdminCommand("applet.list", "Lists all applets installed on the server")]
        public static void ListApplets()
        {
            var applets = m_client.GetApplets();
            DisplayUtil.TablePrint(applets.CollectionItem.OfType<AppletManifestInfo>(),
                new String[] { "ID", "Name", "Version", "Publisher", "Sig" },
                new int[] { 25, 25, 10, 58, 10 },
                o => o.AppletInfo.Id,
                o => o.AppletInfo.Names.FirstOrDefault().Value,
                o => o.AppletInfo.Version,
                o => o.AppletInfo.Author,
                o => o.PublisherData != null ? "Signed" : "Unsigned"
            );

        }

        /// <summary>
        /// List applets
        /// </summary>
        [AdminCommand("solution.list", "Lists all solutions installed on the server")]
        public static void ListSolutions()
        {
            var applets = m_client.GetAppletSolutions();
            DisplayUtil.TablePrint(applets.CollectionItem.OfType<AppletManifestInfo>(),
                new String[] { "ID", "Name", "Version", "Publisher", "Sig" },
                new int[] { 25, 25, 10, 58, 10 },
                o => o.AppletInfo.Id,
                o => o.AppletInfo.Names.FirstOrDefault().Value,
                o => o.AppletInfo.Version,
                o => o.AppletInfo.Author,
                o => o.PublisherData != null ? "Signed" : "Unsigned"
            );

        }

        /// <summary>
        /// Upload an applet
        /// </summary>
        [AdminCommand("applet.upload", "Upload an applet to the default solution binding")]
        public static void UploadApplet(AppletUploadParameter parms)
        {
            foreach (var itm in parms.AppletFile)
            {
                if (!File.Exists(itm))
                {
                    throw new FileNotFoundException(itm);
                }

                using (var fs = File.OpenRead(itm))
                {
                    var package = AppletPackage.Load(fs);
                    var endpointName = package is AppletSolution ? "AppletSolution" : "Applet";
                    using (var ms = new MemoryStream())
                    {
                        Console.Write("Uploading {0} v{1}...", package.Meta.Id, package.Meta.Version);
                        package.Save(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        var result = m_client.Client.Invoke<Stream, Object>("POST", endpointName, "application/octet-stream", ms);
                        if (result is AppletSolutionInfo asi)
                        {
                            Console.WriteLine("OK - {0}", asi.AppletInfo.Id);
                        }
                        else if (result is AppletInfo ai)
                        {
                            Console.WriteLine("OK - {0}", ai.Id);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Get a specific applet information
        /// </summary>
        [AdminCommand("applet.download", "Download the applet")]
        public static void GetApplet(AppletParameter parms)
        {
            foreach (var itm in parms.AppletId)
            {
                Console.Write("(    )   Downloading {0} > {0}.pak", itm);
                using (var rmtstream = m_client.DownloadApplet(itm))
                using (var stream = File.Create(itm + ".pak"))
                {
                    rmtstream.CopyTo(stream);
                }

                Console.CursorLeft = 1;
                Console.Write("100%");
                Console.WriteLine();
            }


        }
    }
}
