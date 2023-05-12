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
