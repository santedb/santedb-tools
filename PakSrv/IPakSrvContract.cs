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
using RestSrvr.Attributes;
using SanteDB.Core.Applets.Model;
using System.Collections.Generic;
using System.IO;

namespace SanteDB.PakSrv
{
    /// <summary>
    /// Pak service contract
    /// </summary>
    [ServiceContract(Name = "SanteDB PakServer")]
    public interface IPakSrvContract
    {
        /// <summary>
        /// Query all packages 
        /// </summary>
        /// <returns></returns>
        [Get("/pak")]
        List<AppletInfo> Find();

        /// <summary>
        /// Push a package to the package repository
        /// </summary>
        [Post("/pak")]
        AppletInfo Put(Stream body);

        /// <summary>
        /// Get package (most recent)
        /// </summary>
        [Get("/pak/{id}")]
        Stream Get(string id);

        /// <summary>
        /// Get package specific version
        /// </summary>
        [Get("/pak/{id}/{version}")]
        Stream Get(string id, string version);

        /// <summary>
        /// Get the most recent headers for a package
        /// </summary>
        [RestInvoke("HEAD", "/pak/{id}")]
        void Head(string id);

        /// <summary>
        /// Delete (unlist a package)
        /// </summary>
        [Delete("/pak/{id}")]
        AppletInfo Delete(string id);

        /// <summary>
        /// Delete a specific version of a package
        /// </summary>
        [Delete("/pak/{id}/{version}")]
        AppletInfo Delete(string id, string version);

        /// <summary>
        /// Get an asset from the PAK file
        /// </summary>
        [Get("/asset/{id}/{*assetPath}")]
        Stream GetAsset(string id, string assetPath);


        /// <summary>
        /// Get an friendly index file
        /// </summary>
        [Get("/{*content}")]
        Stream Serve(string content);

    }
}
