using Kryptos.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using iTextSharp.text;
using NPOI.HSSF.Util;
using System.Configuration;


namespace Kryptos.Controllers
{
    public class FacilityController : Controller
    {

        string UploadDirectory;
        public FacilityController()
        {
            UploadDirectory = ConfigurationManager.AppSettings["UploadDirectory"];
        }
        //
        // GET: /Facility/
        kryptoEntities1 _context = new kryptoEntities1();

        public ActionResult List()
        {
            ViewData["current view"] = "Facility List";
            ViewData["current grid"] = "CurrentGridSelection";
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


        public ActionResult FacilityExportList()
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

        //private List<FacilityMaster> GetMatchingOrganizationsForUserRole(UserLoginInformation currentUser)
        //{
        //    List<FacilityMaster> facility = null;
        //    if (currentUser.IsSuperAdmin)
        //    {
        //        facility = _context.FacilityMasters.ToList();
        //    }
        //    else if (currentUser.IsOrganisationAdmin)
        //    {
        //        facility = (from fac in _context.FacilityMasters
        //                    where fac.OrganisationId == currentUser.Facility.OrganisationId
        //                    select fac).ToList();

        //    }
        //    else
        //    {
        //        facility = null;
        //    }
        //    return facility;
        //}


        private List<Organisation> GetmacthingOrganizationforfacilityForUserRole(UserLoginInformation currentuser)
        {
            List<Organisation> orgs = null;
            if (currentuser.IsSuperAdmin)
            {
                orgs = _context.Organisations.Where(x=>x.Status==1).ToList();
            }
            else if (currentuser.IsOrganisationAdmin)
            {
                orgs = (from org in _context.Organisations
                        where org.OrganisationId == currentuser.Facility.OrganisationId && org.Status==1
                        select org).ToList();
            }
            return orgs;
        }
        public ActionResult FacilityInfoList()
        {
            return Json(new { aaData = _context.FacilityMasters.Where(x=>x.Status==1).ToList() }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetAllOrganizations()
        {
            return Json(GetmacthingOrganizationforfacilityForUserRole(LoginController.ActiveUser), JsonRequestBehavior.AllowGet);
        }


        public JsonResult getMatchingFacility(int selectedfacility)
        {
            FacilityMaster facilityinfo = _context.FacilityMasters.SingleOrDefault(x => x.FacilityMasterId.Equals(selectedfacility) && x.Status==1);
            return Json(facilityinfo, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Result(int currentrecord)
        {
            FacilityMaster @facility = _context.FacilityMasters.Find(currentrecord);
            Organisation currentOrganisation = @facility.OOrganisation;
            List<int> participantsIds = null;
            if (currentrecord != 0)
            {
                List<UserLoginInformation> usersInDb = new List<UserLoginInformation>();

                usersInDb.AddRange(@facility.GetAssocaitedFacilityAdmins());

                participantsIds = usersInDb.Select(x => x.USERID).ToList();
            }
            List<MyNode> nodes = new List<MyNode>();

            MyOrganisation @org = new MyOrganisation();

            @org.Name = currentOrganisation.Name;
            @org.Value = currentOrganisation.OrganisationId;

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

            List<MyFacility> facilities = new List<MyFacility>
            {
                new MyFacility
                {
                    Name = @facility.FacilityMasterName,
                    ParentOrganisationId = @facility.OrganisationId,
                    Value = @facility.FacilityMasterId
                }
            };
            if (facilities.Count > 0)
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
                            if (ChatGroupController.CheckIfMatchingMyUserExists(participantsIds, userNode) != null)
                            {
                                userNode.state = new state
                                {
                                    @checked = true,
                                    disabled = false,
                                    expanded = true,
                                    selected = false
                                };
                            }
                            facNode.nodes.Add(userNode);
                        }
                    }
                    orgNode.nodes.Add(facNode);
                }
            }
            nodes.Add(orgNode);
            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        public string UpdateFacilittMaster(FacilityMaster facilityinfo)
        {
            try
            {
                UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;
                if (facilityinfo.FacilityMasterId == 0)
                {
                    facilityinfo.CreatedById = loggedinUser.USERID.ToString();
                    facilityinfo.ModifiedById = loggedinUser.USERID.ToString();
                    facilityinfo.CreatedDate = DateTime.Now;
                    facilityinfo.ModifiedDate = DateTime.Now;
                    facilityinfo.Status = 1;
                    _context.FacilityMasters.Add(facilityinfo);
                    _context.SaveChanges();
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
                                    facilityinfo = Updateobject(facilityinfo.FacilityMasterId, facilityinfo);
                                    facilityinfo.ModifiedById = loggedinUser.USERID.ToString();
                                    facilityinfo.ModifiedDate = DateTime.Now;
                                    db.Entry(facilityinfo).State = EntityState.Modified;
                                    db.SaveChanges();

                                    List<MyNode> responseNodes =
                                        JsonConvert.DeserializeObject<List<MyNode>>(facilityinfo.UserSelections);
                                    List<UserLoginInformation> usersInDb = new List<UserLoginInformation>();

                                    usersInDb.AddRange(facilityinfo.GetAssocaitedFacilityAdmins());

                                    List<int> indb = usersInDb.Select(x => x.USERID).ToList();
                                    List<int> inselections = responseNodes.Select(x => x.value).ToList();

                                    var toInclude = UserDatatablesController.ExcludedRight(indb, inselections);
                                    var toExclude = UserDatatablesController.ExcludedLeft(indb, inselections);

                                    foreach (int @in in toInclude)
                                    {
                                        UserLoginInformation current = db.UserLoginInformations.Find(@in);
                                        current.IsFacilityAdmin = true;
                                        db.Entry(current).State = EntityState.Modified;
                                    }
                                    foreach (int @out in toExclude)
                                    {
                                        UserLoginInformation current = db.UserLoginInformations.Find(@out);
                                        current.IsFacilityAdmin = false;
                                        db.Entry(current).State = EntityState.Modified;
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


        public FacilityMaster Updateobject(int id, FacilityMaster filled)
        {
            FacilityMaster obj = _context.FacilityMasters.Find(id);
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

        public ActionResult CheckIfValidFacilityName(string facility)
        {
            string[] strings = facility.Split(new string[] { "||||" }, StringSplitOptions.None);
            FacilityMaster res = null;
            string name = strings[0];
            int facilityid = int.Parse(strings[1]);
            if (facilityid == 0) res = _context.FacilityMasters.SingleOrDefault(x => x.FacilityMasterName == name && x.Status==1);
            else res = _context.FacilityMasters.SingleOrDefault(x =>x.FacilityMasterName==name && x.FacilityMasterId != facilityid &&x.Status==1);
            if (res != null) return Json("FacilityName Already Exists.Use Another Facility Name", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }



        public int DeleteSingleOrDefaultFacilityRecord(int selectedfacility)
        {
            List<UserLoginInformation> userinfo =
                _context.UserLoginInformations.Where(x => x.FacilityId == selectedfacility && x.Status==1).ToList();
            List<UserFacility> userfacinfo =
                _context.UserFacilities.Where(x => x.FacilityId == selectedfacility).ToList();

            if (userinfo.Count == 0 && userfacinfo.Count == 0)
            {
                FacilityMaster facility =
                    _context.FacilityMasters.SingleOrDefault(x => x.FacilityMasterId.Equals(selectedfacility));
                    facility.Status = 2;
                    _context.Entry(facility).State = EntityState.Modified;
                //_context.FacilityMasters.Remove(facility);
                _context.SaveChanges();
                return 1;
            }
            return 2;
        }


        public ActionResult CheckIfValidEmail(string email2)
        {
            string[] strings = email2.Split(new[] { "||||" }, StringSplitOptions.None);
            string email = strings[0];
            int facilityMasterId = int.Parse(strings[1]);
            var res = facilityMasterId == 0 ? _context.FacilityMasters.SingleOrDefault(x => x.Email == email && x.Status==1) : _context.FacilityMasters.SingleOrDefault(x => x.Email == email && x.FacilityMasterId != facilityMasterId && x.Status==1);
            return res != null ? Json("Email Id Already Exists.Use Another Email Id", JsonRequestBehavior.AllowGet) : Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckIfValidContactNumber(string phonenumber)
        {
            string[] strings = phonenumber.Split(new[] { "||||" }, StringSplitOptions.None);
            string phonenum = strings[0];
            int facilityMasterId = int.Parse(strings[1]);
            var res = facilityMasterId == 0 ? _context.FacilityMasters.SingleOrDefault(x => x.Phone == phonenum &&x.Status==1) : _context.FacilityMasters.SingleOrDefault(x => x.Phone == phonenum && x.FacilityMasterId != facilityMasterId &&x.Status==1);
            return res != null ? Json("Contact Number Already Exists.Use Another Contact Number", JsonRequestBehavior.AllowGet) : Json(true, JsonRequestBehavior.AllowGet);
        }

       
        public String TreeGridData()
        {
            int indx = 0;
            StringBuilder sb = new StringBuilder();
            foreach (Organisation @org in _context.Organisations.Where(x=>x.Status==1))
            {
                indx++;
                int parentid = indx;
                sb.AppendLine("<tr class=\"treegrid-" + parentid + "\">");
                sb.AppendLine("<td>" + @org.Name + "</td>");
                sb.AppendLine("<td>" + @org.Phone + "</td>");
                sb.AppendLine("<td>" + @org.PostalCode.ToString().PadLeft(5, '0') + "</td>");
                sb.AppendLine("<td>" + @org.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss") + "</td>");
                sb.AppendLine("<td>&nbsp;</td>");
                sb.AppendLine("</tr>");
                foreach (var @fac in @org.GetAssocaitedFacilities())
                {
                    indx++;
                    sb.AppendLine("<tr class=\"treegrid-" + indx + " treegrid-parent-" + parentid + "\">");
                    sb.AppendLine("<td>" + @fac.FacilityMasterName + "</td>");
                    sb.AppendLine("<td>" + @fac.Phone + "</td>");
                    sb.AppendLine("<td>" + @fac.PostalCode.ToString().PadLeft(5, '0') + "</td>");
                    sb.AppendLine("<td>" + @fac.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss") + "</td>");
                    var view = Url.Content("~/styles/imgs/view.png");
                    var edit = Url.Content("~/styles/imgs/edit.png");
                    var delete = Url.Content("~/styles/imgs/delete.png");
                    sb.AppendLine("<td>" + "<p align=\"center\">" +
                                "<a href=\"#\">" +
                                "<img title=\"View\" src=\"" + view + "\" width=\"25px\" height=\"25px\" border=\"0px\" style=\"margin-right:5px\" id=\"btnView_" + @fac.FacilityMasterId + "\" onclick=\"RecoredEdit(" + @fac.FacilityMasterId + ",1)\" > " +
                                "<img title=\"Edit\" src=\"" + edit + "\" width=\"20px\" height=\"20px\" border=\"0px\" style=\"margin-right:5px\" id=\"btnEdit_" + @fac.FacilityMasterId + "\" onclick=\"RecoredEdit(" + @fac.FacilityMasterId + ",2)\" > " +
                                "<img title=\"Delete\" src=\"" + delete + "\" width=\"20px\" height=\"20px\" border=\"0px\" style=\"margin-right:5px\" id=\"btnDelete_" + @fac.FacilityMasterId + "\" onclick=\"RecordDelete(" + @fac.FacilityMasterId + ")\" > " +
                                "</a>" +
                                "</p>" + "</td>");
                    sb.AppendLine("</tr>");
                }
            }
            return sb.ToString();
        }


        public ActionResult GenerateReport(int selectedOrganisation, int selectedtype, int selectedstatus)
        {
            List<FacilityMaster> facilitymasters;

            if(selectedOrganisation==0)
            {
                if (selectedstatus == 0)
                {

                facilitymasters = FacilityMaster.GetAllFacilities();

                }
                else if(selectedstatus==1)
                {
                    facilitymasters = FacilityMaster.GetAllActiveFacilities();
                }
                else 
                {
                    facilitymasters = FacilityMaster.GetAllInActiveFacilities();
                }
            }
            else
            {
                Organisation org = _context.Organisations.Find(selectedOrganisation);

                if(selectedstatus==0)
                {
                    facilitymasters = org.GetAssocaitedFacilities();
                }
                else if(selectedstatus==1)
                {
                    facilitymasters = org.GetAssocaitedActiveFacilities();
                }
                else
                {
                    facilitymasters = org.GetAssocaitedInActiveFacilities();

                }
            }
            foreach (FacilityMaster facmaster in facilitymasters)
            {
                facmaster.AssociatedUsers = facmaster.GetAssocaitedFacilityAdmins();
            }
           
            
            return performActionForReportType(selectedtype, facilitymasters);
        }

        private bool performActionForSelectedOrganisation(int selectedOrg)
        {
            return !(selectedOrg == 0);
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

        private ActionResult performActionForReportType(int selectedtype, List<FacilityMaster> facilities)
        {
            if (selectedtype == 0)
            {
                return ExportToExcel(facilities);
            }
            else
            {
                return ExportToPDF(facilities);
            }
        }

        private void AddCell(IRow header, int Colidx, string CellValue, ICellStyle cellstyle = null)
        {
            ICell cell = header.CreateCell(Colidx);
            if (cellstyle != null) cell.CellStyle = cellstyle;
            cell.SetCellValue(CellValue);
        }

        public ActionResult ExportToExcel(List<FacilityMaster> matchingfacilityadmins)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet facilityadmins = workbook.CreateSheet("FacAdmins");

            IFont boldFont = workbook.CreateFont();
            boldFont.Boldweight = (short)FontBoldWeight.Bold;
            boldFont.FontName = "Arial, Helvetica, sans-serif";
            boldFont.FontHeight = 10;

            ICellStyle headerstyle = workbook.CreateCellStyle();
            headerstyle.SetFont(boldFont);
            headerstyle.BorderBottom = BorderStyle.Medium;
            headerstyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            headerstyle.FillPattern = FillPattern.SolidForeground;

            IRow headerRow = facilityadmins.CreateRow(0);

            int columnIndx = 0;
            AddCell(headerRow, columnIndx++, "Facility Name", headerstyle);
            AddCell(headerRow, columnIndx++, "Organization", headerstyle);
            AddCell(headerRow, columnIndx++, "Address", headerstyle);
            AddCell(headerRow, columnIndx++, "Zip Code", headerstyle);
            AddCell(headerRow, columnIndx++, "City", headerstyle);
            AddCell(headerRow, columnIndx++, "State", headerstyle);
            AddCell(headerRow, columnIndx++, "Country", headerstyle);
            AddCell(headerRow, columnIndx++, "Contact No", headerstyle);
            AddCell(headerRow, columnIndx++, "Contact Person", headerstyle);
            AddCell(headerRow, columnIndx++, "Fax", headerstyle);
            AddCell(headerRow, columnIndx++, "Email", headerstyle);
            AddCell(headerRow, columnIndx++, "Website", headerstyle);
            AddCell(headerRow, columnIndx, "Facility Admins", headerstyle);




            int currentRow = 1;
            foreach (FacilityMaster @facmaster in matchingfacilityadmins)
            {
                columnIndx = 0;
                IRow dataRow = facilityadmins.CreateRow(currentRow++);

                AddCell(dataRow, columnIndx++, @facmaster.FacilityMasterName);
                AddCell(dataRow, columnIndx++, @facmaster.OrganisationName);
                AddCell(dataRow, columnIndx++, @facmaster.AddressLine1);
                string zipcode = "";
                if (@facmaster.ZipId.HasValue)
                {
                    zipcode = @facmaster.ZipId.Value.ToString().PadLeft(5, '0');
                }
                AddCell(dataRow, columnIndx++, zipcode);
                AddCell(dataRow, columnIndx++, @facmaster.City);
                AddCell(dataRow, columnIndx++, @facmaster.State);
                AddCell(dataRow, columnIndx++, @facmaster.Landmark);  //filll with country


                AddCell(dataRow, columnIndx++, @facmaster.Phone);
                AddCell(dataRow, columnIndx++, @facmaster.ContactPerson);
                AddCell(dataRow, columnIndx++, @facmaster.Fax);
                AddCell(dataRow, columnIndx++, @facmaster.Email);

                AddCell(dataRow, columnIndx++, @facmaster.Website);
                AddCell(dataRow, columnIndx, @facmaster.GetAssocaitedFacilityAdminNamesAsString());//fill facility admins

             
            }
            string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".xlsx");

          //  string FilePath = Path.Combine(Server.MapPath("../Uploads"), "FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".xlsx");
            using (
                var fs = new FileStream(FilePath, FileMode.Create,
                    FileAccess.Write))
            {
                workbook.Write(fs);
            }
            FilePath = "FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".xlsx";

           // FilePath = LoginController.DomainName + "/Uploads" + "/FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".xlsx";
            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToPDF(List<FacilityMaster> matchingfacilities)
        {
            string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".pdf");

           // string FilePath = Path.Combine(Server.MapPath("../Uploads"), "FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".pdf");
            using (var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
            {
                Document document = new Document(PageSize.A3.Rotate(), 25, 25, 30, 30);
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
                sb.Append("Facility Name");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Organization");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Address");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("ZipCode");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("City");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("State");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Country");
                sb.Append("</td>");


                sb.Append("<td>");
                sb.Append("Contact No");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Contact Person");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Fax");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Email");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Website");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Facility Admins");
                sb.Append("</td>");



                sb.Append("</tr>");
                //sb.Append("<tr><td colspan=22><hr></td></tr>");
                sb.Append("</thead>");

                sb.Append("<tbody>");
                foreach (FacilityMaster @fac in matchingfacilities)
                {
                    sb.Append("<tr>");

                    sb.Append("<td>");
                    sb.Append(@fac.FacilityMasterName);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.OrganisationName);
                    sb.Append("</td>");


                    sb.Append("<td>");
                    sb.Append(@fac.AddressLine1);
                    sb.Append("</td>");


                    string zipcode = "";
                    if (@fac.ZipId.HasValue)
                    {
                        zipcode = @fac.ZipId.Value.ToString().PadLeft(5, '0');
                    }

                    sb.Append("<td>");
                    sb.Append(zipcode);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.City);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.State);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.Landmark); // fill with country
                    sb.Append("</td>");



                    sb.Append("<td>");
                    sb.Append(@fac.Phone);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.ContactPerson);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.Fax);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.Email);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.Website);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@fac.GetAssocaitedFacilityAdminNamesAsString());
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
            FilePath = "FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".pdf";

          //  FilePath = LoginController.DomainName + "/Uploads" + "/FacAdmins_" + LoginController.ActiveUser_SESSIONID + ".pdf";
            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }


        
    }
}
