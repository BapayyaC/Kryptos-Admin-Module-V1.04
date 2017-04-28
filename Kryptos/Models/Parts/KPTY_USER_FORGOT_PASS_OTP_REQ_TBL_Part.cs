using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
namespace Kryptos.Models
{
    public partial class KPTY_USER_FORGOT_PASS_OTP_REQ_TBL
    {
        kryptoEntities1 ke = new kryptoEntities1();

        private UserLoginInformation _user;
        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(USERID))); }
            set { _user = value; }
        }

    }
}
