using SanteDB.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.AdminConsole.Security
{
    internal class HttpBasicCredentialProvider : ICredentialProvider
    {
        public RestRequestCredentials GetCredentials(IRestClient context) => GetCredentialsInternal();

        public RestRequestCredentials GetCredentials(IPrincipal principal) => GetCredentialsInternal();

        private RestRequestCredentials GetCredentialsInternal()
        {
            return new HttpBasicCredentials(Shell.ApplicationContext.Current.Configuration.User, Shell.ApplicationContext.Current.Configuration.Password);
        }
    }
}
