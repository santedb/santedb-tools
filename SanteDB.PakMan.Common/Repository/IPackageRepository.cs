/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using System.Linq.Expressions;

namespace SanteDB.PakMan.Repository
{
    /// <summary>
    /// Represents a package repository management
    /// </summary>
    public interface IPackageRepository
    {

        /// <summary>
        /// Initialize the package repository
        /// </summary>
        /// <param name="basePath">The base path</param>
        /// <param name="configuration">The configuration object for the repository</param>
        void Initialize(Uri basePath, IDictionary<String, String> configuration);

        /// <summary>
        /// Gets the scheme of this package repository
        /// </summary>
        String Scheme { get; }

        /// <summary>
        /// Gets a specific version of the package from the package repository
        /// </summary>
        /// <param name="id">The id of the package</param>
        /// <param name="version">The version of the package to retrieve</param>
        /// <param name="exactVersion">When true, the package must be the exact version</param>
        /// <returns>The applet package contents</returns>
        AppletPackage Get(string id, Version version, bool exactVersion = false);

        /// <summary>
        /// Gets all the packages matching the specified query
        /// </summary>
        /// <param name="count">The number of results to retrieve</param>
        /// <param name="offset">The offset of the first result</param>
        /// <param name="query">The query filter to execute</param>
        /// <param name="totalResults">The number of matching results</param>
        IEnumerable<AppletInfo> Find(Expression<Func<AppletInfo, bool>> query, int offset, int count, out int totalResults);

        /// <summary>
        /// Puts a package into the repository
        /// </summary>
        /// <param name="package">The package to be installed</param>
        /// <returns>The installed package</returns>
        AppletInfo Put(AppletPackage package);
    }
}
