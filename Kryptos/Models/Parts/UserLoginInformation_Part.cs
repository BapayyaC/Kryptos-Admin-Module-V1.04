using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Text;

namespace Kryptos.Models
{
    public partial class UserLoginInformation
    {
        private kryptoEntities1 ke = new kryptoEntities1();

        [JsonIgnore]
        [ScriptIgnore]
        private FacilityMaster _facility;

        [JsonIgnore]
        [ScriptIgnore]
        public FacilityMaster Facility
        {
            get
            {
                if (!FacilityId.HasValue) return null;
                _facility = ke.FacilityMasters.SingleOrDefault(x => x.FacilityMasterId.Equals(FacilityId.Value));
                return _facility;
            }
            set { _facility = value; }
        }

        private String _organisationName;

        public String OrganisationName
        {
            get
            {
                if (_organisationName == null)
                {
                    if (Facility != null)
                    {
                        Organisation temp = Facility.OOrganisation;
                        if (temp != null)
                        {
                            _organisationName = temp.Name;
                            return _organisationName;
                        }
                    }
                }
                _organisationName = "No Associated Organisation!";
                return _organisationName;
            }
            set
            {
                _organisationName = value;
            }
        }

        private String _titleName;

        public String TitleName
        {
            get
            {
                if (Title == null) _titleName = "Invalid Title";
                else
                {
                    int value = int.Parse(Title);
                    _titleName = (from x in ke.Titles where x.Id == value select x.Name).Single();
                }
                return _titleName;
            }
            set { _titleName = value; }
        }

        public string[] OtherFacilityIds { get; set; }
        
        public string UserSelections { get; set; }

        public string[] GetOtherFacilityIds()
        {
            if (OtherFacilityIds == null)
            {
                List<int> facilityIdsInUserFacilityList = GetFacilityIdsInUserFacilityList();
                if (facilityIdsInUserFacilityList != null)
                {
                    OtherFacilityIds = new string[facilityIdsInUserFacilityList.Count];
                    for (int i = 0; i < facilityIdsInUserFacilityList.Count; i++)
                    {
                        OtherFacilityIds[i] = facilityIdsInUserFacilityList[i].ToString();
                    }
                }
            }
            return OtherFacilityIds;
        }

        public List<int> GetOtherFacilityIdsAsints()
        {
            string[] otherFacilityIds = OtherFacilityIds;
            if (otherFacilityIds != null && otherFacilityIds.Length > 0)
            {
                _otherFacilityIdsAsints = otherFacilityIds.Select(int.Parse).ToList();
                return _otherFacilityIdsAsints;
            }
            return null;
        }



        [JsonIgnore]
        [ScriptIgnore]
        private List<int> _otherFacilityIdsAsints;

        [JsonIgnore]
        [ScriptIgnore]
        private List<UserFacility> _userFacilityList;


        public List<UserFacility> GetUserFacilityList()
        {
            if (_userFacilityList == null && USERID != 0)
            {
                return _userFacilityList ?? (_userFacilityList = ke.UserFacilities.Where(x => x.USERID.Equals(USERID)).ToList());
            }
            return _userFacilityList;
        }

        [JsonIgnore]
        [ScriptIgnore]
        private List<String> _userFacilityNames;

        public List<String> GetUserFacilityNames()
        {
            if (_userFacilityNames == null)
            {
                _userFacilityNames = new List<String>();
                foreach (UserFacility @UserFacility in GetUserFacilityList())
                {
                    _userFacilityNames.Add(@UserFacility.FacilityName);
                }
            }
            return _userFacilityNames;
        }

        public String GetUserFacilityNamesAsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string @facName in GetUserFacilityNames())
            {
                sb.Append(@facName + " ,");
            }
            if (sb.Length > 2)
            {
                sb = sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }

        public bool FillOtherDeatilsFortheMatchingZip()
        {
            if (!ZipId.HasValue) return false;
            string value = ZipId.Value.ToString();
            value = value.PadLeft(5, '0');
            ZipCode currentcode = ke.ZipCodes.SingleOrDefault(x => x.ZipCode1 == value);
            if (currentcode == null) return false;
            Country = currentcode.Country;
            State = currentcode.State;
            City = currentcode.City;
            return true;
        }

        public List<int> GetFacilityIdsInUserFacilityList()
        {
            if (_facilityIdsInUserFacilityList != null) return _facilityIdsInUserFacilityList;
            List<UserFacility> userfacilities = GetUserFacilityList();
            if (userfacilities == null) return _facilityIdsInUserFacilityList;
            _facilityIdsInUserFacilityList = new List<int>();
            _facilityIdsInUserFacilityList.AddRange(userfacilities.Select(userFacility => userFacility.FacilityId ?? 0));
            return _facilityIdsInUserFacilityList;
        }

        [JsonIgnore]
        [ScriptIgnore]
        private List<int> _facilityIdsInUserFacilityList;

    }
}
