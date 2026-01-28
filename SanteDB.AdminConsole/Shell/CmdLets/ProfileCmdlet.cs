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
 * Date: 2024-7-2
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.AdminConsole.Attributes;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Messaging.HDSI.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Cmdlet for interacting with user profiles
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class ProfileCmdlet
    {

        /// <summary>
        /// View profile
        /// </summary>
        internal class ViewProfileParms
        {
            /// <summary>
            /// Gets or sets the user name
            /// </summary>
            [Parameter("*")]
            [Parameter("u")]
            [Description("The user whose profile is being edited")]
            public String UserName { get; set; }

        }

        /// <summary>
        /// Edit profile parameters
        /// </summary>
        internal class EditProfileParms
        {
            /// <summary>
            /// Gets or sets the user name
            /// </summary>
            [Parameter("*")]
            [Parameter("u")]
            [Description("The user whose profile is being edited")]
            public String UserName { get; set; }

            /// <summary>
            /// Gets or sets the name to set on the profile
            /// </summary>
            [Parameter("name")]
            [Description("The display name to add to the user's public profile")]
            public String Name { get; set; }

            /// <summary>
            /// Gets or sets the public phonenumber
            /// </summary>
            [Parameter("tel")]
            [Description("The telephone number or e-mail to show on the user's public profile")]
            public String PhoneNumber { get; set; }

            /// <summary>
            /// Gets or sets the lanugage of the user
            /// </summary>
            [Parameter("lang")]
            [Description("The display/preferred language to set on the user's profile")]
            public String Laguage { get; set; }

        }

        private static AmiServiceClient m_amiClient = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));
        private static HdsiServiceClient m_hdsiClient = new HdsiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.HealthDataService));

        /// <summary>
        /// Edit the user profile
        /// </summary>
        /// <param name="editProfileParms"></param>
        [AdminCommand("profile.edit", "Creates or changes a user's CDR profile")]
        internal static void ProfileEdit(EditProfileParms editProfileParms)
        {

            if(String.IsNullOrEmpty(editProfileParms.UserName))
            {
                throw new ArgumentNullException(nameof(EditProfileParms.UserName));
            }

            var securityUser = m_amiClient.GetUsers(o => o.UserName.ToLowerInvariant() == editProfileParms.UserName.ToLowerInvariant()).CollectionItem.OfType<SecurityUserInfo>().FirstOrDefault();
            if(securityUser == null)
            {
                throw new KeyNotFoundException($"User {editProfileParms.UserName} not found");
            }

            var userEntity = m_hdsiClient.Query<UserEntity>(o => o.SecurityUserKey == securityUser.Key.Value).Item.OfType<UserEntity>().FirstOrDefault();

            if(userEntity == null)
            {
                userEntity = new UserEntity()
                {
                    SecurityUserKey = securityUser.Key
                };
            }

            // Change the values
            if (!String.IsNullOrEmpty(editProfileParms.Name)) {
                var name = editProfileParms.Name.Split(' ');
                userEntity.Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.OfficialRecord, name.Last(), name.Take(name.Length - 1).ToArray())
                };
            }

            if(!String.IsNullOrEmpty(editProfileParms.PhoneNumber)) {
                userEntity.Telecoms = new List<EntityTelecomAddress>()
                {
                    new EntityTelecomAddress(TelecomAddressUseKeys.MobileContact, editProfileParms.PhoneNumber)
                };
            }

            if(!String.IsNullOrEmpty(editProfileParms.Laguage))
            {
                userEntity.LanguageCommunication = new List<PersonLanguageCommunication>()
                {
                    new PersonLanguageCommunication(editProfileParms.Laguage, true)
                };
            }

            userEntity = m_hdsiClient.Create(userEntity);
            Console.WriteLine(userEntity.Key.ToString());
        }

        /// <summary>
        /// View profile
        /// </summary>
        [AdminCommand("profile.view", "View a user's public CDR profile")]
        internal static void ProfileView(ViewProfileParms viewProfileParms)
        {

            var userEntity = m_hdsiClient.Query<UserEntity>(o => o.SecurityUser.UserName.ToLowerInvariant() == viewProfileParms.UserName.ToLowerInvariant()).Item.OfType<UserEntity>().FirstOrDefault();
            if(userEntity == null)
            {
                Console.WriteLine("NO PROFILE");
            }
            else
            {
                Console.WriteLine("CDR Public Profile for: {0}", viewProfileParms.UserName);
                Console.WriteLine("Name: {0}", userEntity.Names.FirstOrDefault()?.ToString() ?? "N/A");
                Console.WriteLine("Telephone: {0}", String.Join(",", userEntity.Telecoms.Select(o=>o.Value)));
                Console.WriteLine("Language(s): {0}", String.Join(",", userEntity.LanguageCommunication?.Select(o => $"{o.LanguageCode} {(o.IsPreferred ? "P" : "")}")));

            }

        }

    }
}
