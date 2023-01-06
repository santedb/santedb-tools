using SanteDB.Core.Model.Query;
using SanteDB.Core.Protocol;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.SDK.BreDebugger.Services
{

    /// <summary>
    /// Class for protocol debugging
    /// </summary>
    internal class DebugProtocolRepository : IClinicalProtocolRepositoryService
    {
        /// <summary>
        /// Get the service name
        /// </summary>
        public String ServiceName => "Protocol Debugging Repository";

        // Protocols
        private List<IClinicalProtocol> m_protocols = new List<IClinicalProtocol>();

        /// <summary>
        /// Constructor
        /// </summary>
        public DebugProtocolRepository()
        {

        }

        /// <inheritdoc/>
        public IClinicalProtocol InsertProtocol(IClinicalProtocol data)
        {
            this.m_protocols.Add(data);
            return data;
        }

        /// <inheritdoc/>
        public IQueryResultSet<IClinicalProtocol> FindProtocol(string protocolName = null, string protocolOid = null)
        {
            if (!String.IsNullOrEmpty(protocolName))
            {
                return this.m_protocols.Where(o => o.Name == protocolName).AsResultSet();
            }
            else if (!String.IsNullOrEmpty(protocolOid))
            {
                return this.m_protocols.Where(o => o.GetProtocolData().Oid == protocolOid).AsResultSet();
            }
            else
            {
                return this.m_protocols.AsResultSet();
            }
        }

        /// <inheritdoc/>
        public IClinicalProtocol GetProtocol(Guid protocolUuid) => this.m_protocols.FirstOrDefault(o => o.Id == protocolUuid);

        /// <summary>
        /// Remove protocol
        /// </summary>
        public IClinicalProtocol RemoveProtocol(Guid protocolUuid)
        {
            var protocol = this.m_protocols.Find(o => o.Id == protocolUuid);
            this.m_protocols.RemoveAll(o => o.Id == protocolUuid);
            return protocol;
        }
    }
}
