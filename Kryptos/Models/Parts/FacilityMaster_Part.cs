using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Text;

namespace Kryptos.Models
{
    public partial class FacilityMaster
    {
      static   kryptoEntities1 ke = new kryptoEntities1();

        [JsonIgnore]
        [ScriptIgnore]
        private Organisation _oOrganisation;

        [JsonIgnore]
        [ScriptIgnore]
        public Organisation OOrganisation
        {
            get
            {
                return _oOrganisation ??
                       (_oOrganisation = ke.Organisations.SingleOrDefault(x => x.OrganisationId.Equals(OrganisationId) && x.Status==1));
            }
            set
            {
                _oOrganisation = value;
            }
        }

        private String _organisationName;
        public String OrganisationName
        {
            get
            {
                if (_organisationName != null) return _organisationName;
                _organisationName = OOrganisation != null ? OOrganisation.Name : "";
                return _organisationName;
            }
            set
            {
                _organisationName = value;
            }
        }

        public string UserSelections { get; set; }

        public List<UserLoginInformation> GetAssocaitedUsers()
        {
            return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.Status==1).ToList();
        }

        public List<UserLoginInformation> GetAssocaitedActiveUsers()
        {
            return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.IsActive == true && x.Status==1).ToList();
        }

        public List<UserLoginInformation> GetAssocaitedInActiveUsers()
        {
            return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.IsActive == false && x.Status==1).ToList();
        }

        public List<UserLoginInformation> GetAssocaitedUsersBasedOnStatus(bool status)
        {
            if (status)
            {
                return GetAssocaitedActiveUsers();
            }
            else
            {
                return GetAssocaitedInActiveUsers();
            }
        }


        public List<UserLoginInformation> GetAssocaitedOrganisationAdmins()
        {
            return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.IsOrganisationAdmin && x.Status==1).ToList();
        }

        public  List<UserLoginInformation> GetAssocaitedFacilityAdmins()
        {
            return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.IsFacilityAdmin && x.Status==1).ToList();
        }

        public List<UserLoginInformation> GetAssocaitedActiveFacilityAdmins()
        {
            return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.IsFacilityAdmin && x.IsActive && x.Status==1).ToList();
        }

        public List<UserLoginInformation> GetAssocaitedInActiveFacilityAdmins()
        {
            return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.IsFacilityAdmin && !x.IsActive &&x.Status==1).ToList();
        }

        [JsonIgnore]
        [ScriptIgnore]
        public List<UserLoginInformation> AssociatedUsers { get; set; }


        public String GetAssocaitedFacilityAdminNamesAsString()
        {
            StringBuilder sb = new StringBuilder();
            List<UserLoginInformation> Users = AssociatedUsers;
            if (Users != null)
            {
                foreach (UserLoginInformation @User in Users)
                {
                    sb.Append(@User.EmailId + " ,");
                }
                if (sb.Length > 2)
                {
                    sb = sb.Remove(sb.Length - 2, 2);
                }
                return sb.ToString();
            }
            return "No Users";
        }

        public static List<FacilityMaster> GetAllFacilities()
        {
            return ke.FacilityMasters.ToList();
        }
        public static List<FacilityMaster> GetAllActiveFacilities()
        {

            return ke.FacilityMasters.Where(x=>x.Status==1).ToList();
        }

        public static List<FacilityMaster> GetAllInActiveFacilities()
        {

            return ke.FacilityMasters.Where(x => x.Status == 0).ToList();
        }
    }
}
