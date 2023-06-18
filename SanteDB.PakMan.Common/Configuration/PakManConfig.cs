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
 * Date: 2023-5-19
 */
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SanteDB.PakMan.Configuration
{
    /// <summary>
    /// Pakman configuration
    /// </summary>
    [XmlType(nameof(PakManConfig), Namespace = "http://santedb.org/pakman")]
    [XmlRoot(nameof(PakManConfig), Namespace = "http://santedb.org/pakman")]
    public class PakManConfig
    {

        // Serializer
        private static XmlSerializer s_serializer = new XmlSerializer(typeof(PakManConfig));

        /// <summary>
        /// Repositories
        /// </summary>
        [XmlArray("repositories"), XmlArrayItem("add")]
        public List<PackageRepositoryConfig> Repository { get; set; }


        /// <summary>
        /// Load the specified configuration
        /// </summary>
        public static PakManConfig Load(Stream config)
        {
            return s_serializer.Deserialize(config) as PakManConfig;
        }

        /// <summary>
        /// Save the specified configuration file
        /// </summary>
        public void Save(Stream fs)
        {
            s_serializer.Serialize(fs, this);
        }
    }
}
