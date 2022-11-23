using SanteDB.Client.OAuth;
using SanteDB.Core.Http;
using SanteDB.Core.Security.Claims;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.AdminConsole.Security
{
    internal class OAuthBearerTokenCredentials : RestRequestCredentials
    {
        public OAuthBearerTokenCredentials(OAuthClaimsPrincipal principal) : base(principal)
        {
        }

        public override void SetCredentials(HttpWebRequest webRequest)
        {
            if (null == webRequest)
            {
                throw new ArgumentNullException(nameof(webRequest));
            }

            if (Principal is Client.OAuth.OAuthClaimsPrincipal oacp)
            {
                webRequest.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {oacp.GetAccessToken()}");
            }
        }
    }
}
