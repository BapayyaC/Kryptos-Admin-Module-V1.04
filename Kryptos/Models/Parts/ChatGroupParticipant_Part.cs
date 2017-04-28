using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Kryptos.Models
{
    public partial class ChatGroupParticipant
    {
        kryptoEntities1 ke = new kryptoEntities1();
        private UserLoginInformation _user;

        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(USERID))); }
            set { _user = value; }
        }

        private ChatGroup _group;
        public ChatGroup Group
        {
            get { return _group ?? (_group = ke.ChatGroups.SingleOrDefault(x => x.GroupId.Equals(GroupId))); }
            set
            {
                _group = value;
            }
        }

        private ChatGroup _participantGroup;
        public ChatGroup ParticipantGroup
        {
            get
            {
                if (!ParticipantGroupId.HasValue) return null;
                return _participantGroup ??
                       (_participantGroup = ke.ChatGroups.SingleOrDefault(x => x.GroupId.Equals(ParticipantGroupId.Value)));
            }
            set { _participantGroup = value; }
        }

    }
}
