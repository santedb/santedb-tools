using SanteDB.Core.Http;
using SanteDB.Core.Http.Description;
using SanteDB.Core.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.AdminConsole
{
    internal class AdminConsoleRestClientFactory : IRestClientFactory
    {
        public IRestClient CreateRestClient(IRestClientDescription description) => new RestClient(description);

        public IRestClient GetRestClientFor(ServiceEndpointType serviceEndpointType) => GetRestClientFor(serviceEndpointType.ToString());

        public IRestClient GetRestClientFor(string clientName)
        {
            if (TryGetRestClientFor(clientName, out var client))
            {
                return client;
            }

            throw new KeyNotFoundException(clientName);
        }

        public bool TryGetRestClientFor(ServiceEndpointType serviceEndpointType, out IRestClient restClient)
            => TryGetRestClientFor(serviceEndpointType, out restClient);

        public bool TryGetRestClientFor(string clientName, out IRestClient restClient)
            => Shell.ApplicationContext.Current.TryGetRestClient(clientName, out restClient);
    }
}
