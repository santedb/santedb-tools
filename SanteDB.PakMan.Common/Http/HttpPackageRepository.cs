/*
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
 * Date: 2023-3-10
 */
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.Query;
using SanteDB.PakMan.Exceptions;
using SanteDB.PakMan.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.PakMan.Http
{
    /// <summary>
    /// Package repository that uses HTTP(S)
    /// </summary>
    public class HttpPackageRepository : IPackageRepository
    {

        // Base path
        private SimpleRestClient m_client;

        // Trace source
        private TraceSource m_traceSource = new TraceSource(nameof(HttpPackageRepository));

        /// <summary>
        /// Get the scheme for this repository
        /// </summary>
        public virtual string Scheme => "https";

        /// <summary>
        /// Find the specified package
        /// </summary>
        public IEnumerable<AppletInfo> Find(Expression<Func<AppletInfo, bool>> query, int offset, int count, out int totalResults)
        {
            // Locate the specified package
            var nvc = QueryExpressionBuilder.BuildQuery<AppletInfo>(query);
            try
            {
                nvc.Add("_count", count.ToString());
                nvc.Add("_offset", offset.ToString());
                var results = this.m_client.Get<List<AppletInfo>>("pak", nvc.ToArray());
                totalResults = results.Count();
                return results;
            }
            catch (RestClientException e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error searching packages: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Get the specified package contents
        /// </summary>
        public AppletPackage Get(string id, Version version, bool exactVersion = false)
        {
            // Locate the specified package
            try
            {
                var path = $"pak/{id}";
                if (version != null)
                    path += $"/{version}";

                using (var inStream = this.m_client.Get<MemoryStream>(path))
                    return AppletPackage.Load(inStream);
            }
            catch (RestClientException e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error fetching package {0} v{1}: {2}", id, version, e);
                throw;
            }
        }

        /// <summary>
        /// Initialize the request
        /// </summary>
        public void Initialize(Uri basePath, IDictionary<string, string> configuration)
        {
            this.m_client = new SimpleRestClient(basePath);

            if (configuration != null && configuration.TryGetValue("username", out string userName) && configuration.TryGetValue("password", out string password))
                this.m_client.SetCredentials(userName, password);

        }

        /// <summary>
        /// Install the package
        /// </summary>
        public AppletInfo Put(AppletPackage package)
        {
            // Locate the specified package
            try
            {
                using (var ms = new MemoryStream())
                {
                    package.Save(ms);
                    return this.m_client.Put<MemoryStream, AppletInfo>("pak", new MemoryStream(ms.ToArray()));
                }
            }
            catch (RestClientException e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error pushing package {0}: {1}", package.Meta, e);
                throw;
            }
        }
    }
}
