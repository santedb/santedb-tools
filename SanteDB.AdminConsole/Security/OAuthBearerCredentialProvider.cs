/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.AdminConsole.Util;
using SanteDB.Client.OAuth;
using SanteDB.Client.Services;
using SanteDB.Core.Http;
using SanteDB.Core.Security;
using SanteDB.Core.Security.OAuth;
using System;
using System.Security.Principal;

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

            if (AuthenticationContext.Current.Principal is OAuthClaimsPrincipal oacp)
            {
                //TODO: Validate that the token isn't expired.
                return new OAuthBearerTokenCredentials(oacp);
            }
            string mfaSecret = null;

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
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Password:{0}", new String('*', app.Configuration.Password.Length * 2));
                }

                // Now authenticate
                try
                {
                    var oauthclient = app.GetService<IOAuthClient>();

                    var principal = oauthclient.AuthenticateUser(app.Configuration.User, app.Configuration.Password, clientId: app.Configuration.AppId, tfaSecret: mfaSecret) as OAuthClaimsPrincipal;

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
                catch (RestClientException<OAuthTokenResponse> e)
                {
                    switch (e.Result.Error)
                    {
                        case "mfa_required":
                            mfaSecret = DisplayUtil.PasswordPrompt($"{e.Result.ErrorDescription}:");
                            break;
                        default:
                            app.Tracer.TraceError("Authentication error: {0} - {1}", e.Result.Error, e.Result.ErrorDescription);
                            app.Configuration.Password = null;
                            break;
                    }
                }
                catch (Exception e)
                {
                    app.Tracer.TraceError("Authentication error: {0}", e.Message);
                    app.Configuration.Password = null;
                }
            }

        }
    }
}
