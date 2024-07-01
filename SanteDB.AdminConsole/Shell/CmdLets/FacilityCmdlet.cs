using MohawkCollege.Util.Console.Parameters;
using SanteDB.AdminConsole.Attributes;
using SanteDB.AdminConsole.Util;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Messaging.HDSI.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SanteDB.AdminConsole.Shell.CmdLets.FacilityCmdlet;

namespace SanteDB.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Cmdlet for interacting with facilities
    /// </summary>
    [AdminCommandlet]
    public static class FacilityCmdlet
    {
        /// <summary>
        /// List staff parameters
        /// </summary>
        internal class ListStaffParms
        {
            /// <summary>
            /// The facility to assign
            /// </summary>
            [Parameter("facility")]
            [Parameter("f")]
            [Description("The facility for which the roster should be listed")]
            public String Facility { get; set; }
        }

        internal class AssignStaffParms
        {

            /// <summary>
            /// Gets or sets the user to assign
            /// </summary>
            [Parameter("*")]
            [Parameter("u")]
            [Description("The user(s) to assign to the facility")]
            public StringCollection Users { get; set; }

            /// <summary>
            /// The facility to assign
            /// </summary>
            [Parameter("facility")]
            [Parameter("f")]
            [Description("The facility to which the users are to be assigned")]
            public String Facility { get; set; }
        }

        private static HdsiServiceClient m_hdsiClient = new HdsiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.HealthDataService));
        private static AmiServiceClient m_amiClient = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));


        /// <summary>
        /// Assign users to a facility
        /// </summary>
        [AdminCommand("facility.staff.add", "Add facility staff to the specified facility")]
        internal static void AssignUsersToFacility(AssignStaffParms assignStaffParms)
        {

            Place facility = null;
            if(Guid.TryParse(assignStaffParms.Facility, out var g))
            {
                facility = m_hdsiClient.Get<Place>(g, null);
            }
            else
            {
                facility = m_hdsiClient.Query<Place>(o => o.Names.Any(n => n.Component.Any(c => c.Value == assignStaffParms.Facility))).Item.OfType<Place>().SingleOrDefault();
            }

            if(facility == null)
            {
                throw new KeyNotFoundException($"Facility {assignStaffParms.Facility} not found");
            }

            foreach(var stm in assignStaffParms.Users)
            {
                var userEntity = m_hdsiClient.Query<UserEntity>(o => o.SecurityUser.UserName.ToLowerInvariant() == stm.ToLowerInvariant()).Item.OfType<UserEntity>().FirstOrDefault();
                if(userEntity == null)
                {
                    throw new KeyNotFoundException($"User {stm} not found or does not have a user profile");
                }

                // Assign the user 
                if(userEntity.Relationships.Any(r=>r.RelationshipTypeKey == EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation && r.TargetEntityKey == facility.Key))
                {
                    Console.WriteLine("WARN: User {0} already assigned to {1} - skipping", stm, facility.Names.First().ToString());
                }
                else
                {
                    m_hdsiClient.Create(new EntityRelationship(EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation, userEntity.Key, facility.Key, null));
                    Console.WriteLine("Assign {0} to {1} - OK", stm, facility.Names.First().ToString());
                }
            }
        }

        /// <summary>
        /// Remove staff
        /// </summary>
        [AdminCommand("facility.staff.del", "Remove facility staff from the specified facility")]
        internal static void UnassignUsersToFacility(AssignStaffParms assignStaffParms)
        {
            Place facility = null;
            if (Guid.TryParse(assignStaffParms.Facility, out var g))
            {
                facility = m_hdsiClient.Get<Place>(g, null);
            }
            else
            {
                facility = m_hdsiClient.Query<Place>(o => o.Names.Any(n => n.Component.Any(c => c.Value == assignStaffParms.Facility))).Item.OfType<Place>().SingleOrDefault();
            }

            if (facility == null)
            {
                throw new KeyNotFoundException($"Facility {assignStaffParms.Facility} not found");
            }

            foreach (var stm in assignStaffParms.Users)
            {
                var eRelationship = m_hdsiClient.Query<EntityRelationship>(o => (o.SourceEntity as UserEntity).SecurityUser.UserName.ToLowerInvariant() == stm.ToLowerInvariant() && o.RelationshipTypeKey == EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation && o.TargetEntityKey == facility.Key).Item.OfType<EntityRelationship>().SingleOrDefault();
                if (eRelationship == null)
                {
                    Console.WriteLine("WARN: User {0} is not assigned to {1} - skipping", stm, facility.Names.First().ToString());
                }
                else
                {
                    m_hdsiClient.Obsolete<EntityRelationship>(eRelationship);
                    Console.WriteLine("Remove {0} from {1} - OK", stm, facility.Names.First().ToString());
                }
            }
        }

        /// <summary>
        /// List all staff in facility
        /// </summary>
        [AdminCommand("facility.staff", "List all staff in the facility")]
        internal static void ListRoster(ListStaffParms listStaffParms)
        {
            Place facility = null;
            if (Guid.TryParse(listStaffParms.Facility, out var uid))
            {
                facility = m_hdsiClient.Get<Place>(uid, null);
            }
            else
            {
                facility = m_hdsiClient.Query<Place>(o => o.Names.Any(n => n.Component.Any(c => c.Value == listStaffParms.Facility))).Item.OfType<Place>().SingleOrDefault();
            }

            if (facility == null)
            {
                throw new KeyNotFoundException($"Facility {listStaffParms.Facility} not found");
            }

            var staffList = m_hdsiClient.Query<UserEntity>(o => o.Relationships.Where(g => g.RelationshipTypeKey == EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation || g.RelationshipTypeKey == EntityRelationshipTypeKeys.MaintainedEntity).Any(r => r.TargetEntityKey == facility.Key)).Item.OfType<UserEntity>();
            DisplayUtil.TablePrint(staffList,
                new string[]
                {
                    "Login",
                    "Name",
                    "Manager"
                },
                new int[]
                {
                    20,
                    20,
                    5
                },
                o => m_amiClient.GetUser(o.SecurityUserKey.Value).Entity.UserName,
                o => o.Names.First().ToString(),
                o => o.Relationships.Any(r => r.RelationshipTypeKey == EntityRelationshipTypeKeys.MaintainedEntity && r.TargetEntityKey == facility.Key)
            );
        }
    }
}
