using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Kryptos.Models
{
    public partial class MessageTransaction
    {
        kryptoEntities1 ke = new kryptoEntities1();

        private Message _message;
        public Message Message
        {
            get { return _message ?? (_message = ke.Messages.SingleOrDefault(x => x.MessageId.Equals(MessageId))); }
            set
            {
                _message = value;
            }
        }

        private UserLoginInformation _user;
        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(USERID))); }
            set { _user = value; }
        }
    }
}
