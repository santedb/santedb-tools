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
using MohawkCollege.Util.Console.Parameters;
using SanteDB.AdminConsole.Attributes;
using SanteDB.AdminConsole.Util;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Security;
using SanteDB.Messaging.AMI.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using static SanteDB.AdminConsole.Shell.CmdLets.RoleCmdlet;

namespace SanteDB.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Represents an application commandlet for adding/removing/updating applications
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class ApplicationCmdlet
    {
        /// <summary>
        /// Base class for user operations
        /// </summary>
        internal class GenericApplicationParms
        {
            /// <summary>
            /// Gets or sets the username
            /// </summary>
            [Description("The identity of the application")]
            [Parameter("*")]
            public StringCollection ApplictionId { get; set; }
        }


        /// <summary>
        /// Secret to set
        /// </summary>
        internal class GenericApplicationSecretParms : GenericApplicationParms
        {
            /// <summary>
            /// The secret of the application
            /// </summary>
            [Description("The application secret to set")]
            [Parameter("s")]
            public string Secret { get; set; }
        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        #region Application Add

        /// <summary>
        /// Parameters for adding applications
        /// </summary>
        internal class AddApplicationParms : GenericApplicationSecretParms
        {


            /// <summary>
            /// The policies to add
            /// </summary>
            [Description("The policies to grant deny application")]
            [Parameter("g")]
            public StringCollection GrantPolicies { get; set; }

            /// <summary>
            /// The policies to deny
            /// </summary>
            [Description("The policies to deny the application")]
            [Parameter("d")]
            public StringCollection DenyPolicies { get; set; }

            /// <summary>
            /// The note
            /// </summary>
            [Description("A description/note to add to the application")]
            [Parameter("n")]
            public String Description { get; set; }
        }

        [AdminCommand("application.add", "Add security application")]
        [Description("This command will create a new security application which can be used to access the SanteDB instance")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        internal static void AddApplication(AddApplicationParms parms)
        {
            var policies = new List<SecurityPolicyInfo>();

            if (parms.GrantPolicies?.Count > 0)
            {
                policies = parms.GrantPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o)).ToList();
            }

            if (parms.DenyPolicies?.Count > 0)
            {
                policies = policies.Union(parms.DenyPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o))).ToList();
            }

            policies.ForEach(o => o.Grant = parms.GrantPolicies?.Contains(o.Oid) == true ? PolicyGrantType.Grant : PolicyGrantType.Deny);

            if (policies.Count != (parms.DenyPolicies?.Count ?? 0) + (parms.GrantPolicies?.Count ?? 0))
            {
                throw new InvalidOperationException("Could not find one or more policies");
            }

            if (String.IsNullOrEmpty(parms.Secret))
            {
                parms.Secret = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "").Substring(0, 12);
                Console.WriteLine("Application secret: {0}", parms.Secret);
            }

            m_client.CreateApplication(new SecurityApplicationInfo()
            {
                Policies = policies,
                Entity = new SecurityApplication()
                {
                    Name = parms.ApplictionId.OfType<String>().First(),
                    ApplicationSecret = parms.Secret
                }
            });
            Console.WriteLine("CREATE {0}", parms.ApplictionId[0]);
        }


        [AdminCommand("application.secret", "Reset application secret")]
        [Description("This command resets or re-generates the application secret")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        internal static void ChangeSecret(GenericApplicationSecretParms parms)
        {

            var appid = parms.ApplictionId.OfType<String>().First();
            var application = m_client.GetApplications(o => o.Name.ToLowerInvariant() == appid.ToLowerInvariant()).CollectionItem.OfType<SecurityApplicationInfo>().FirstOrDefault();

            if(application == null)
            {
                throw new KeyNotFoundException($"Application {appid} not found");
            }

            if (String.IsNullOrEmpty(parms.Secret))
            {
                parms.Secret = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "").Substring(0, 12);
                Console.WriteLine("Application secret: {0}", parms.Secret);
            }

            m_client.UpdateApplication(application.Key.Value, new SecurityApplicationInfo()
            {
                Entity = new SecurityApplication()
                {
                    Name = parms.ApplictionId.OfType<String>().First(),
                    ApplicationSecret = parms.Secret
                },
                
            });
            Console.WriteLine("SECRET {0}", parms.ApplictionId[0]);
        }

        /// <summary>
        /// User list parameters
        /// </summary>
        internal class ApplicationListParams
        {
            /// <summary>
            /// Locked
            /// </summary>
            [Description("Filter on locked status")]
            [Parameter("l")]
            public bool Locked { get; set; }

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Include non-active application")]
            [Parameter("a")]
            public bool Active { get; set; }
        }

        [AdminCommand("application.list", "List Security Applications")]
        [Description("This command will list all security applications in the SanteDB instance")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        internal static void ListRoles(ApplicationListParams parms)
        {
            AmiCollection list = null;
            int tr = 0;
            if (parms.Active)
            {
                list = m_client.Query<SecurityApplication>(o => o.ObsoletionTime.HasValue, 0, 100, out tr);
            }
            else if (parms.Locked)
            {
                list = m_client.Query<SecurityApplication>(o => o.Lockout.HasValue, 0, 100, out tr);
            }
            else
            {
                list = m_client.Query<SecurityApplication>(o => o.ObsoletionTime == null, 0, 100, out tr);
            }

            DisplayUtil.TablePrint(list.CollectionItem.OfType<SecurityApplicationInfo>(),
                new String[] { "SID", "Name", "Last Auth.", "Lockout", "ILA", "A" },
                new int[] { 38, 24, 22, 22, 4, 2 },
                o => o.Entity.Key,
                o => o.Entity.Name,
                o => o.Entity.LastAuthenticationXml,
                o => o.Entity.LockoutXml,
                o => o.Entity.InvalidAuthAttempts ?? 0,
                o => !o.Entity.ObsoletionTime.HasValue ? "*" : null);
        }

        /// <summary>
        /// Detail security information
        /// </summary>
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        [AdminCommand("application.info", "Displays detailed information about the application")]
        [Description("This command will display detailed information about the specified security application account. It will status, and effective policies")]
        internal static void ApplicationInfo(GenericApplicationParms parms)
        {
            if (parms.ApplictionId == null)
            {
                throw new InvalidOperationException("Must specify a application");
            }

            foreach (var un in parms.ApplictionId)
            {
                var device = m_client.GetApplications(o => o.Name == un).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
                if (device == null)
                {
                    throw new KeyNotFoundException($"Application {un} not found");
                }

                DisplayUtil.PrintPolicies(device,
                    new string[] { "Name", "SID", "Invalid Auth", "Lockout", "Last Auth", "Created", "Updated", "De-Activated" },
                    u => u.Name,
                    u => u.Key,
                    u => u.InvalidAuthAttempts,
                    u => u.LockoutXml,
                    u => u.LastAuthenticationXml,
                    u => String.Format("{0} ({1})", u.CreationTimeXml, m_client.GetUser(m_client.GetProvenance(u.CreatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.UpdatedTimeXml, m_client.GetUser(m_client.GetProvenance(u.UpdatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.ObsoletionTimeXml, m_client.GetUser(m_client.GetProvenance(u.ObsoletedByKey.Value).UserKey.Value).Entity.UserName)
                );
            }
        }

        #endregion Application Add

        #region Application Delete/Lock Commands

        /// <summary>
        /// User locking parms
        /// </summary>
        internal class ApplicationLockParms : GenericApplicationParms
        {
            /// <summary>
            /// The time to set the lock util
            /// </summary>
            [Description("Whether or not to set the lock")]
            [Parameter("l")]
            public bool Locked { get; set; }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("application.del", "De-activates an application in the SanteDB instance")]
        [Description("This command change the obsoletion time of the application effectively de-activating it")]
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        internal static void Delete(GenericApplicationParms parms)
        {
            if (parms.ApplictionId == null)
            {
                throw new InvalidOperationException("Must specify an application id");
            }

            foreach (var un in parms.ApplictionId)
            {
                var user = m_client.GetApplications(o => o.Name == un).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
                if (user == null)
                {
                    throw new KeyNotFoundException($"Application {un} not found");
                }

                m_client.DeleteApplication(user.Entity.Key.Value);
            }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("application.undel", "Re-activates an application in the SanteDB instance")]
        [Description("This command will undo a de-activation and will reset the applications's obsoletion time")]
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        internal static void UnDelete(GenericApplicationParms parms)
        {
            if (parms.ApplictionId == null)
            {
                throw new InvalidOperationException("Must specify an application");
            }

            foreach (var un in parms.ApplictionId)
            {
                var application = m_client.GetApplications(o => o.Name == un).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
                if (application == null)
                {
                    throw new KeyNotFoundException($"Application {un} not found");
                }

                var patch = new Patch()
                {
                    AppliesTo = new PatchTarget(application.Entity),
                    Operation = new List<PatchOperation>()
                    {
                        new PatchOperation(PatchOperationType.Remove, "obsoletedBy", null),
                        new PatchOperation(PatchOperationType.Remove, "obsoletionTime", null)
                    }
                };
                m_client.Client.Patch($"SecurityApplication/{application.Key}", application.Tag, patch);
            }
        }

        /// <summary>
        /// Useradd parameters
        /// </summary>
        [AdminCommand("application.lock", "Engages or disengages the application lock")]
        // // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AlterIdentity)]
        [Description("This command will change lock status of the application, either setting it or un-setting it")]
        internal static void Userlock(ApplicationLockParms parms)
        {
            if (parms.ApplictionId == null)
            {
                throw new InvalidOperationException("Must specify an application");
            }

            foreach (var un in parms.ApplictionId)
            {
                var application = m_client.GetApplications(o => o.Name == un).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
                if (application == null)
                {
                    throw new KeyNotFoundException($"Application {un} not found");
                }

                if (parms.Locked)
                {
                    m_client.Client.Lock<SecurityApplicationInfo>($"SecurityApplication/{application.Key}");
                }
                else
                {
                    m_client.Client.Unlock<SecurityApplicationInfo>($"SecurityApplication/{application.Key}");
                }
            }
        }

        /// <summary>
        /// Add role parameters
        /// </summary>
        internal class GrantApplicationParms : GenericApplicationParms
        {

            /// <summary>
            /// Gets or sets the policies
            /// </summary>
            [Description("The policies to grant")]
            [Parameter("p")]
            public StringCollection GrantPolicies { get; set; }

            /// <summary>
            /// The grant
            /// </summary>
            [Description("The grant action")]
            [Parameter("g")]
            public String Grant { get; set; }

        }

        /// <summary>
        /// Add a role
        /// </summary>
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateRoles)]
        [AdminCommand("application.grant", "Grants an application a policy")]
        internal static void GrantApplication(GrantApplicationParms parms)
        {
            // get the role
            var appid = parms.ApplictionId.OfType<String>().First();
            var application = m_client.GetApplications(o => o.Name == appid).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
            if (application == null)
            {
                throw new KeyNotFoundException($"Application {appid} not found");
            }

            // Get the policies
            var policyKeys = parms.GrantPolicies.OfType<String>().Select(r =>
            {
                var pol = m_client.GetPolicies(p => p.Oid.ToLowerInvariant() == r.ToLowerInvariant() | p.Name.ToLowerInvariant() == r.ToLowerInvariant()).CollectionItem.OfType<SecurityPolicy>().FirstOrDefault();
                if (pol == null)
                {
                    throw new KeyNotFoundException($"Policy having OID or Name of {r}");
                }
                else
                {
                    return pol;
                }
            }).ToArray();

            if (!Enum.TryParse<PolicyGrantType>(parms.Grant, true, out var grant))
            {
                throw new ArgumentOutOfRangeException($"{parms.Grant} - Expected Grant, Deny, Elevate");
            }

            foreach (var p in policyKeys)
            {
                m_client.AddPolicy(application.Entity, p.Oid, grant);
                Console.WriteLine("{0} {1} - ADDED", grant, p.Name);
            }

        }

        /// <summary>
        /// Add a role
        /// </summary>
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateRoles)]
        [AdminCommand("application.ungrant", "Removes an application policy")]
        internal static void UnGrantApplication(GrantApplicationParms parms)
        {
            // get the role
            var appid = parms.ApplictionId.OfType<String>().First();
            var application = m_client.GetApplications(o => o.Name == appid).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
            if (application == null)
            {
                throw new KeyNotFoundException($"Application {appid} not found");
            }

            // Get the policies
            var policyKeys = parms.GrantPolicies.OfType<String>().Select(r =>
            {
                var pol = m_client.GetPolicies(p => p.Oid.ToLowerInvariant() == r.ToLowerInvariant() | p.Name.ToLowerInvariant() == r.ToLowerInvariant()).CollectionItem.OfType<SecurityPolicy>().FirstOrDefault();
                if (pol == null)
                {
                    throw new KeyNotFoundException($"Policy having OID or Name of {r}");
                }
                else
                {
                    return pol;
                }
            }).ToArray();

            if (!Enum.TryParse<PolicyGrantType>(parms.Grant, true, out var grant))
            {
                throw new ArgumentOutOfRangeException($"{parms.Grant} - Expected Grant, Deny, Elevate");
            }

            foreach (var p in policyKeys)
            {
                m_client.RemovePolicy(application.Entity, p.Key.Value);
                Console.WriteLine("{0} {1} - REMOVED", grant, p.Name);
            }
        }

        #endregion Application Delete/Lock Commands
    }
}