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
 */
using SanteDB.PakMan.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SanteDB.PakSrv
{
    /// <summary>
    /// A specialized package manager configuration for the server host
    /// </summary>
    [XmlType(nameof(PakSrvConfiguration), Namespace = "http://santedb.org/pakman")]
    [XmlRoot(nameof(PakSrvConfiguration), Namespace = "http://santedb.org/pakman")]
    public class PakSrvConfiguration
    {

        // Serializer instance 
        private static readonly XmlSerializer s_serializer = new XmlSerializer(typeof(PakSrvConfiguration));

        /// <summary>
        /// Bindigs for the service host
        /// </summary>
        [XmlArray("binding"), XmlArrayItem("add")]
        public List<String> Bindings { get; set; }

        /// <summary>
        /// Authorization application keys
        /// </summary>
        [XmlArray("authorizations"), XmlArrayItem("add")]
        public List<PakSrvAuthentication> AuthorizedKeys { get; set; }

        /// <summary>
        /// Repository
        /// </summary>
        [XmlElement("repository")]
        public PackageRepositoryConfig Repository { get; set; }

        /// <summary>
        /// Load the configuration from the specifed source
        /// </summary>
        public static PakSrvConfiguration Load(Stream source)
        {
            return s_serializer.Deserialize(source) as PakSrvConfiguration;
        }

        /// <summary>
        /// Save the configuration file
        /// </summary>
        public void Save(Stream destination)
        {
            s_serializer.Serialize(destination, this);
        }

    }
}
