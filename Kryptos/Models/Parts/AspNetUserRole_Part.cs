using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kryptos.Models
{
    public partial class UserRole
    {
        kryptoEntities1 ke = new kryptoEntities1();
        private UserLoginInformation _user;
        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(USERID))); }
            set { _user = value; }
        }

        private Role _role;
        public Role Role
        {
            get { return _role ?? (_role = ke.Roles.SingleOrDefault(x => x.Id.Equals(RoleId))); }
            set { _role = value; }
        }

    }
}
