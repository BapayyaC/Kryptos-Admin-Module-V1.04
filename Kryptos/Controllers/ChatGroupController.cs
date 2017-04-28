using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection;
using System.Transactions;
using System.Web.Mvc;
using Kryptos.Models;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.Util;
using System.IO;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.Configuration;


namespace Kryptos.Controllers
{
    public class ChatGroupController : Controller
    {
          string UploadDirectory;
          public ChatGroupController()
        {
            UploadDirectory = ConfigurationManager.AppSettings["UploadDirectory"];
        }

       kryptoEntities1 _context = new kryptoEntities1();

        public ActionResult List()
        {
            ViewData["current view"] = "Group List";
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

        public ActionResult GroupInfoList()
        {
            return Json(new { aaData = _context.ChatGroups.Where(x=>x.Status==1).ToList() }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GroupExportList()
        {
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

        private void AddCell(IRow header, int Colidx, string CellValue, ICellStyle cellstyle = null)
        {
            ICell cell = header.CreateCell(Colidx);
            if (cellstyle != null) cell.CellStyle = cellstyle;
            cell.SetCellValue(CellValue);
        }



        public ActionResult GenerateReport(int selectedGroup, int selectedtype, int selectedstatus)
        {
            List<ChatGroup> grouplist = new List<ChatGroup>();

            if(selectedGroup==0)
            {
                if(selectedstatus==0)
                {
                    grouplist = new ChatGroup().GetAllChatGroups();
                }
                else if(selectedstatus==1)
                {
                    grouplist = new ChatGroup().GetActiveChatGroups();
                }
                else
                {
                    grouplist = new ChatGroup().GetInActiveChatGroups();
                }
                return performActionForReportType(selectedtype, grouplist);
            }
            else
            {
                List<ChatGroupParticipant> chatgroupparticipants;

                ChatGroup chatgroup = _context.ChatGroups.Find(selectedGroup);
                if(selectedstatus==0)
                {
                    chatgroupparticipants = chatgroup.GetAssociatedChatGroupParticipants();

                }
                else if(selectedstatus==1)
                {
                    chatgroupparticipants = chatgroup.GetAssociatedActiveChatGroupParticipants();
                }
                else
                {
                    chatgroupparticipants = chatgroup.GetAssociatedInActiveChatGroupParticipants();

                }


                return performActionForReportTypeForGroupUser(selectedtype, chatgroupparticipants);
                
            }

        }

        private bool performActionForSelectedstatus(int selectedstatus)
        {
            switch (selectedstatus)
            {
                case 0:
                    return false;
                case 1:
                case 2:
                    return true;
            }
            return false;
        }

        private ActionResult performActionForReportType(int selectedtype, List<ChatGroup> users)
        {
            if (selectedtype == 0)
            {
                return ExportToExcel(users);
            }
            else
            {
                return ExportToPDF(users);
            }
        }
      

        public ActionResult ExportToExcel(List<ChatGroup> matchinggroup)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet groups = workbook.CreateSheet("ChatGroup");

            IFont boldFont = workbook.CreateFont();
            boldFont.Boldweight = (short)FontBoldWeight.Bold;
            boldFont.FontName = "Arial, Helvetica, sans-serif";
            boldFont.FontHeight = 10;

            ICellStyle headerstyle = workbook.CreateCellStyle();
            headerstyle.SetFont(boldFont);
            headerstyle.BorderBottom = BorderStyle.Medium;
            headerstyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            headerstyle.FillPattern = FillPattern.SolidForeground;

            IRow headerRow = groups.CreateRow(0);

            int columnIndx = 0;
            AddCell(headerRow, columnIndx++, "Group Name", headerstyle);
            AddCell(headerRow, columnIndx++, "Status", headerstyle);
            AddCell(headerRow, columnIndx++, "Created date", headerstyle);
            AddCell(headerRow, columnIndx, "About Group", headerstyle);



            int currentRow = 1;
            foreach (ChatGroup @chatgroup in matchinggroup)
            {
                columnIndx = 0;
                IRow dataRow = groups.CreateRow(currentRow++);

                AddCell(dataRow, columnIndx++, @chatgroup.GroupName);
                if(@chatgroup.IsActive.Value)
            
                    AddCell(dataRow, columnIndx++, "Active");
                else
                    AddCell(dataRow, columnIndx++, "In Active");
               
                AddCell(dataRow, columnIndx++, @chatgroup.CreatedDate.ToString());
                AddCell(dataRow, columnIndx, @chatgroup.Notes);

            }

         
             string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "groups_" + LoginController.ActiveUser_SESSIONID + ".xlsx");
            using (
                var fs = new FileStream(FilePath, FileMode.Create,
                    FileAccess.Write))
            {
                workbook.Write(fs);
            }
            FilePath =  "groups_" + LoginController.ActiveUser_SESSIONID + ".xlsx";
            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToPDF(List<ChatGroup> matchinggroup)
        {
            string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "groups_" + LoginController.ActiveUser_SESSIONID + ".pdf");

           // string FilePath = Path.Combine(Server.MapPath("../Uploads"), "groups_" + LoginController.ActiveUser_SESSIONID + ".pdf");
            using (var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
            {
                Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                //document.AddAuthor("Micke Blomquist");
                //document.AddCreator("Sample application using iTextSharp");
                //document.AddKeywords("PDF tutorial education");
                //document.AddSubject("Document subject - Describing the steps creating a PDF document");
                //document.AddTitle("The document title - PDF creation using iTextSharp");
                document.Open();

                StringBuilder sb = new StringBuilder();
                sb.Append("<table style=\"color:black;font-family: Arial, Helvetica, sans-serif;font-size: 8px;width:100%;max-width:100%;margin-bottom:20px;border:1px solid #ddd;\">");
                sb.Append("<thead>");
                sb.Append("<tr style=\"color: black;font-family:Arial, Helvetica, sans-serif; font-size:10px;font-weight:bold;border-bottom: 1px solid black; \">");

                sb.Append("<td>");
                sb.Append("Group Name");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Status");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Created Date");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("About Group");
                sb.Append("</td>");


                sb.Append("</tr>");
                //sb.Append("<tr><td colspan=22><hr></td></tr>");
                sb.Append("</thead>");

                sb.Append("<tbody>");
                foreach (ChatGroup @chatgroup in matchinggroup)
                {
                    sb.Append("<tr>");

                    sb.Append("<td>");
                    sb.Append(@chatgroup.GroupName);
                    sb.Append("</td>");
                    string status="";
                    if (@chatgroup.IsActive.Value)
                         status = "Active";
                    else
                        status = "In Active";
                    sb.Append("<td>");
                    sb.Append(status);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@chatgroup.CreatedDate);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@chatgroup.Notes);
                    sb.Append("</td>");


                    sb.Append("</tr>");
                }
                sb.Append("</tbody>");
                sb.Append("</table>");

                //TextReader reader = new StringReader("<p class=\"error\"> HEADING 1 </p>");
                TextReader reader = new StringReader(sb.ToString());
                using (var htmlWorker = new HTMLWorker(document))
                {
                    htmlWorker.Parse(reader);
                }

                //document.Add(new Paragraph("Hello World!"));
                document.Close();
                writer.Close();
                fs.Close();
            }
            FilePath = "groups_" + LoginController.ActiveUser_SESSIONID + ".pdf";
           
           // FilePath = LoginController.DomainName + "/Uploads" + "/groups_" + LoginController.ActiveUser_SESSIONID + ".pdf";
            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }




        public ActionResult ExportToExcelForGroupUser(List<ChatGroupParticipant> matchingGroupParticipants)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet groups = workbook.CreateSheet("ChatGroup");

            IFont boldFont = workbook.CreateFont();
            boldFont.Boldweight = (short)FontBoldWeight.Bold;
            boldFont.FontName = "Arial, Helvetica, sans-serif";
            boldFont.FontHeight = 10;

            ICellStyle headerstyle = workbook.CreateCellStyle();
            headerstyle.SetFont(boldFont);
            headerstyle.BorderBottom = BorderStyle.Medium;
            headerstyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            headerstyle.FillPattern = FillPattern.SolidForeground;

            IRow headerRow = groups.CreateRow(0);

            int columnIndx = 0;
            AddCell(headerRow, columnIndx++, "Group Name", headerstyle);
            AddCell(headerRow, columnIndx++, "About Group", headerstyle);
            AddCell(headerRow, columnIndx++, "Status", headerstyle);
            AddCell(headerRow, columnIndx++, "User", headerstyle);
            AddCell(headerRow, columnIndx++, "Organization", headerstyle);
            AddCell(headerRow, columnIndx, "Facility", headerstyle);




            int currentRow = 1;
            foreach (ChatGroupParticipant Participant in matchingGroupParticipants)
            {
                columnIndx = 0;
                IRow dataRow = groups.CreateRow(currentRow++);

                AddCell(dataRow, columnIndx++, Participant.Group.GroupName);
                AddCell(dataRow, columnIndx++, Participant.Notes);
                if (Participant.Group.IsActive.Value)
                    AddCell(dataRow, columnIndx++, "Active");
                else
                    AddCell(dataRow, columnIndx++, "In Active");
                AddCell(dataRow, columnIndx++, Participant.User.EmailId);
                AddCell(dataRow, columnIndx++, Participant.User.OrganisationName);
                AddCell(dataRow, columnIndx,   Participant.User.Facility.FacilityMasterName);



            }
            string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "Usergroups_" + LoginController.ActiveUser_SESSIONID + ".xlsx");
            using (
                var fs = new FileStream(FilePath, FileMode.Create,
                    FileAccess.Write))
            {
                workbook.Write(fs);
            }
            FilePath = "Usergroups_" + LoginController.ActiveUser_SESSIONID + ".xlsx";
         
            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToPDFForGroupUser(List<ChatGroupParticipant> Groupparticipants)
        {
            string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "Usergroups_" + LoginController.ActiveUser_SESSIONID + ".pdf");
            using (var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
            {
                Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                //document.AddAuthor("Micke Blomquist");
                //document.AddCreator("Sample application using iTextSharp");
                //document.AddKeywords("PDF tutorial education");
                //document.AddSubject("Document subject - Describing the steps creating a PDF document");
                //document.AddTitle("The document title - PDF creation using iTextSharp");
                document.Open();

                StringBuilder sb = new StringBuilder();
                sb.Append("<table style=\"color:black;font-family: Arial, Helvetica, sans-serif;font-size: 8px;width:100%;max-width:100%;margin-bottom:20px;border:1px solid #ddd;\">");
                sb.Append("<thead>");
                sb.Append("<tr style=\"color: black;font-family:Arial, Helvetica, sans-serif; font-size:10px;font-weight:bold;border-bottom: 1px solid black; \">");

                sb.Append("<td>");
                sb.Append("Group Name");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("About Group");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Status");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("User");
                sb.Append("</td>");


                sb.Append("<td>");
                sb.Append("Organization");
                sb.Append("</td>");


                sb.Append("<td>");
                sb.Append("Facility");
                sb.Append("</td>");


                sb.Append("</tr>");
                //sb.Append("<tr><td colspan=22><hr></td></tr>");
                sb.Append("</thead>");

                sb.Append("<tbody>");
                foreach (ChatGroupParticipant participant in Groupparticipants)
                {
                    sb.Append("<tr>");

                    sb.Append("<td>");
                    sb.Append(participant.Group.GroupName);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(participant.Notes);
                    sb.Append("</td>");


                    string status = "";
                    if (participant.Group.IsActive.Value)
                        status = "Active";
                    else
                        status = "In Active";

                    sb.Append("<td>");
                    sb.Append(status);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(participant.User.EmailId);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(participant.User.OrganisationName);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(participant.User.Facility.FacilityMasterName);
                    sb.Append("</td>");


                    sb.Append("</tr>");
                }
                sb.Append("</tbody>");
                sb.Append("</table>");

                //TextReader reader = new StringReader("<p class=\"error\"> HEADING 1 </p>");
                TextReader reader = new StringReader(sb.ToString());
                using (var htmlWorker = new HTMLWorker(document))
                {
                    htmlWorker.Parse(reader);
                }

                //document.Add(new Paragraph("Hello World!"));
                document.Close();
                writer.Close();
                fs.Close();
            }
            FilePath = "Usergroups_" + LoginController.ActiveUser_SESSIONID + ".pdf";
           // FilePath = LoginController.DomainName + "/Uploads" + "/Usergroups_" + LoginController.ActiveUser_SESSIONID + ".pdf";
            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }


        private ActionResult performActionForReportTypeForGroupUser(int selectedtype, List<ChatGroupParticipant> users)
        {
            if (selectedtype == 0)
            {
                return ExportToExcelForGroupUser(users);
            }
            else
            {
                return ExportToPDFForGroupUser(users);
            }
        }

        public ActionResult GetAllChatGroups()
        {
            return Json(_context.ChatGroups.ToList(), JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetMatchingGroup(int selectedgroup)
        {
            ChatGroup groupinfo = _context.ChatGroups.SingleOrDefault(x => x.GroupId.Equals(selectedgroup));
            return Json(groupinfo, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllGroupTypes()
        {
            return Json(_context.GroupTypes.ToList(), JsonRequestBehavior.AllowGet);
        }


        public string UpdateChatGroup(ChatGroup groupinfo)
        {
            try
            {
                UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;
                if (groupinfo.GroupId == 0)
                {
                    try
                    {
                        using (TransactionScope transactionScope = new TransactionScope())
                        {
                            try
                            {
                                using (kryptoEntities1 db = new kryptoEntities1())
                                {
                                    groupinfo.USERID = loggedinUser.USERID;
                                    groupinfo.ModifiedById = loggedinUser.USERID.ToString();
                                    groupinfo.CreatedById = loggedinUser.USERID.ToString();
                                    groupinfo.CreatedDate = DateTime.Now;
                                    groupinfo.Status = 1;
                                   // groupinfo.GroupType = 1;
                                    db.ChatGroups.Add(groupinfo);
                                    db.SaveChanges();
                                    if (groupinfo.GroupId > 0)
                                    {
                                        List<MyNode> responseNodes =
                                            JsonConvert.DeserializeObject<List<MyNode>>(groupinfo.UserSelections);

                                        foreach (MyNode @node in responseNodes)
                                        {
                                            ChatGroupParticipant participant = new ChatGroupParticipant
                                            {
                                                USERID = @node.value,
                                                GroupId = groupinfo.GroupId,
                                                CreatedById = loggedinUser.USERID.ToString(),
                                                ModifiedById = loggedinUser.USERID.ToString(),
                                                CreatedDate = DateTime.Now,
                                                ModifiedDate = DateTime.Now,
                                                IsActive = true,
                                                IsAdmin = false,
                                                Status=1
                                            };
                                            db.ChatGroupParticipants.Add(participant);
                                        }
                                        db.SaveChanges();
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
                                    groupinfo = Updateobject(groupinfo.GroupId, groupinfo);
                                    groupinfo.ModifiedById = loggedinUser.USERID.ToString();
                                    groupinfo.ModifiedDate = DateTime.Now;
                                    db.Entry(groupinfo).State = EntityState.Modified;
                                    db.SaveChanges();

                                    List<MyNode> responseNodes =
                                        JsonConvert.DeserializeObject<List<MyNode>>(groupinfo.UserSelections);
                                    List<ChatGroupParticipant> participantsInDb =
                                        _context.ChatGroupParticipants.Where(x => x.GroupId == groupinfo.GroupId)
                                            .ToList();

                                    List<int> indb = participantsInDb.Select(x => x.USERID).ToList();
                                    List<int> inselections = responseNodes.Select(x => x.value).ToList();

                                    var toAdd = UserDatatablesController.ExcludedRight(indb, inselections);
                                    var toDelete = UserDatatablesController.ExcludedLeft(indb, inselections);

                                    foreach (int @id in toAdd)
                                    {
                                        db.ChatGroupParticipants.Add(new ChatGroupParticipant
                                        {
                                            USERID = @id,
                                            GroupId = groupinfo.GroupId,
                                            CreatedById = loggedinUser.USERID.ToString(),
                                            ModifiedById = loggedinUser.USERID.ToString(),
                                            CreatedDate = DateTime.Now,
                                            ModifiedDate = DateTime.Now,
                                            IsActive = true,
                                            IsAdmin = false,
                                            Status=1
                                        });
                                    }
                                    foreach (
                                        ChatGroupParticipant existingChatGroupParticipant in
                                            toDelete.Select(
                                                id =>
                                                    db.ChatGroupParticipants.SingleOrDefault(
                                                        x => x.USERID.Equals(id) && x.GroupId.Equals(groupinfo.GroupId)))
                                        )
                                    {
                                        db.ChatGroupParticipants.Remove(existingChatGroupParticipant);
                                    }
                                    db.SaveChanges();
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


        public ChatGroup Updateobject(int id, ChatGroup filled)
        {
            ChatGroup obj = _context.ChatGroups.Find(id);
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
                    prop.SetValue(filled, (String)currentprop, null);
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


        public void DeleteSingleGroupRecord(int selectedgroup)
        {
            ChatGroup group = _context.ChatGroups.SingleOrDefault(x => x.GroupId.Equals(selectedgroup));
            group.Status = 2;
            _context.Entry(group).State = EntityState.Modified;
            List<ChatGroupParticipant> participantlist = (from cgp in _context.ChatGroupParticipants
                                                where cgp.GroupId == selectedgroup
                                                select cgp).ToList();

            foreach (ChatGroupParticipant eachparticipant in participantlist)
            {
                eachparticipant.Status = 2;
                _context.Entry(eachparticipant).State = EntityState.Modified;
            }
            _context.SaveChanges();
        }


        public static MyOrganisation GetMatchingOrganisation(List<MyOrganisation> organisations, MyOrganisation organisation)
        {
            return organisations.FirstOrDefault(each => @each.Value == organisation.Value);
        }

        public static MyFacility GetMatchingFacilty(List<MyFacility> facilities, MyFacility facility)
        {
            return facilities.FirstOrDefault(each => @each.Value == facility.Value);
        }

        public static MyUser CheckIfUserBelongsToAFacilty(List<MyUser> users, MyUser user)
        {
            return users.FirstOrDefault(each => @each.Value == user.Value);
        }

        public ActionResult SubResults(String selections)
        {
            List<MyNode> responseNodes = JsonConvert.DeserializeObject<List<MyNode>>(selections);

            List<MyUser> resultUsers = new List<MyUser>();

            List<MyOrganisation> resultOrgs = new List<MyOrganisation>();

            foreach (MyNode @node in responseNodes)
            {
                MyUser user = new MyUser
                {
                    Name = @node.text,
                    Value = @node.value,
                    ParentFacilityId = @node.parent
                };

                MyOrganisation organisation = GetMatchingOrganisation(resultOrgs,
                    user.GetParentFacility().GetParentOrganisation());

                if (organisation == null)
                {
                    resultOrgs.Add(user.GetParentFacility().GetParentOrganisation());
                }
                resultUsers.Add(user);
            }

            foreach (MyUser @myUser in resultUsers)
            {
                foreach (MyOrganisation @organisation in resultOrgs)
                {
                    if (GetMatchingFacilty(@organisation.TempFacilities, @myUser.GetParentFacility()) == null &&
                        GetMatchingFacilty(@organisation.GetAllMatchingFacilities(), @myUser.GetParentFacility()) !=
                        null)
                    {
                        @organisation.TempFacilities.Add(@myUser.GetParentFacility());
                    }
                }
            }

            foreach (MyUser @myUser in resultUsers)
            {
                foreach (MyOrganisation organisation in resultOrgs)
                {
                    foreach (MyFacility facility in organisation.TempFacilities)
                    {
                        if (CheckIfUserBelongsToAFacilty(facility.GetAllMatchingUsers(), @myUser) != null)
                        {
                            @facility.TempUsers.Add(@myUser);
                        }
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
                        List<MyUser> users = @fac.TempUsers;
                        if (users != null && users.Count > 0)
                        {
                            facNode.nodes = new List<MyNode>();
                            foreach (MyUser @user in users)
                            {
                                MyNode userNode = new MyNode
                                {
                                    text = user.Name,
                                    value = user.Value,
                                    icon = "glyphicon glyphicon-user",
                                    backColor = "#ffffff",
                                    color = "#31708f",
                                    parent = fac.Value,
                                    nodetype = MyNodeType.User
                                };
                                facNode.nodes.Add(userNode);
                            }
                        }
                        orgNode.nodes.Add(facNode);
                    }
                }
                nodes.Add(orgNode);
            }
            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckIfValidGroupName(string group)
        {
            string[] strings = group.Split(new string[] { "||||" }, StringSplitOptions.None);
            ChatGroup res = null;
            string gname = strings[0];
            int groupid = int.Parse(strings[1]);
            if (groupid == 0) res = _context.ChatGroups.SingleOrDefault(x => x.GroupName == gname);
            else res = _context.ChatGroups.SingleOrDefault(x => x.GroupName == gname && x.GroupId != groupid);
            if (res != null) return Json("GroupName Already Exists.Use Another Group Name", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Result(int currentrecord)
        {
            ChatGroup currentGroup = null;
            List<int> participantsIds = null;
            if (currentrecord != 0)
            {
                currentGroup = _context.ChatGroups.Find(currentrecord);
                participantsIds = currentGroup.GetAssociatedChatGroupParticipants().Select(x => x.USERID).ToList();
            }
            List<MyNode> nodes = new List<MyNode>();
            foreach (MyOrganisation @org in new MyOrganisation().GetAllOrganisations())
            {
                MyNode orgNode = new MyNode
                {
                    text = org.Name,
                    value = org.Value,
                    icon = "glyphicon glyphicon-home",
                    backColor = "#ffffff",
                    color = "#428bca",
                    //state = new state() { @checked = true, disabled = false, expanded = true, selected = false },
                    nodetype = MyNodeType.Organisation
                };
                List<MyFacility> facilities = @org.GetAllMatchingFacilities();
                if (facilities != null && facilities.Count > 0)
                {
                    orgNode.nodes = new List<MyNode>();
                    foreach (MyFacility @fac in facilities)
                    {
                        MyNode facNode = new MyNode
                        {
                            parent = orgNode.value,
                            text = fac.Name,
                            value = fac.Value,
                            icon = "glyphicon glyphicon-th-list",
                            backColor = "#ffffff",
                            color = "#66512c",
                            //state = new state() { @checked = true, disabled = false, expanded = true, selected = false },
                            nodetype = MyNodeType.Facility
                        };
                        List<MyUser> users = @fac.GetAllMatchingUsers();
                        if (users != null && users.Count > 0)
                        {
                            facNode.nodes = new List<MyNode>();
                            foreach (MyUser @user in users)
                            {
                                MyNode userNode = new MyNode
                                {
                                    parent = facNode.value,
                                    text = user.Name,
                                    value = user.Value,
                                    icon = "glyphicon glyphicon-user",
                                    backColor = "#ffffff",
                                    color = "#31708f",
                                    nodetype = MyNodeType.User
                                };
                                if (currentGroup != null)
                                {
                                    if (CheckIfMatchingMyUserExists(participantsIds, userNode) != null)
                                    {
                                        userNode.state = new state()
                                        {
                                            @checked = true,
                                            disabled = false,
                                            expanded = true,
                                            selected = false
                                        };
                                    }
                                }
                                facNode.nodes.Add(userNode);
                            }
                        }
                        orgNode.nodes.Add(facNode);
                    }
                }
                nodes.Add(orgNode);
            }

            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        public static MyNode CheckIfMatchingMyUserExists(List<int> ids, MyNode currentMyUser)
        {
            return ids.Any(eachId => @eachId == currentMyUser.value) ? currentMyUser : null;
        }

        public static MyNode CheckIfMatchingMyFacilityExists(List<int> ids, MyNode currentMyFacility)
        {
            return ids.Any(eachId => @eachId == currentMyFacility.value) ? currentMyFacility : null;
        }
    }

    public enum MyNodeType
    {
        User,
        Facility,
        Organisation
    }

    [Serializable]
    public class MyNode
    {
        public MyNodeType nodetype { get; set; }
        public string text { get; set; }
        public int value { get; set; }
        public int parent { get; set; }
        public string icon { get; set; }
        public string color { get; set; }
        public string backColor { get; set; }
        public state state { get; set; }
        public List<MyNode> nodes { get; set; }
    }

    [Serializable]
    public class state
    {
        public bool @checked { get; set; }
        public bool disabled { get; set; }
        public bool expanded { get; set; }
        public bool selected { get; set; }
    }

    public class MyOrganisation
    {
        private  kryptoEntities1 _context = new kryptoEntities1();
        public string Name { get; set; }
        public int Value { get; set; }


        private List<MyFacility> _tempFacilities;

        public List<MyFacility> TempFacilities
        {
            get { return _tempFacilities ?? (_tempFacilities = new List<MyFacility>()); }
        }

        public List<MyFacility> GetAllMatchingFacilities()
        {
            List<MyFacility> list = new List<MyFacility>();
            foreach (
                var @user in
                    _context.FacilityMasters.Where(user => user.OrganisationId == Value)
                        .Select(x => new { x.FacilityMasterId, x.FacilityMasterName, x.OrganisationId }))
            {
                list.Add(new MyFacility
                {
                    Name = user.FacilityMasterName,
                    Value = user.FacilityMasterId,
                    ParentOrganisationId = user.OrganisationId
                });
            }
            return list;
        }

        public List<MyOrganisation> GetAllOrganisations()
        {
            List<MyOrganisation> list = new List<MyOrganisation>();
            foreach (
                var facility in
                    _context.Organisations.Select(
                        x => new { x.Name, x.OrganisationId }))
            {
                list.Add(new MyOrganisation
                {
                    Name = facility.Name,
                    Value = facility.OrganisationId,
                });
            }
            return list;
        }

        public MyOrganisation GetMatchingOrganisation(int orgId)
        {
            var fetchOrg =
                _context.Organisations.Where(x=>x.Status==1).Select(x => new { x.Name, x.OrganisationId })
                    .Single(x => x.OrganisationId == orgId);
            return new MyOrganisation { Name = fetchOrg.Name, Value = fetchOrg.OrganisationId };
        }
    }

    public class MyFacility
    {
         kryptoEntities1 _context = new kryptoEntities1();
        public string Name { get; set; }
        public int Value { get; set; }
        public int ParentOrganisationId { get; set; }

        private List<MyUser> _tempUsers;

        public List<MyUser> TempUsers
        {
            get { return _tempUsers ?? (_tempUsers = new List<MyUser>()); }
        }

        public MyOrganisation GetParentOrganisation()
        {
            var res =
                _context.Organisations.Where(x => x.OrganisationId == ParentOrganisationId && x.Status==1)
                    .Select(x => new { x.Name, x.OrganisationId })
                    .Single();
            MyOrganisation facility = new MyOrganisation
            {
                Name = res.Name,
                Value = res.OrganisationId,
            };
            return facility;
        }

        public List<MyUser> GetAllMatchingUsers()
        {
            List<MyUser> list = new List<MyUser>();
            foreach (
                var @user in
                    _context.UserLoginInformations.Where(user => user.FacilityId.Value == Value && user.Status==1)
                        .Select(x => new { x.EmailId, x.USERID, x.FacilityId }))
            {
                list.Add(new MyUser
                {
                    Name = user.EmailId,
                    Value = user.USERID,
                    ParentFacilityId = user.FacilityId.Value
                });
            }
            return list;
        }

        public List<MyFacility> GetAllFacilities()
        {
            List<MyFacility> list = new List<MyFacility>();
            foreach (
                var facility in
                    _context.FacilityMasters.Where(x=>x.Status==1).Select(
                        x => new { x.FacilityMasterName, x.FacilityMasterId, x.OrganisationId }))
            {
                list.Add(new MyFacility
                {
                    Name = facility.FacilityMasterName,
                    Value = facility.FacilityMasterId,
                    ParentOrganisationId = facility.OrganisationId
                });
            }
            return list;
        }

        public MyFacility GetMatchingFacility(int facId)
        {
            var fetchOrg =
                _context.FacilityMasters.Where(x=>x.Status==1).Select(x => new { x.FacilityMasterName, x.FacilityMasterId })
                    .Single(x => x.FacilityMasterId == facId);
            return new MyFacility { Name = fetchOrg.FacilityMasterName, Value = fetchOrg.FacilityMasterId };
        }
    }

    public class MyUser
    {
         kryptoEntities1 _context = new kryptoEntities1();
        public string Name { get; set; }
        public int Value { get; set; }
        public int ParentFacilityId { get; set; }

        public MyFacility GetParentFacility()
        {
            var res =
                _context.FacilityMasters.Where(x => x.FacilityMasterId == ParentFacilityId && x.Status == 1)
                    .Select(x => new { x.FacilityMasterName, x.FacilityMasterId, x.OrganisationId })
                    .Single();
            MyFacility facility = new MyFacility
            {
                Name = res.FacilityMasterName,
                Value = res.FacilityMasterId,
                ParentOrganisationId = res.OrganisationId
            };
            return facility;
        }

        public List<MyUser> GetAllUsers()
        {
            List<MyUser> list = new List<MyUser>();
            foreach (
                var user in _context.UserLoginInformations.Where(x=>x.Status ==1).Select(x => new { x.EmailId, x.USERID, x.FacilityId }))
            {
                list.Add(new MyUser
                {
                    Name = user.EmailId,
                    Value = user.USERID,
                    ParentFacilityId = user.FacilityId.Value
                });
            }
            return list;
        }

    

    }
}
