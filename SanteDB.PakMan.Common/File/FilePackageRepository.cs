/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.Applets.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.PakMan.Repository.File
{
    /// <summary>
    /// A package repository that runs off a directory
    /// </summary>
    public class FilePackageRepository : IPackageRepository
    {
        /// <summary>
        /// The scheme that this package supports
        /// </summary>
        public string Scheme => "file";

        // Base path
        private Uri m_basePath;

        // Trace source
        private TraceSource m_traceSource = new TraceSource(nameof(FilePackageRepository));

        // Lock object
        private object m_lockObject = new object();

        /// <summary>
        /// Package information
        /// </summary>
        private IDictionary<String, AppletInfo> m_packageInfos;

        /// <summary>
        /// Get the repository path
        /// </summary>
        private String GetRepositoryPath()
        {
            // Get confifuration for repository
            var scanDir = this.m_basePath.LocalPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).Replace("/", Path.DirectorySeparatorChar.ToString());
            if (scanDir.StartsWith("\\"))
            {
                scanDir = scanDir.Substring(1);
            }

            if (!Directory.Exists(scanDir))
            {
                Directory.CreateDirectory(scanDir);
            }

            return scanDir;
        }

        /// <summary>
        /// Get the specified package with the specified version
        /// </summary>
        public AppletPackage Get(string id, Version version, bool exactVersion = false)
        {
            if (this.m_packageInfos == null)
            {
                throw new InvalidOperationException("Package repository has not been initialized");
            }

            // Now we want to look for the package
            IEnumerable<KeyValuePair<String, AppletInfo>> candidates = null;
            lock (this.m_lockObject)
            {
                candidates = this.m_packageInfos.Where(o => o.Value.Id == id && (version == null || new Version(o.Value.Version).Major == version.Major)).ToArray(); // take a copy
            }

            var match = candidates.OrderByDescending(o => o.Value.Version.ParseVersion(out _)).FirstOrDefault(o => version == null || o.Value.Version == version.ToString());
            if (match.Key != null)
            {
                return this.OpenPackage(match.Key);
            }
            else if (!exactVersion) // fuzzy look
            {
                match = candidates.OrderByDescending(o => o.Value.Version)
                        .FirstOrDefault(o =>
                        {
                            var v = o.Value.Version.ParseVersion(out _);
                            if (v.Revision == -1)
                            {
                                v = (o.Value.Version + ".0").ParseVersion(out _);
                            }

                            return v >= version;
                        }); // higher version
                if (match.Key != null)
                {
                    return this.OpenPackage(match.Key);
                }
                else
                {
                    throw new KeyNotFoundException($"Package {id}-{version} not found");
                }
            }
            else
            {
                throw new KeyNotFoundException($"Package {id}-{version} not found");
            }
        }

        /// <summary>
        /// Opens the specified package from the repository
        /// </summary>
        private AppletPackage OpenPackage(string filePath)
        {
            using (var s = System.IO.File.OpenRead(filePath))
            {
                return AppletPackage.Load(s);
            }
        }

        /// <summary>
        /// Query the package repository for the specified package
        /// </summary>
        /// <param name="query">The query to be executed</param>
        /// <param name="offset">The offset to the first result</param>
        /// <param name="count">The count of results</param>
        /// <param name="totalResults">The total results matching the file</param>
        /// <returns>The list of matching packages</returns>
        public IEnumerable<AppletInfo> Find(Expression<Func<AppletInfo, bool>> query, int offset, int count, out int totalResults)
        {
            if (this.m_packageInfos == null)
            {
                throw new InvalidOperationException("Package repository is not initialized");
            }

            IEnumerable<AppletInfo> matches = null;
            var queryPredicate = query.Compile();
            lock (this.m_lockObject)
            {
                matches = this.m_packageInfos
                    .Select(o => o.Value)
                    .Where(o => queryPredicate(o))
                    .ToArray();
            }

            totalResults = matches.Count();
            return matches.Skip(offset).Take(count);
        }

        /// <summary>
        /// Installs a package into the package repository
        /// </summary>
        public AppletInfo Put(AppletPackage package)
        {
            if (this.m_packageInfos == null)
            {
                throw new InvalidOperationException("Package repository is not initialized");
            }
            else if (String.IsNullOrEmpty(package.Meta?.Id))
            {
                throw new ArgumentNullException("Package must have ID");
            }
            else if (String.IsNullOrEmpty(package.Meta?.Version))
            {
                throw new ArgumentNullException("Package must have version");
            }

            try
            {
                var targetPath = Path.Combine(this.GetRepositoryPath(), $"{package.Meta.Id}-{package.Meta.Version}.pak");
                if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                }

                using (var fs = System.IO.File.Create(targetPath))
                {
                    package.Save(fs);
                }

                // Add the file to the repository
                lock (this.m_lockObject)
                {
                    if (!this.m_packageInfos.ContainsKey(targetPath))
                    {
                        this.m_packageInfos.Add(targetPath, package.Meta);
                    }
                }

                return package.Meta;
            }
            catch (System.Exception e)
            {
                throw new System.Exception($"Could not install package {package.Meta.Id} v {package.Meta.Version}", e);
            }
        }

        /// <summary>
        /// Initialize this package repository
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="configuration"></param>
        public void Initialize(Uri basePath, IDictionary<string, string> configuration)
        {
            if (this.m_packageInfos != null)
            {
                throw new InvalidOperationException("Repository is already initialized");
            }

            this.m_basePath = basePath;
            this.m_packageInfos = new Dictionary<String, AppletInfo>();
            foreach (var f in Directory.GetFiles(this.GetRepositoryPath(), "*.pak"))
            {
                try
                {
                    lock (this.m_lockObject)
                    {
                        this.m_packageInfos.Add(f, this.OpenPackage(f).Meta);
                    }
                }
                catch (System.Exception e)
                {
                    this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error loading {0} - {1}", f, e);
                }
            }
        }
    }
}