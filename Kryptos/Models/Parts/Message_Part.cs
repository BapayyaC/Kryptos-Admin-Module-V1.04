using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Kryptos.Models
{
    public partial class Message
    {
        kryptoEntities1 ke = new kryptoEntities1();
        private UserLoginInformation _user;
        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(USERID))); }
            set { _user = value; }
        }

        private UserLoginInformation _toUser;
        public UserLoginInformation ToUser
        {
            get { return _toUser ?? (_toUser = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals((int)ToUserId))); }
            set { _toUser = value; }
        }
    }
}
