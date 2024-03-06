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
using SanteDB.PakMan.Configuration;
using SanteDB.PakMan.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.PakMan.Repository
{
    /// <summary>
    /// Package repository utility
    /// </summary>
    public static class PackageRepositoryUtil
    {

        /// <summary>
        /// Configuration
        /// </summary>
        private static PakManConfig s_configuration;

        // Pseudo configuration for local cache
        private static PackageRepositoryConfig s_localCache;

        // Sante DB SDK
        private const string LocalCachePath = "file:///~/.santedb-sdk";

        /// <summary>
        /// Static ctor
        /// </summary>
        static PackageRepositoryUtil()
        {
            var configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "pakman.config");
            if (!System.IO.File.Exists(configFile))
            {
                if (!Directory.Exists(Path.GetDirectoryName(configFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(configFile));
                }

                using (var fs = System.IO.File.Create(configFile))
                {
                    s_configuration = new PakManConfig()
                    {
                        Repository = new List<PackageRepositoryConfig>()
                        {
                            new PackageRepositoryConfig() { Path = "https://packages.santesuite.net" }
                        }
                    };
                    s_configuration.Save(fs);
                }
            }
            else
            {
                using (var fs = System.IO.File.OpenRead(configFile))
                {
                    s_configuration = PakManConfig.Load(fs);
                }
            }

            s_localCache = s_configuration.Repository.Find(o => o.Path == LocalCachePath);
            if (s_localCache == null)
            {
                s_localCache = new PackageRepositoryConfig()
                {
                    Path = LocalCachePath
                };
                s_configuration.Repository.Add(s_localCache);
            }
        }

        /// <summary>
        /// Publish to the specified service
        /// </summary>
        public static AppletInfo Publish(String serverUrl, AppletPackage package)
        {
            try
            {
                if (String.IsNullOrEmpty(serverUrl))
                {
                    foreach (var c in s_configuration.Repository)
                    {
                        c.GetRepository().Put(package);
                    }

                    return package.Meta;
                }
                else
                {
                    var config = s_configuration.Repository.Find(o => o.Path == serverUrl);
                    if (config == null)
                    {
                        throw new KeyNotFoundException($"Configuration for {serverUrl} not found");
                    }

                    return config.GetRepository().Put(package);
                }
            }
            catch (RestClientException e)
            {
                throw new Exception($"PUT Failed - {e.Status} - {e.Message} - {e.Result?.Message}");
            }
            catch (Exception e)
            {
                throw new Exception($"PUT Failed - {e.Message}", e);
            }
        }

        /// <summary>
        /// Get specified package from any package repository
        /// </summary>
        public static AppletPackage GetFromAny(String packageId, Version packageVersion)
        {

            AppletPackage retVal = null;

            try
            {
                return s_localCache.GetRepository().Get(packageId, packageVersion, true);
            }
            catch
            {
                foreach (var rep in s_configuration.Repository)
                {
                    try
                    {
                        retVal = rep.GetRepository().Get(packageId, packageVersion);
                        if (retVal == null)
                        {
                            continue;
                        }

                        if (packageVersion == null || retVal.Version == packageVersion.ToString())
                        {
                            if (!LocalCachePath.Equals(rep.Path))
                            {
                                PackageRepositoryUtil.InstallCache(retVal);
                            }

                            break;
                        }
                    }
                    catch
                    {

                    }
                }
            }


            return retVal;

        }

        /// <summary>
        /// Get specified package from any package repository
        /// </summary>
        public static IEnumerable<AppletInfo> FindFromAny(Expression<Func<AppletInfo, bool>> query, int offset, int count)
        {
            IEnumerable<AppletInfo> results = null;
            foreach (var rep in s_configuration.Repository)
            {
                try
                {
                    var retVal = rep.GetRepository().Find(query, offset, count, out int _);

                    if (results == null)
                    {
                        results = retVal;
                    }
                    else
                    {
                        results = results.Union(retVal);
                    }
                }
                catch
                {

                }
            }
            return results ?? new List<AppletInfo>();

        }

        /// <summary>
        /// Install the package into the local cache repository
        /// </summary>
        public static void InstallCache(AppletPackage pkg)
        {
            s_localCache.GetRepository().Put(pkg);
        }

    }
}
