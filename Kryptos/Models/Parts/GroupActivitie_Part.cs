using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Kryptos.Models
{
    public partial class GroupActivity
    {
        kryptoEntities1 ke = new kryptoEntities1();
        private UserLoginInformation _user;

        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(USERID))); }
            set { _user = value; }
        }

        private GroupActivitiesType _groupActivityType;

        public GroupActivitiesType GroupActivityType
        {
            get
            {
                return _groupActivityType ??
                       (_groupActivityType =
                           ke.GroupActivitiesTypes.SingleOrDefault(x => x.GroupActivitiesTypeId.Equals(GroupActivitiesTypeId)));
            }
            set { _groupActivityType = value; }
        }

    }
}
