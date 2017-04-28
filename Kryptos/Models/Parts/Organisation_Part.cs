using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
namespace Kryptos.Models
{
    public partial class Organisation
    {
        private kryptoEntities1 ke = new kryptoEntities1();

        //private String _CreatedBy;
        //public String CreatedBy
        //{
        //    get
        //    {
        //        if (_CreatedBy == null)
        //        {
        //            if (CreatedById != null)
        //            {
        //                int created = int.Parse(CreatedById);
        //                _CreatedBy = (from p in ke.UserLoginInformations where p.USERID.Equals(created) select p.FirstName)
        //                        .FirstOrDefault();
        //            }
        //        }
        //        return _CreatedBy;

        //    }
        //    set { _CreatedBy = value; }
        //}

        //private String _mainOrganisationName;
        //public String MainOrganisationName
        //{
        //    get
        //    {
        //        if (_mainOrganisationName == null)
        //        {
        //            if (ParentId.HasValue)
        //            {
        //                var temp = (from p in ke.Organisations where p.OrganisationId.Equals(ParentId.Value) select p);
        //                Organisation organisation = temp.FirstOrDefault();
        //                if (organisation != null)
        //                {
        //                    _mainOrganisationName = organisation.Name;
        //                }
        //            }
        //        }
        //        return _mainOrganisationName;
        //    }
        //    set
        //    {
        //        _mainOrganisationName = value;
        //    }
        //}

        public List<UserLoginInformation> GetAssocaitedOrganisationAdmins()
        {
            List<UserLoginInformation> list = (from u in ke.UserLoginInformations
                                               join f in ke.FacilityMasters on u.FacilityId equals f.FacilityMasterId
                                               join o in ke.Organisations on f.OrganisationId equals o.OrganisationId
                                               where (u.IsOrganisationAdmin && o.OrganisationId == OrganisationId)
                                               select u).ToList();
            return list;

        }

        public List<UserLoginInformation> GetAssocaitedActiveOrganisationAdmins()
        {
            List<UserLoginInformation> list = (from u in ke.UserLoginInformations
                                               join f in ke.FacilityMasters on u.FacilityId equals f.FacilityMasterId
                                               join o in ke.Organisations on f.OrganisationId equals o.OrganisationId
                                               where (u.IsOrganisationAdmin && u.IsActive && o.OrganisationId == OrganisationId)
                                               select u).ToList();

            return list;
        }

        public List<UserLoginInformation> GetAssocaitedInActiveOrganisationAdmins()
        {
            List<UserLoginInformation> list = (from u in ke.UserLoginInformations
                                               join f in ke.FacilityMasters on u.FacilityId equals f.FacilityMasterId
                                               join o in ke.Organisations on f.OrganisationId equals o.OrganisationId
                                               where (u.IsOrganisationAdmin && !u.IsActive && o.OrganisationId == OrganisationId)
                                               select u).ToList();

            //select * from UserLoginInformations as u join FacilityMaster as f on u.FacilityId=f.FaciliyMasterId join Organisation as o on o.OrganisationId=f.OrganisationId where (u.IsOrganisationAdmin =1 and u.IsActive=0 and o.OrganisationId == 1)
            return list;
        }

        public string UserSelections { get; set; }

        public List<FacilityMaster> GetAssocaitedFacilities()
        {
            return ke.FacilityMasters.Where(x => x.OrganisationId == OrganisationId && x.Status == 1).ToList();
        }

        public int[] GetAssocaitedFacilityIDs()
        {
            return ke.FacilityMasters.Where(x => x.OrganisationId == OrganisationId && x.Status == 1).Select(x => x.FacilityMasterId).ToArray();
        }

        public List<FacilityMaster> GetAssocaitedActiveFacilities()
        {
            return ke.FacilityMasters.Where(x => x.OrganisationId == OrganisationId && x.Status==1).ToList();
        }
        public List<FacilityMaster> GetAssocaitedInActiveFacilities()
        {
            return ke.FacilityMasters.Where(x => x.OrganisationId == OrganisationId && x.Status==0).ToList();
        }


        [JsonIgnore]
        [ScriptIgnore]
        public List<UserLoginInformation> AssociatedUsers { get; set; }


        public String GetAssocaitedOrganisationAdminNamesAsString()
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

        public List<Organisation> GetAllOrganizations()
        {
            return ke.Organisations.ToList();
        }

        public List<Organisation> GetAllActiveOrganizations()
        {
            return ke.Organisations.Where(x => x.Status == 1).ToList();
        }

        public List<Organisation> GetAllInActiveOrganizations()
        {
            return ke.Organisations.Where(x => x.Status == 0).ToList();
        }
    }
}
