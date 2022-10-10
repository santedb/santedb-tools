/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core;
using SanteDB.Core.Http;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.AdminConsole.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;

using System.Security.Principal;

namespace SanteDB.AdminConsole.Security
{
    /// <summary>
    /// Represents a credential provider which provides a token
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TokenCredentialProvider : ICredentialProvider, IDisposable
    {

        // Token cred
        private IDisposable m_tokenCred = null;

        #region ICredentialProvider implementation
        /// <summary>
        /// Gets or sets the credentials which are used to authenticate
        /// </summary>
        /// <returns>The credentials.</returns>
        /// <param name="context">Context.</param>
        public Credentials GetCredentials(IRestClient context)
        {
            if (!AuthenticationContext.Current.Principal.Identity.IsAuthenticated ||
                AuthenticationContext.Current.Principal == AuthenticationContext.AnonymousPrincipal)
            {
                var credentials = this.Authenticate(context);
                this.m_tokenCred = AuthenticationContext.EnterContext(credentials.Principal);
                return credentials;
            }
            else
            {
                return this.GetCredentials(AuthenticationContext.Current.Principal);
            }
        }

        /// <summary>
        /// Authenticate a user - this occurs when reauth is required
        /// </summary>
        /// <param name="context">Context.</param>
        public Credentials Authenticate(IRestClient context)
        {

            // TODO: Determine why we're reauthenticating... if it is an expired token we'll need to get the refresh token
            var tokenCredentials = AuthenticationContext.Current.Principal as TokenClaimsPrincipal;
            if (tokenCredentials != null)
            {
                var expiryTime = DateTime.MinValue;
                if (DateTime.TryParse(tokenCredentials.FindFirst(SanteDBClaimTypes.Expiration).Value, out expiryTime) &&
                    expiryTime < DateTime.Now)
                {
                    var idp = ApplicationContext.Current.GetService(typeof(IIdentityProviderService)) as IIdentityProviderService;
                    var principal = idp.Authenticate(null, null);   // Force a re-issue
                    this.m_tokenCred = AuthenticationContext.EnterContext(principal);

                }
                else if (expiryTime > DateTime.Now) // Token is good?
                {
                    return this.GetCredentials(context);
                }
                else // I don't know what happened
                {
                    throw new SecurityException();
                }
            }
            else
            {
                if ((ApplicationServiceContext.Current as ApplicationContext).Authenticate(new OAuthIdentityProvider(), context))
                {
                    return this.GetCredentials(AuthenticationContext.Current.Principal);
                }
            }
            return null;

        }

        /// <summary>
        /// Get credentials from the specified principal
        /// </summary>
        public Credentials GetCredentials(IPrincipal principal)
        {
            if (principal is TokenClaimsPrincipal)
            {
                return new TokenCredentials(principal);
            }
            else
            {
                // We need a token claims principal
                // TODO: Re-authenticate this user against the ACS
                return new TokenCredentials(AuthenticationContext.Current.Principal);
            }
        }
        #endregion

        /// <summary>
        /// Chain disposal
        /// </summary>
        public void Dispose()
        {
            this.m_tokenCred?.Dispose();
        }
    }

}

