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
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI.Security;
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
    /// Certificate commandlet
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class CertCmdlet
    {

        public class CertQueryOptions
        {
            [Parameter("store")]
            [Description("The store name (My, Trusted, etc.)")]
            public String StoreName { get; set; }

            [Parameter("pk")]
            [Description("Only certificates with private keys")]
            public bool HasPrivateKey { get; set; }

            [Parameter("subject")]
            [Description("Filter by subject name")]
            public string SubjectName { get; set; }
        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// Query for certificates
        /// </summary>
        [AdminCommand("cert.list", "List certificates known to the iCDR/dCDR server")]
        public static void Query(CertQueryOptions certQueryOptions)
        {

            var query = new NameValueCollection();
            if (String.IsNullOrEmpty(certQueryOptions.StoreName))
            {
                query.Add("storeName", "My");
            }
            else
            {
                query.Add("storeName", certQueryOptions.StoreName);
            }

            if (certQueryOptions.HasPrivateKey)
            {
                query.Add("hasPrivateKey", "true");
            }

            if (!String.IsNullOrEmpty(certQueryOptions.SubjectName))
            {
                query.Add("subject", certQueryOptions.SubjectName);
            }

            var certs = m_client.Client.Get<AmiCollection>($"/Certificate", query);
            DisplayUtil.TablePrint(certs.CollectionItem.OfType<X509Certificate2Info>(),
                new String[] { "ID", "Subject", "Thumbrint", "Expires", "Key", "Valid" },
                new int[] { 5, 54, 44, 25, 4, 5 },
                o => o.Id,
                o => o.Subject,
                o => o.Thumbprint,
                o => o.NotAfter,
                o => o.HasPrivateKey ? "*" : "",
                o => o.IsValid ? "*" : ""
                );
        }

        public class CertGetOptions
        {
            [Parameter("store")]
            [Description("The store name (My, Trusted, etc.)")]
            public String StoreName { get; set; }

            [Parameter("thumb")]
            [Parameter("*")]
            [Description("Thumbprint of the certificate")]
            public StringCollection Thumbprint { get; set; }
        }

        /// <summary>
        /// Query for certificates
        /// </summary>
        [AdminCommand("cert.get", "Get certificates known to the iCDR/dCDR server")]
        public static void Query(CertGetOptions certGetOptions)
        {
            if (certGetOptions.Thumbprint.Count == 0)
            {
                throw new ArgumentNullException("thumb parameter is required");
            }
            var query = new NameValueCollection();
            if (String.IsNullOrEmpty(certGetOptions.StoreName))
            {
                query.Add("storeName", "My");
            }
            else
            {
                query.Add("storeName", certGetOptions.StoreName);
            }

            using (var ms = new MemoryStream(m_client.Client.Get($"/Certificate/{certGetOptions.Thumbprint[0]}", query)))
            {
                using (var sr = new StreamReader(ms))
                {
                    Console.WriteLine(sr.ReadToEnd());
                }
            }

        }
    }
}
