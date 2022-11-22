using SanteDB.AdminConsole.Shell;
using SanteDB.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.AdminConsole.Security
{
    internal class ApplicationCredentialProvider : ICredentialProvider
    {

        public RestRequestCredentials GetCredentials(IRestClient context)
        {
            return new HttpBasicCredentials(ApplicationContext.Current.ApplicationName, ApplicationContext.Current.ApplicationSecret);
        }

        public RestRequestCredentials GetCredentials(IPrincipal principal)
        {
            return new HttpBasicCredentials(ApplicationContext.Current.ApplicationName, ApplicationContext.Current.ApplicationSecret);
        }
    }
}
