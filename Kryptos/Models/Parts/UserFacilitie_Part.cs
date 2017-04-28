using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kryptos.Models
{
    public partial class UserFacility
    {
        kryptoEntities1 ke = new kryptoEntities1();
        private FacilityMaster _facility;
        public FacilityMaster Facility
        {
            get
            {
                return _facility ?? (_facility = ke.FacilityMasters.SingleOrDefault(x => x.FacilityMasterId == FacilityId));
            }
            set
            {
                _facility = value;
            }
        }

        private String _facilityName;
        public String FacilityName
        {
            get
            {
                if (_facilityName == null)
                {
                    var facility = Facility;
                    if (facility != null)
                    {
                        _facilityName = facility.FacilityMasterName;
                    }
                    else _facilityName = "No Assocaited Facility exists";
                }
                return _facilityName;
            }
            set
            {
                _facilityName = value;
            }
        }

        private UserLoginInformation _user;
        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID == USERID)); }
            set { _user = value; }
        }
    }
}
