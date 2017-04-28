using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Kryptos.Models
{
    public partial class ChatGroup
    {
       kryptoEntities1 ke = new kryptoEntities1();
        private GroupType _oGroupType;
        public GroupType OGroupType
        {
            get { return _oGroupType ?? (_oGroupType = ke.GroupTypes.SingleOrDefault(x => x.GroupTypeId.Equals(GroupType.Value))); }
            set
            {
                _oGroupType = value;
            }
        }

        private UserLoginInformation _user;
        public UserLoginInformation User
        {
            get { return _user ?? (_user = ke.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(USERID))); }
            set { _user = value; }
        }

        public string UserSelections { get; set; }

        public List<ChatGroupParticipant> GetAssociatedChatGroupParticipants()
        {
            return ke.ChatGroupParticipants.Where(x => x.GroupId == GroupId).ToList();
        }

        public List<ChatGroupParticipant> GetAssociatedActiveChatGroupParticipants()
        {
            return ke.ChatGroupParticipants.Where(x => x.GroupId == GroupId && IsActive==true).ToList();

        }

        public List<ChatGroupParticipant> GetAssociatedInActiveChatGroupParticipants()
        {
            return ke.ChatGroupParticipants.Where(x => x.GroupId == GroupId && IsActive == false).ToList();

        }
        //public List<ChatGroupParticipant> GetAssocaitedChatGroupAdmins()
        //{
        //    // return ke.UserLoginInformations.Where(x => x.FacilityId == FacilityMasterId && x.IsFacilityAdmin).ToList();

        //    List<ChatGroupParticipant> list = (from c in ke.ChatGroups 
        //                                       join cg in ke.ChatGroupParticipants on c.GroupId equals cg.GroupId
                                              
                                             
        //                                       select cg.user).ToList();
        //    return list;

        //}

        //public List<UserLoginInformation> GetAssocaitedActiveOrganisationAdmins()
        //{
        //    List<UserLoginInformation> list = (from u in ke.UserLoginInformations
        //                                       join f in ke.FacilityMasters on u.FacilityId equals f.FacilityMasterId
        //                                       join o in ke.Organisations on f.OrganisationId equals o.OrganisationId
        //                                       where u.IsOrganisationAdmin && u.IsActive
        //                                       select u).ToList();

        //    return list;
        //}

        //public List<UserLoginInformation> GetAssocaitedInActiveOrganisationAdmins()
        //{
        //    List<UserLoginInformation> list = (from u in ke.UserLoginInformations
        //                                       join f in ke.FacilityMasters on u.FacilityId equals f.FacilityMasterId
        //                                       join o in ke.Organisations on f.OrganisationId equals o.OrganisationId
        //                                       where u.IsOrganisationAdmin && !u.IsActive
        //                                       select u).ToList();


        //    //select * from UserLoginInformations as u join FacilityMaster as f on u.FacilityId=f.FacilityMasterId join Organisation as o on o.OrganisationId=f.OrganisationId where u.IsOrganisationAdmin =1 and u.IsActive=0

        //    return list;
        //}

        public List<ChatGroup> GetAllChatGroups()
        {
            return ke.ChatGroups.ToList();
        }
        public List<ChatGroup> GetActiveChatGroups()
        {
            return ke.ChatGroups.Where(x => x.IsActive.Value == true).ToList();
        }

        public List<ChatGroup> GetInActiveChatGroups()
        {
            return ke.ChatGroups.Where(x => x.IsActive.Value == false).ToList();
        }

   
    }
}
