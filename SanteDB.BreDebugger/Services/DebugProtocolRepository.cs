/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.Cdss;
using SanteDB.Core.Model.Query;
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

        private class DebugCdssLibraryRepositoryEntry : ICdssLibraryRepositoryMetadata
        {

            public DebugCdssLibraryRepositoryEntry(ICdssLibrary library)
            {
                this.Key = library.Uuid;
            }

            public long? VersionSequence { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Guid? VersionKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Guid? PreviousVersionKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool IsHeadVersion { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Guid? CreatedByKey => throw new NotImplementedException();

            public Guid? ObsoletedByKey => throw new NotImplementedException();

            public DateTimeOffset CreationTime => throw new NotImplementedException();

            public DateTimeOffset? ObsoletionTime => throw new NotImplementedException();

            public Guid? Key { get; set; }

            public string Tag => throw new NotImplementedException();

            public DateTimeOffset ModifiedOn => throw new NotImplementedException();
        }

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
            if (data.Uuid == Guid.Empty)
            {
                data.Uuid = Guid.NewGuid();
            }
            this.m_protocols.RemoveAll(o => o.Uuid == data.Uuid || o.Id == data.Id);
            var entry = new DebugCdssLibraryRepositoryEntry(data);
            this.m_protocols.Add(data);
            return data;
        }

        /// <inheritdoc/>
        public IQueryResultSet<ICdssLibrary> Find(Expression<Func<ICdssLibrary, bool>> filter)
        {
            return this.m_protocols.Where(filter.Compile()).AsResultSet();

        }

        /// <inheritdoc/>
        public ICdssLibrary Get(Guid protocolUuid, Guid? versionUuid) => this.m_protocols.FirstOrDefault(o => o.Uuid == protocolUuid);

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
