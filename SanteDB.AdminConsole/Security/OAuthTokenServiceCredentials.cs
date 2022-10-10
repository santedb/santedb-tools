﻿/*
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
using SanteDB.Core.Http;
using SanteDB.Core.Security.Claims;
using SanteDB.AdminConsole.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace SanteDB.AdminConsole.Security
{
    /// <summary>
    /// Represents credentials for this android application on all requests going to the OAuth service
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OAuthTokenServiceCredentials : Credentials
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SanteDB.AdminConsole.Security.OAuthTokenServiceCredentials"/> class.
        /// </summary>
        /// <param name="principal">Principal.</param>
        public OAuthTokenServiceCredentials(IPrincipal principal) : base(principal)
        {
        }

        #region implemented abstract members of Credentials

        /// <summary>
        /// Get the http headers which are requried for the credential
        /// </summary>
        /// <returns>The http headers.</returns>
        public override System.Collections.Generic.Dictionary<string, string> GetHttpHeaders()
        {
            // App ID credentials
            String appAuthString = String.Format("{0}:{1}",
                ApplicationContext.Current.ApplicationName,
                ApplicationContext.Current.ApplicationSecret);

            // TODO: Add claims
            List<IClaim> claims = new List<IClaim>()
            {
            };

            //// Additional claims?
            //if (this.Principal is IClaimsPrincipal) {
            //	claims.AddRange ((this.Principal as IClaimsPrincipal).Claims);
            //}

            // Build the claim string
            //StringBuilder claimString = new StringBuilder();
            //foreach (var c in claims) {
            //	claimString.AppendFormat ("{0},", 
            //		Convert.ToBase64String (Encoding.UTF8.GetBytes (String.Format ("{0}={1}", c.Type, c.Value))));
            //}
            //if(claimString.Length > 0)
            //	claimString.Remove (claimString.Length - 1, 1);

            // Add authenticat header
            var retVal = new System.Collections.Generic.Dictionary<string, string>() {
                { "Authorization", String.Format("BASIC {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(appAuthString))) }
            };
            //if (claimString.Length > 0)
            //	retVal.Add ("X-SanteDBClient-Claim", claimString.ToString ());

            return retVal;
        }
        #endregion

    }
}

