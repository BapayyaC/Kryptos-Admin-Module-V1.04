using Kryptos.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.UI;
using Newtonsoft.Json;
using System.Web.Configuration;
using System.Data.SqlClient;
using CommonHelper;


namespace Kryptos.Controllers
{
    public class UserDatatablesController : Controller
    {
        //
        // GET: /UserDatatables/

        kryptoEntities1 _context = new kryptoEntities1();

        public ActionResult List()
        {
            ViewData["current view"] = "Users List";
            if (LoginController.ActiveUser != null)
            {
                ViewData["CURRENTUSER"] = LoginController.ActiveUser;
            }
            else
            {
                TempData["errormsg"] = "Please Login and Proceed!";
                return RedirectToAction("Login", "Login");
            }
            return View();
        }

        private List<UserLoginInformation> GetMatchingUsersForUserRole(UserLoginInformation currentUser)
        {
            List<UserLoginInformation> users = null;
            if (currentUser.IsSuperAdmin)
            {
                users = _context.UserLoginInformations.Where(x => x.Status == 1).ToList();
            }
            else if (currentUser.IsOrganisationAdmin)
            {
                users = (from user in _context.UserLoginInformations
                         join facility in _context.FacilityMasters on user.FacilityId equals facility.FacilityMasterId
                         join organisation in _context.Organisations on facility.OrganisationId equals organisation.OrganisationId
                         where facility.OrganisationId == currentUser.Facility.OrganisationId && user.Status == 1
                         select user).ToList();
            }
            else if (currentUser.IsFacilityAdmin)
            {
                users = (from user in _context.UserLoginInformations
                         join facility in _context.FacilityMasters on user.FacilityId equals facility.FacilityMasterId
                         where facility.FacilityMasterId == currentUser.Facility.FacilityMasterId && user.Status == 1
                         select user).ToList();
            }
            return users;
        }

        public ActionResult UserInfoList()
        {
            return Json(new { aaData = GetMatchingUsersForUserRole(LoginController.ActiveUser) }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getMatchingUser(int selecteduser)
        {
            UserLoginInformation userlogininfo =
                _context.UserLoginInformations.Single(x => x.USERID.Equals(selecteduser));
            userlogininfo.OtherFacilityIds = userlogininfo.GetOtherFacilityIds();
            return Json(userlogininfo, JsonRequestBehavior.AllowGet);
        }

        public UserLoginInformation Updateobject(int id, UserLoginInformation filled)
        {
            UserLoginInformation obj = _context.UserLoginInformations.Find(id);
            PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                object currentprop = prop.GetValue(filled);
                if (currentprop is Int32)
                {
                    int currentint = (int)currentprop;
                    if (currentint == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is Int16)
                {
                    Int16 currentInt16 = (Int16)currentprop;
                    if (currentInt16 == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is Byte)
                {
                    Byte currentByte = (Byte)currentprop;
                    if (currentByte == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is Boolean)
                {
                    Boolean currentBoolean = (Boolean)currentprop;
                    if (currentBoolean == (Boolean)prop.GetValue(obj))
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is String)
                {
                    string currentstring = (string)currentprop;
                    if (currentstring.Length == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is DateTime)
                {
                    DateTime currentDateTime = (DateTime)currentprop;
                    if (currentDateTime == new DateTime())
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else
                {
                    if (currentprop == null)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
            }
            return filled;
        }


        private List<Organisation> GetMatchingOrganisationsForUserRole(UserLoginInformation currentUser)
        {
            List<Organisation> orgs = null;
            if (currentUser.IsSuperAdmin)
            {
                orgs = _context.Organisations.ToList();
            }
            else if (currentUser.IsOrganisationAdmin || currentUser.IsFacilityAdmin)
            {
                orgs = new List<Organisation>();
                orgs.Add(_context.Organisations.Find(currentUser.Facility.OrganisationId));
            }
            else
            {
                orgs = null;
            }
            return orgs;
        }


        public ActionResult GetAllOrganisations()
        {
            //return Json(GetMatchingOrganisationsForUserRole(LoginController.ActiveUser), JsonRequestBehavior.AllowGet);
            return Json(_context.Organisations.ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMatchingFacilitiesForSelectedOrganisationAndPrimaryFacilty(string selectedOrgs, string selectedPrimary)
        {
            if (selectedOrgs == "---") return null;
            if (selectedPrimary == "---" || selectedPrimary == "0") return null;
            string[] selectionStrings = selectedOrgs.Split(',');
            int[] selections = new int[selectionStrings.Count()];
            for (int i = 0; i < selectionStrings.Count(); i++)
            {
                selections[i] = int.Parse(selectionStrings[i]);
            }

            List<FacilityMaster> facilityMasters = (from facilityMaster in _context.FacilityMasters.AsQueryable()
                                                    where selections.Any(i => facilityMaster.OrganisationId.Equals(i))
                                                    select facilityMaster).OrderBy(x => x.OrganisationId).ToList();

            List<FacilityMaster> finalfacilityMasters = facilityMasters;

            List<FacilityMaster> templist =
                facilityMasters.Where(facility => facility.FacilityMasterId == int.Parse(selectedPrimary)).ToList();

            if (templist.Count > 0)
            {
                foreach (FacilityMaster facility in templist)
                {
                    finalfacilityMasters.Remove(@facility);
                }
            }
            return Json(finalfacilityMasters, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllTitles()
        {
            return Json(_context.Titles.ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllCountries()
        {
            return Json(_context.Countries.ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetStatesBasedOnCountry(int selectedCountry)
        {
            List<State> stateslist = _context.States.Where(x => x.CountryId == selectedCountry).ToList();
            return Json(stateslist, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateUserStatus(int currentRecord, bool currentStatus)
        {
            UserLoginInformation info = _context.UserLoginInformations.Single(x => x.USERID == currentRecord);
            UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;

            if (info.IsActive != currentStatus)
            {
                info.IsActive = currentStatus;
                info.UserIsActive = currentStatus;
                try
                {
                    using (TransactionScope transactionScope = new TransactionScope())
                    {
                        try
                        {
                            using (kryptoEntities1 db = new kryptoEntities1()) // Context object
                            {
                                if (info.IsActive)
                                {
                                    info.ActivatedDate = DateTime.Now;
                                }
                                else
                                {
                                    info.DeactivatedDate = DateTime.Now;
                                }
                                _context.Entry(info).State = EntityState.Modified;
                                _context.SaveChanges();
                                UserActivate useracive = new UserActivate
                                {
                                    CreatedById = loggedinUser.USERID.ToString(),
                                    Date = DateTime.Now,
                                    USERID = info.USERID,
                                    IsActive = info.IsActive
                                };
                                _context.UserActivates.Add(useracive);
                                _context.SaveChanges();
                            }
                            transactionScope.Complete();
                        }
                        catch (Exception Wx)
                        {
                            return Json("Something went Wrong!", JsonRequestBehavior.AllowGet);
                        }
                    }
                    if (info.USERID > 0 && info.IsActive)
                    {
                        OtpSent(info, loggedinUser.USERID);
                    }
                    else if (info.IsActive == false)
                        RemoveUser(info.USERID);
                }
                catch (Exception Ex)
                {
                    return Json("Something went Wrong!", JsonRequestBehavior.AllowGet);
                }
                return Json("Sucessfully Updated the Status", JsonRequestBehavior.AllowGet);
            }
            return Json("No Changes to Update", JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCitiesBasedOnState(int selectedCity)
        {
            List<City> citieslist = _context.Cities.Where(x => x.State == selectedCity).ToList();

            return Json(citieslist, JsonRequestBehavior.AllowGet);
        }

        private List<FacilityMaster> GetMatchingFacilitiesForUserRole(UserLoginInformation currentUser, int[] p_selections)
        {
            List<FacilityMaster> facilityMasters = null;
            if (currentUser.IsSuperAdmin)
            {
                facilityMasters = (from facilityMaster in _context.FacilityMasters
                                   where p_selections.Any(i => facilityMaster.OrganisationId.Equals(i))
                                   select facilityMaster).OrderBy(x => x.OrganisationId).ToList();
            }
            else if (currentUser.IsOrganisationAdmin)
            {
                facilityMasters = (from facilityMaster in _context.FacilityMasters
                                   where p_selections.Any(i => facilityMaster.OrganisationId.Equals(i) && i.Equals(currentUser.Facility.OrganisationId))
                                   select facilityMaster).OrderBy(x => x.OrganisationId).ToList();
            }
            else if (currentUser.IsFacilityAdmin)
            {
                facilityMasters = new List<FacilityMaster>();
                facilityMasters.Add(_context.FacilityMasters.Find(currentUser.FacilityId));
            }
            else
            {
                facilityMasters = null;
            }
            return facilityMasters;
        }

        public ActionResult GetMatchingFacilityMasters(string selectedOrgs)
        {
            if (selectedOrgs == "---") return null;
            string[] selectionStrings = selectedOrgs.Split(',');
            int[] selections = new int[selectionStrings.Count()];
            for (int i = 0; i < selectionStrings.Count(); i++)
            {
                selections[i] = int.Parse(selectionStrings[i]);
            }
            List<FacilityMaster> facilityMasters = GetMatchingFacilitiesForUserRole(LoginController.ActiveUser, selections);
            return Json(facilityMasters, JsonRequestBehavior.AllowGet);
        }

        private bool hasitems(List<UserFacility> list1, List<UserFacility> list2)
        {
            var firstNotSecond = list1.Except(list2).ToList();
            var secondNotFirst = list2.Except(list1).ToList();
            return !firstNotSecond.Any() && !secondNotFirst.Any();
        }

        public static List<int> Union(List<int> firstList, List<int> secondList)
        {
            if (firstList == null)
            {
                return secondList;
            }
            return secondList != null ? firstList.Union(secondList).ToList() : firstList;
        }

        public static List<int> Intersection(List<int> firstList, List<int> secondList)
        {
            if (firstList == null)
            {
                return null;
            }
            return secondList != null ? firstList.Intersect(secondList).ToList() : null;
        }

        public static List<int> ExcludedLeft(List<int> firstList, List<int> secondList)
        {
            return secondList != null ? Union(firstList, secondList).Except(secondList).ToList() : firstList;
        }

        public static List<int> ExcludedRight(List<int> firstList, List<int> secondList)
        {
            return firstList != null ? Union(firstList, secondList).Except(firstList).ToList() : secondList;
        }

        public ActionResult SubResults(String selections)
        {
            List<MyNode> responseNodes = JsonConvert.DeserializeObject<List<MyNode>>(selections);

            List<MyFacility> resultFacilities = new List<MyFacility>();

            List<MyOrganisation> resultOrgs = new List<MyOrganisation>();

            foreach (MyNode @node in responseNodes)
            {
                MyFacility facility = new MyFacility
                {
                    Name = @node.text,
                    Value = @node.value,
                    ParentOrganisationId = @node.parent
                };

                MyOrganisation organisation = ChatGroupController.GetMatchingOrganisation(resultOrgs,
                    facility.GetParentOrganisation());

                if (organisation == null)
                {
                    resultOrgs.Add(facility.GetParentOrganisation());
                }
                resultFacilities.Add(facility);
            }

            foreach (MyFacility @myFacility in resultFacilities)
            {
                foreach (MyOrganisation @organisation in resultOrgs)
                {
                    if (ChatGroupController.GetMatchingFacilty(@organisation.TempFacilities, @myFacility) == null &&
                        ChatGroupController.GetMatchingFacilty(@organisation.GetAllMatchingFacilities(), @myFacility) !=
                        null)
                    {
                        @organisation.TempFacilities.Add(@myFacility);
                    }
                }
            }

            List<MyNode> nodes = new List<MyNode>();
            foreach (MyOrganisation @org in resultOrgs)
            {
                MyNode orgNode = new MyNode
                {
                    text = org.Name,
                    value = org.Value,
                    icon = "glyphicon glyphicon-home",
                    backColor = "#ffffff",
                    color = "#428bca",
                    nodetype = MyNodeType.Organisation
                };
                List<MyFacility> facilities = @org.TempFacilities;
                if (facilities != null && facilities.Count > 0)
                {
                    orgNode.nodes = new List<MyNode>();
                    foreach (MyFacility @fac in facilities)
                    {
                        MyNode facNode = new MyNode
                        {
                            text = fac.Name,
                            value = fac.Value,
                            icon = "glyphicon glyphicon-th-list",
                            backColor = "#ffffff",
                            color = "#66512c",
                            parent = org.Value,
                            nodetype = MyNodeType.Facility
                        };
                        orgNode.nodes.Add(facNode);
                    }
                }
                nodes.Add(orgNode);
            }
            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Result(string selectedOrgs, string selectedPrimary, int currentrecord, bool selectExisting)
        {
            if (selectedOrgs == "---") return null;
            if (selectedPrimary == "---" || selectedPrimary == "0") return null;
            string[] selectionStrings = selectedOrgs.Split(',');
            int[] selections = new int[selectionStrings.Count()];
            for (int i = 0; i < selectionStrings.Count(); i++)
            {
                selections[i] = int.Parse(selectionStrings[i]);
            }

            int selectedPrimaryAsInt = int.Parse(selectedPrimary);

            List<Organisation> organisations = (from orgs in _context.Organisations.AsQueryable()
                                                where selections.Any(i => orgs.OrganisationId.Equals(i))
                                                select orgs).ToList();

            List<MyOrganisation> myOrganisations = organisations.Select(organisation => new MyOrganisation { Name = organisation.Name, Value = organisation.OrganisationId }).ToList();

            List<int> participantsIds = null;
            if (currentrecord != 0)
            {
                UserLoginInformation currentUser = _context.UserLoginInformations.Find(currentrecord);
                participantsIds = currentUser.GetFacilityIdsInUserFacilityList();
            }
            List<MyNode> nodes = new List<MyNode>();

            foreach (MyOrganisation @org in myOrganisations)
            {
                MyNode orgNode = new MyNode
                {
                    text = org.Name,
                    value = org.Value,
                    icon = "glyphicon glyphicon-home",
                    backColor = "#ffffff",
                    color = "#428bca",
                    nodetype = MyNodeType.Organisation
                };
                List<MyFacility> myFacilities = @org.GetAllMatchingFacilities();
                if (myFacilities != null && myFacilities.Count > 0)
                {
                    orgNode.nodes = new List<MyNode>();
                    foreach (MyFacility @fac in myFacilities)
                    {
                        if (@fac.Value != selectedPrimaryAsInt)
                        {
                            MyNode facNode = new MyNode
                            {
                                parent = orgNode.value,
                                text = fac.Name,
                                value = fac.Value,
                                icon = "glyphicon glyphicon-th-list",
                                backColor = "#ffffff",
                                color = "#66512c",
                                nodetype = MyNodeType.Facility
                            };
                            if (participantsIds != null && selectExisting)
                            {
                                if (ChatGroupController.CheckIfMatchingMyFacilityExists(participantsIds, facNode) !=
                                    null)
                                {
                                    facNode.state = new state
                                    {
                                        @checked = true,
                                        disabled = false,
                                        expanded = true,
                                        selected = false
                                    };
                                }
                            }
                            orgNode.nodes.Add(facNode);
                        }
                    }
                }
                nodes.Add(orgNode);
            }
            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        public string UpdateUser(UserLoginInformation ulinfo)
        {
            try
            {
                UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;
                ulinfo.ModifiedById = loggedinUser.USERID.ToString();
                if (ulinfo.USERID == 0)
                {
                    ulinfo.CreatedById = loggedinUser.USERID.ToString();
                    ulinfo.CreatedDate = DateTime.Now;
                    ulinfo.Status = 1;  //insert record status
                    if (ulinfo.IsActive)
                    {
                        ulinfo.UserIsActive = true;
                    }
                    try
                    {
                        using (TransactionScope transactionScope = new TransactionScope())
                        {
                            try
                            {
                                using (kryptoEntities1 db = new kryptoEntities1()) // Context object
                                {
                                    db.UserLoginInformations.Add(ulinfo);
                                    db.SaveChanges();

                                    if (ulinfo.USERID > 0)
                                    {
                                        if (ulinfo.IsActive)
                                        {
                                            ulinfo.ActivatedDate = DateTime.Now;
                                            db.Entry(ulinfo).State = EntityState.Modified;
                                            db.SaveChanges();
                                            UserActivate useracive = new UserActivate();
                                            useracive.CreatedById = loggedinUser.USERID.ToString();
                                            useracive.Date = DateTime.Now;
                                            useracive.USERID = ulinfo.USERID;
                                            useracive.IsActive = ulinfo.IsActive;
                                            useracive.Status = 1;
                                            db.UserActivates.Add(useracive);
                                            db.SaveChanges();
                                        }
                                    }

                                    string[] otherFacilityIds = ulinfo.OtherFacilityIds;
                                    if (ulinfo.USERID > 0 &&
                                        (otherFacilityIds != null && otherFacilityIds.Length > 0))
                                    {
                                        string[] facilyIds = otherFacilityIds;
                                        foreach (string eachid in facilyIds)
                                        {
                                            int facilityid = int.Parse(eachid);
                                            db.UserFacilities.Add(new UserFacility
                                            {
                                                FacilityId = facilityid,
                                                USERID = ulinfo.USERID,
                                                Status = 1,
                                                CreatedById = loggedinUser.USERID.ToString(),
                                                CreatedDate = DateTime.Now,
                                                ModifiedDate = DateTime.Now,
                                                ModifiedById = loggedinUser.USERID.ToString()
                                            });
                                        }
                                        db.SaveChanges();
                                    }
                                    if (ulinfo.USERID > 0)
                                    {
                                        UserRegitrationForInitialLogin initiallogin = new UserRegitrationForInitialLogin();
                                        initiallogin.USERID = ulinfo.USERID;
                                        initiallogin.Createdate = DateTime.Now;
                                        initiallogin.IsInitialLogin = true;
                                        initiallogin.IsTermsAccepted = false;
                                        initiallogin.IsSecQuestEnabled = false;
                                        initiallogin.IsMpinCreated = false;
                                        initiallogin.IsPasswordUpdated = false;
                                        initiallogin.Status = 1;
                                        initiallogin.CreatedById = loggedinUser.USERID.ToString();
                                        initiallogin.ModifiedById = loggedinUser.USERID.ToString();
                                        initiallogin.ModifiedDate = initiallogin.Createdate;
                                        db.UserRegitrationForInitialLogins.Add(initiallogin);
                                        db.SaveChanges();
                                    }

                                }
                                transactionScope.Complete(); // transaction complete
                            }
                            catch (Exception ee)
                            {
                                return "FAIL";
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        return "FAIL";
                    }
                }
                else
                {
                    try
                    {
                        using (TransactionScope transactionScope = new TransactionScope())
                        {
                            try
                            {
                                using (kryptoEntities1 db = new kryptoEntities1())
                                {
                                    ulinfo.ModifiedDate = DateTime.Now;
                                    UserLoginInformation prevobj = _context.UserLoginInformations.Find(ulinfo.USERID);
                                    if (prevobj.IsActive != ulinfo.IsActive)
                                    {
                                        UserActivate activate = new UserActivate();
                                        activate.IsActive = !prevobj.IsActive;
                                        activate.CreatedById = loggedinUser.USERID.ToString();
                                        if (ulinfo.IsActive)
                                        {
                                            ulinfo.ActivatedDate = DateTime.Now;
                                            activate.Date = ulinfo.ActivatedDate;

                                        }
                                        else
                                        {
                                            ulinfo.DeactivatedDate = DateTime.Now;
                                            activate.Date = ulinfo.DeactivatedDate;

                                        }
                                        activate.USERID = prevobj.USERID;
                                        db.UserActivates.Add(activate);
                                    }
                                    ulinfo.UserIsActive = ulinfo.IsActive;
                                    ulinfo = Updateobject(ulinfo.USERID, ulinfo);
                                    db.Entry(ulinfo).State = EntityState.Modified;
                                    db.SaveChanges();

                                    List<int> otherFacilityIdsAsints = ulinfo.GetOtherFacilityIdsAsints();
                                    List<int> facilityIdsInUserFacilityList = ulinfo.GetFacilityIdsInUserFacilityList();
                                    var toAdd = ExcludedRight(facilityIdsInUserFacilityList, otherFacilityIdsAsints);
                                    var toDelete = ExcludedLeft(facilityIdsInUserFacilityList, otherFacilityIdsAsints);
                                    foreach (int @id in toAdd)
                                    {
                                        db.UserFacilities.Add(new UserFacility
                                        {
                                            FacilityId = @id,
                                            USERID = ulinfo.USERID,
                                            Status = 1,
                                            CreatedById = loggedinUser.USERID.ToString(),
                                            CreatedDate = DateTime.Now,
                                            ModifiedDate = DateTime.Now,
                                            ModifiedById = loggedinUser.USERID.ToString()
                                        });
                                    }
                                    foreach (UserFacility existingUserFacility in toDelete.Select(id => db.UserFacilities.SingleOrDefault(x => x.FacilityId.Value.Equals(id) && x.USERID.Equals(ulinfo.USERID))))
                                    {
                                        db.UserFacilities.Remove(existingUserFacility);
                                    }
                                    db.SaveChanges();
                                    if (ulinfo.IsActive == false)
                                    {
                                        RemoveUser(ulinfo.USERID);
                                    }

                                }
                                transactionScope.Complete();
                            }
                            catch (Exception ee)
                            {
                                return "FAIL";
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        return "FAIL";
                    }
                }
                if (ulinfo.USERID > 0 && ulinfo.IsActive)
                {
                    OtpSent(ulinfo, loggedinUser.USERID);
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                return "FAIL";
            }
            return "SUCESS";
        }

        public int DeleteSingleuserRecord(int selecteduser)
         {
            UserLoginInformation user = _context.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(selecteduser));
            if(user.IsSuperAdmin || user.IsOrganisationAdmin || user.IsFacilityAdmin )
            {
                return 1;
            }
            else 
            {
                ChatGroupParticipant chatgroupparticipants = _context.ChatGroupParticipants.FirstOrDefault(x => x.USERID == user.USERID);
                if(chatgroupparticipants==null)
                {
                    user.Status = 2;
                    _context.Entry(user).State = EntityState.Modified;
                    _context.SaveChanges();
                    return 2;
                }
                return 1;
            }



             //set the status 2 for Deleting Record

            //List<UserFacility> faclist = (from uf in _context.UserFacilities
            //                              where uf.USERID == selecteduser
            //                              select uf).ToList();
        
            //foreach (UserFacility removeeachfac in faclist)
            //{
            //    removeeachfac.Status = 2; //set status 2 for deleting record
            //    _context.Entry(removeeachfac).State = EntityState.Modified;

            //}
          

        }

        public ActionResult CreateNew()
        {
            TempData["OpenCreateUser"] = true;
            return RedirectToAction("List", "UserDatatables");
        }

        public ActionResult CheckIfValidEmail(string email2)
        {
            string[] strings = email2.Split(new string[] { "||||" }, StringSplitOptions.None);
            UserLoginInformation res = null;
            string email = strings[0];
            int userid = int.Parse(strings[1]);
            if (userid == 0) res = _context.UserLoginInformations.SingleOrDefault(x => x.EmailId == email && x.Status==1);
            else res = _context.UserLoginInformations.SingleOrDefault(x => x.EmailId == email && x.USERID != userid && x.Status==1);
            if (res != null) return Json("Email Id Already Exists.Use Another Email Id", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckIfValidContactNumber(string phonenumber)
        {
            string[] strings = phonenumber.Split(new string[] { "||||" }, StringSplitOptions.None);
            UserLoginInformation res = null;
            string phonenum = strings[0];
            int userid = int.Parse(strings[1]);
            if (userid == 0)
                res = _context.UserLoginInformations.SingleOrDefault(x => x.ContactNumber == phonenum && x.Status==1);
            else res = _context.UserLoginInformations.SingleOrDefault(x => x.ContactNumber == phonenum && x.USERID != userid && x.Status==1);
            if (res != null) return Json("Contact Number Already Exists.Use Another Contact Number", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult checkifvalidZipcode(string zipcode)
        {
            if (zipcode.Length == 5)
            {
                var zipcodeitem = _context.ZipCodes.FirstOrDefault(x => x.ZipCode1 == zipcode);
                if (zipcodeitem != null)
                {
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
            }
            return Json("Please Enter valid ZipCode", JsonRequestBehavior.AllowGet);
        }

        private static Random random = new Random();

        public static string GenerateOTP(int length)
        {
            kryptoEntities1 krypto = new kryptoEntities1();
            //krypto.Database.
            var res = krypto.Database.SqlQuery<int>("SELECT * from [dbo].fnotpgenerationAdmin(1);").ToList();
            return res[0].ToString();
        }

        public bool SendOTPMail(string sOTP, string sToemailId, string sFirstName)
        {
            bool flag = false;
            string sSmtpServer = WebConfigurationManager.AppSettings["SMTPSERVER"] != null ? WebConfigurationManager.AppSettings["SMTPSERVER"].ToString() : string.Empty;
            string sFromEmail = WebConfigurationManager.AppSettings["MAIL_FRM"] != null ? WebConfigurationManager.AppSettings["MAIL_FRM"].ToString() : string.Empty;
            string sUserId = WebConfigurationManager.AppSettings["MAIL_USR"] != null ? WebConfigurationManager.AppSettings["MAIL_USR"].ToString() : string.Empty;
            string sPassword = WebConfigurationManager.AppSettings["MAIL_PWD"] != null ? WebConfigurationManager.AppSettings["MAIL_PWD"].ToString() : string.Empty;
            string sMailSubject = WebConfigurationManager.AppSettings["REGISTRATION_PASS_MAIL_SUBJECT"] != null ? WebConfigurationManager.AppSettings["REGISTRATION_PASS_MAIL_SUBJECT"].ToString() : string.Empty;
            try
            {
                string msg = "Your KryptosText Application One Time Password (OTP) is   " + sOTP;
                StringBuilder strMailBody = new StringBuilder();
                strMailBody.Append("<table><tr><td> Dear:" + sFirstName + "<br><br></td>");
                strMailBody.Append("</tr>");
                strMailBody.Append("<tr><td>" + msg + "<br></td></tr>");

                //"    For your security, please change the password once logged in using the new OTP.Validity of OTP is 30 Mins only."
                strMailBody.Append("<br><tr><td>For your security reasons, please change the password once logged in using the new OTP.</td></tr>");
                strMailBody.Append("<tr><td>Validity of OTP is 24 Mins only.<br></td></tr>");
                strMailBody.Append("<br><tr><td>Note: This is an auto generated mail please do not reply this mail.</td></tr><br>");
                strMailBody.Append("<br><tr><td> Support Team</td></tr>");
                strMailBody.Append("<tr><td> KryptosText.com</td></tr>");
                strMailBody.Append("</table> ");

                clsEMailHelper objMail = new clsEMailHelper();
                objMail.MailFrom = sFromEmail;
                objMail.SMTPServer = sSmtpServer;
                objMail.UserID = sFromEmail;
                objMail.Password = sPassword;
                objMail.MailTo = sToemailId;
                objMail.Subject = sMailSubject.Replace("$DATE$", System.DateTime.Now.ToString()); ;
                objMail.Body = strMailBody;
                objMail.IsBodyHtml = true;
                flag = objMail.SendMail();
                if (flag == true)
                {
                    Console.WriteLine(Environment.NewLine + "SendMail success");

                }
                else if (flag == false)
                {
                    Console.WriteLine(Environment.NewLine + "SendMail failed");
                }
                objMail = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when SendMail:-" + ex.InnerException);
            }
            return flag;
        }

        public string ResetPswd(int selecteduser)
        {
            UserLoginInformation user = _context.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(selecteduser));
            KPTY_USER_FORGOT_PASS_OTP_REQ_TBL info = new KPTY_USER_FORGOT_PASS_OTP_REQ_TBL();
            UserRegitrationForInitialLogin IntialLogin = new UserRegitrationForInitialLogin();

            var OTP = GenerateOTP(4);
            try
            {
                UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;
                //ResetOTPStatus(loggedinUser.USERID);
                info.USERID = user.USERID;
                info.ModifiedById = loggedinUser.USERID.ToString();
                info.CreatedById = loggedinUser.USERID.ToString();
                info.CREATED_DATE = DateTime.Now;
                info.ModifiedDate = DateTime.Now;
                info.STATUS = 1;
                info.OTPVAL = OTP;
                try
                {
                    using (TransactionScope transactionScope = new TransactionScope())
                    {
                        try
                        {
                            using (kryptoEntities1 db = new kryptoEntities1()) // Context object
                            {

                                db.Database.ExecuteSqlCommand("delete from KPTY_USER_FORGOT_PASS_OTP_REQ_TBL where UserId = {0}", selecteduser);

                                db.KPTY_USER_FORGOT_PASS_OTP_REQ_TBL.Add(info);
                                db.SaveChanges();

                                db.Database.ExecuteSqlCommand("delete from UserRegitrationForInitialLogin where UserId = {0}", selecteduser);

                                IntialLogin.IsInitialLogin = true;
                                IntialLogin.IsTermsAccepted = false;
                                IntialLogin.IsSecQuestEnabled = false;
                                IntialLogin.IsPasswordUpdated = false;
                                IntialLogin.IsMpinCreated = false;
                                IntialLogin.Notes = null;
                                IntialLogin.Status = 1;
                                IntialLogin.ModifiedById = loggedinUser.USERID.ToString();
                                IntialLogin.ModifiedDate = DateTime.Now;
                                IntialLogin.USERID = selecteduser;
                                IntialLogin.Createdate = DateTime.Now;
                                IntialLogin.CreatedById = loggedinUser.USERID.ToString();

                                db.UserRegitrationForInitialLogins.Add(IntialLogin);
                                db.SaveChanges();
                            }
                            transactionScope.Complete();
                            // transaction complete
                            var recemail = user.EmailId;
                            var msg = "Dear User,\n\n Your request to process the reset password is successful and your new OTP generated is  " + OTP + " . Please use it to login again. \n This is system generated message please do not reply.";
                            if (!SendOTPMail(msg, recemail, user.FirstName))
                                return "Invalid Email";
                        }

                        catch (Exception ee)
                        {
                            return "FAIL";
                        }
                    }
                }
                catch (Exception exception)
                {
                    return "FAIL";
                }
            }

            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                return "FAIL";
            }


            return "SUCESS";

        }

        public string ResetOtp(int selectedUser)
        {
            UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;
            UserLoginInformation user = _context.UserLoginInformations.SingleOrDefault(x => x.USERID.Equals(selectedUser));
            UserForgotMpinsOTP UserOtp = new UserForgotMpinsOTP();
            string Number = GenerateOTP(11);
            try
            {
                using (TransactionScope transactionScope = new TransactionScope())
                {
                    try
                    {
                        using (kryptoEntities1 db = new kryptoEntities1()) // Context object
                        {

                            UserOtp.USERID = selectedUser;
                            UserOtp.OTPVAL = Number;
                            UserOtp.STATUS = 2;
                            UserOtp.Notes = "";
                            UserOtp.CREATED_DATE = DateTime.Now;
                            UserOtp.ModifiedDate = DateTime.Now;
                            UserOtp.CreatedById = loggedinUser.USERID.ToString();
                            UserOtp.ModifiedById = loggedinUser.USERID.ToString();

                            db.Database.ExecuteSqlCommand("delete from UserForgotMpinsOTPS where UserId = {0}", selectedUser);

                            db.UserForgotMpinsOTPS.Add(UserOtp);
                            db.SaveChanges();
                        }

                        transactionScope.Complete();

                        var msg = "Dear User, \n\n Your request to process the reset OTP is successful and your new OTP generated is  " + Number + " .  \n\n This is system generated message please do not reply.";

                        bool x1 = SendOTPMail(msg, user.EmailId, user.FirstName);
                        if (!x1)
                            return "Invalid Email";
                    }

                    catch (Exception ee)
                    {
                        return "FAIL";
                    }
                }
            }

            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                return "FAIL";
            }

            return "SUCESS";

        }


        public void OtpSent(UserLoginInformation ulinfo, int LoginUserId)
        {
            // _context.Database.Connection.Open();
            UserRegistrationOTP user = _context.UserRegistrationOTPs.SingleOrDefault(u => u.USERID.Equals(ulinfo.USERID));
            if (user == null)
            {
                bool value = false;
                string Number = GenerateOTP(11);
                using (TransactionScope transactionscope = new TransactionScope())
                {
                    try
                    {
                        UserRegistrationOTP UsRegOtp = new UserRegistrationOTP();
                        UsRegOtp.USERID = ulinfo.USERID;
                        UsRegOtp.OTP = Number;
                        UsRegOtp.Status = 2;
                        UsRegOtp.Notes = "";
                        UsRegOtp.CreatedById = LoginUserId.ToString();
                        UsRegOtp.ModifiedById = LoginUserId.ToString();
                        UsRegOtp.CreatedDate = DateTime.Now;
                        UsRegOtp.ModifiedDate = DateTime.Now;
                        _context.UserRegistrationOTPs.Add(UsRegOtp);
                        _context.SaveChanges();
                        value = true;
                        transactionscope.Complete();
                    }
                    catch (Exception)
                    {
                        value = false;
                    }
                }
                if (value == true)
                    SendOTPMail(Number, ulinfo.EmailId, ulinfo.FirstName + " " + ulinfo.LastName);


            }
        }

        public void RemoveUser(int UserId)
        {
            _context.Database.ExecuteSqlCommand("delete from UserRegistrationOTP where UserId = {0}", UserId);
        }
    }
}
