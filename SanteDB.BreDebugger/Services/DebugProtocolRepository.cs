﻿/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
 */
using SanteDB.Core.Model.Query;
using SanteDB.Core.Cdss;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.SDK.BreDebugger.Services
{

    /// <summary>
    /// Class for protocol debugging
    /// </summary>
    internal class DebugProtocolRepository : ICdssLibraryRepository
    {
        /// <summary>
        /// Get the service name
        /// </summary>
        public String ServiceName => "Protocol Debugging Repository";

        // Protocols
        private List<ICdssLibrary> m_protocols = new List<ICdssLibrary>();

        /// <summary>
        /// Constructor
        /// </summary>
        public DebugProtocolRepository()
        {

        }

        /// <inheritdoc/>
        public ICdssLibrary InsertOrUpdate(ICdssLibrary data)
        {
            this.m_protocols.Add(data);
            return data;
        }

        /// <inheritdoc/>
        public IQueryResultSet<ICdssLibrary> Find(Expression<Func<ICdssLibrary, bool>> filter)
        {
            return this.m_protocols.Where(filter.Compile()).AsResultSet();

        }

        /// <inheritdoc/>
        public ICdssLibrary Get(Guid protocolUuid) => this.m_protocols.FirstOrDefault(o => o.Uuid == protocolUuid);

        /// <inheritdoc/>
        public ICdssLibrary GetByOid(String protocolOid) => this.m_protocols.FirstOrDefault(o => o.Oid == protocolOid);

        /// <summary>
        /// Remove protocol
        /// </summary>
        public ICdssLibrary Remove(Guid protocolUuid)
        {
            var protocol = this.m_protocols.Find(o => o.Uuid == protocolUuid);
            this.m_protocols.RemoveAll(o => o.Uuid == protocolUuid);
            return protocol;
        }
    }
}
