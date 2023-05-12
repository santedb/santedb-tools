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
using System.Linq;

namespace SanteDB.AdminConsole.Shell.CmdLets
{
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class SessionCmdlet
    {
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// WHO information
        /// </summary>
        [AdminCommand("session.list", "Displays all active sessions which are logged into the server")]
        internal static void Who()
        {

            var sessions = m_client.Client.Get<AmiCollection>("/SessionInfo");
            DisplayUtil.TablePrint(sessions.CollectionItem.OfType<SecuritySessionInfo>(),
                new String[] { "ID", "User", "Application", "Device", "Established", "Expires", "IP Address" },
                new int[] { 30, 26, 32, 32, 22, 22, 10 },
                o => o.SessionId,
                o => o.User,
                o => o.Application,
                o => o.Device,
                o => o.NotBefore,
                o => o.NotAfter,
                o => o.RemoteEndpoint
                );

        }

        internal class KillSessionParameter
        {
            [Parameter("user")]
            [Description("Kill all sessions for user")]
            public String User { get; set; }

            [Parameter("*")]
            [Parameter("id")]
            [Description("Kills a specific session")]
            public StringCollection SessionId { get; set; }

        }

        /// <summary>
        /// Kill a session
        /// </summary>
        [AdminCommand("session.kill", "Kills one or more sessions according to the session parameters")]
        internal static void Kill(KillSessionParameter killSession)
        {

            if (!String.IsNullOrEmpty(killSession.User))
            {
                var query = new NameValueCollection();
                query.Add("userIdentity", killSession.User);
                var sessions = m_client.Client.Get<AmiCollection>("/SessionInfo", query).CollectionItem.OfType<SecuritySessionInfo>().Select(o => o.SessionId);
                killSession.SessionId = new StringCollection();
                killSession.SessionId.AddRange(sessions.Select(o => o.HexEncode()).ToArray());
            }
            foreach (var itm in killSession.SessionId)
            {
                Console.Write("\tKilling {0}.", itm);
                m_client.Client.Delete<SecuritySessionInfo>($"/SessionInfo/{itm}");
                Console.WriteLine("OK");
            }
        }
    }
}
