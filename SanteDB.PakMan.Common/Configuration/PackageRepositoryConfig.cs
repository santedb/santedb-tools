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
using SanteDB.PakMan.Repository;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.PakMan.Configuration
{
    /// <summary>
    /// Package repository configuratio
    /// </summary>
    [XmlType(nameof(PackageRepositoryConfig), Namespace = "http://santedb.org/pakman")]
    public class PackageRepositoryConfig
    {

        // Repository
        private IPackageRepository m_repository;

        /// <summary>
        /// Any attribute on the configuration
        /// </summary>
        [XmlAnyAttribute]
        public XmlAttribute[] Configuration { get; set; }

        /// <summary>
        /// Gets or sets the path configured
        /// </summary>
        [XmlText]
        public string Path { get; set; }

        /// <summary>
        /// Gets the repository instance
        /// </summary>
        public IPackageRepository GetRepository()
        {

            if (this.m_repository == null)
            {
                this.m_repository = AppDomain.CurrentDomain.GetAllTypes()
                    .Where(t => !t.IsInterface && typeof(IPackageRepository).IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(t => Activator.CreateInstance(t) as IPackageRepository)
                    .FirstOrDefault(o => o.Scheme == new Uri(this.Path).Scheme);
                this.m_repository.Initialize(new Uri(this.Path), this.Configuration?.ToDictionary(o => o.Name, o => o.Value));
            }

            return this.m_repository;

        }

    }
}