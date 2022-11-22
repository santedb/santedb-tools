using SanteDB.AdminConsole.Shell;
using SanteDB.AdminConsole.Util;
using SanteDB.Client.OAuth;
using SanteDB.Client.Services;
using SanteDB.Core.Http;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.AdminConsole.Security
{
    public class OAuthBearerCredentialProvider : ICredentialProvider
    {


        public OAuthBearerCredentialProvider()
        {

        }

        public RestRequestCredentials GetCredentials(IRestClient context) => GetCredentialsInternal();

        public RestRequestCredentials GetCredentials(IPrincipal principal) => GetCredentialsInternal();

        private RestRequestCredentials GetCredentialsInternal()
        {
            var app = Shell.ApplicationContext.Current;

            while (true)
            {
                Console.WriteLine("Access denied, authentication required.");
                if (String.IsNullOrEmpty(app.Configuration.User))
                {
                    Console.Write("Username:");
                    app.Configuration.User = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Username:{0}", app.Configuration.User);
                }

                if (String.IsNullOrEmpty(app.Configuration.Password))
                {
                    app.Configuration.Password = DisplayUtil.PasswordPrompt("Password:");
                    if (String.IsNullOrEmpty(app.Configuration.Password))
                    {
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("Password:{0}", new String('*', app.Configuration.Password.Length * 2));
                }

                // Now authenticate
                try
                {
                    var oauthclient = app.GetService<IOAuthClient>();

                    var principal = oauthclient.AuthenticateUser(app.Configuration.User, app.Configuration.Password) as OAuthClaimsPrincipal;

                    //var principal = (authenticationProvider as AdminConsole.Security.OAuthIdentityProvider)?.Authenticate(
                    //    new SanteDBClaimsPrincipal(new SanteDBClaimsIdentity(this.m_configuration.User, false, "OAUTH2")), this.m_configuration.Password) ??
                    //    authenticationProvider.Authenticate(this.m_configuration.User, this.m_configuration.Password);
                    if (principal != null)
                    {
                        app.TokenAuthContext = AuthenticationContext.EnterContext(principal);

                        return new OAuthBearerTokenCredentials(principal);
                    }
                    else
                    {
                        app.Configuration.Password = null;
                    }
                }
                catch (Exception e)
                {
                    app.Tracer.TraceError("Authentication error: {0}", e.Message);
                    app.Configuration.Password = null;
                }
            }

            return null;
        }
    }
}
