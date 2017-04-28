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
using NPOI.HSSF.Util;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.html.simpleparser;
using System.Configuration;


namespace Kryptos.Controllers
{
    public class OrganisationController : Controller
    {


        string UploadDirectory;
        public OrganisationController()
        {
            UploadDirectory = ConfigurationManager.AppSettings["UploadDirectory"];
        }

        //
        // GET: /Organisation/
        kryptoEntities1 _context = new kryptoEntities1();
        public ActionResult List()
        {
            ViewData["current view"] = "Organization List";
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

        public ActionResult OrganizationExportList()
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

        public ActionResult GetMatchingZipCodeResult(string prefix)
        {
            List<ZipCode> zipList = (from m in _context.ZipCodes where m.ZipCode1.Contains(prefix) select m).Take(10).ToList();
            return Json(zipList.Select(zip => string.Format("{0}-{1}-{2}-{3}-{4}", zip.ZipId, zip.ZipCode1, zip.Country, zip.State, zip.City)).ToArray(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult OrganizationInfoList()
        {
            return Json(new { aaData = _context.Organisations.Where(x=>x.Status==1).ToList() }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllOrganizations()
        {
            return Json(_context.Organisations.Where(x=>x.Status==1).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllOrganizationsExceptSelected(int selectedorg)
        {
            List<Organisation> orglist = _context.Organisations.Where(x => x.OrganisationId != selectedorg && x.Status==1).ToList();
            return Json(orglist, JsonRequestBehavior.AllowGet);
        }


        public JsonResult getMatchingOrganization(int selectedOrg)
        {
            Organisation orginfo = _context.Organisations.SingleOrDefault(x => x.OrganisationId.Equals(selectedOrg) && x.Status==1);

            return Json(orginfo, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Result(int currentrecord)
        {
            Organisation currentOrganisation = _context.Organisations.Find(currentrecord);
            List<int> participantsIds = null;
            if (currentrecord != 0)
            {
                List<UserLoginInformation> usersInDb = new List<UserLoginInformation>();

                foreach (FacilityMaster @facility in currentOrganisation.GetAssocaitedFacilities())
                {
                    usersInDb.AddRange(@facility.GetAssocaitedOrganisationAdmins());
                }

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

        public string UpdateOrganization(Organisation orginfo)
        {
            try
            {
                UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;
                if (orginfo.OrganisationId == 0)
                {
                    orginfo.ModifiedById = loggedinUser.USERID.ToString();
                    orginfo.CreatedById = loggedinUser.USERID.ToString();
                    orginfo.CreatedDate = DateTime.Now;
                    orginfo.ModifiedDate = DateTime.Now;
                    orginfo.Status = 1;
                    _context.Organisations.Add(orginfo);
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
                                    orginfo = Updateobject(orginfo.OrganisationId, orginfo);
                                    orginfo.ModifiedById = loggedinUser.USERID.ToString();
                                    orginfo.ModifiedDate = DateTime.Now;
                                    db.Entry(orginfo).State = EntityState.Modified;
                                    db.SaveChanges();

                                    List<MyNode> responseNodes =
                                        JsonConvert.DeserializeObject<List<MyNode>>(orginfo.UserSelections);
                                    List<UserLoginInformation> usersInDb = new List<UserLoginInformation>();

                                    foreach (FacilityMaster @facility in orginfo.GetAssocaitedFacilities())
                                    {
                                        usersInDb.AddRange(@facility.GetAssocaitedOrganisationAdmins());
                                    }

                                    List<int> indb = usersInDb.Select(x => x.USERID).ToList();
                                    List<int> inselections = responseNodes.Select(x => x.value).ToList();

                                    var toInclude = UserDatatablesController.ExcludedRight(indb, inselections);
                                    var toExclude = UserDatatablesController.ExcludedLeft(indb, inselections);

                                    foreach (int @in in toInclude)
                                    {
                                        UserLoginInformation current = db.UserLoginInformations.Find(@in);
                                        current.IsOrganisationAdmin = true;
                                        current.IsFacilityAdmin = true;
                                        db.Entry(current).State = EntityState.Modified;
                                    }
                                    foreach (int @out in toExclude)
                                    {
                                        UserLoginInformation current = db.UserLoginInformations.Find(@out);
                                        current.IsOrganisationAdmin = false;
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

        public Organisation Updateobject(int id, Organisation filled)
        {
            Organisation obj = _context.Organisations.Find(id);
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


        public bool DeleteSingleOrganizationRecord(int selectedorg)
        {
            var orgIds =
                _context.Database.SqlQuery<string>(
                    "SELECT DISTINCT Organisation from [krypto].[dbo].GetAllDepndeeOrganisations();").ToList();
            if (orgIds.Any(each => selectedorg.ToString() == each))
            {
                return false;
            }
            Organisation currentOrg = _context.Organisations.Find(selectedorg);
            currentOrg.Status = 2;
            _context.Entry(currentOrg).State = EntityState.Modified;
            _context.SaveChanges();
            return true;
        }


        public ActionResult CheckIfValidOrganizationName(string org)
        {
            string[] strings = org.Split(new string[] { "||||" }, StringSplitOptions.None);
            Organisation res = null;
            string orgname = strings[0];
            int orgid = int.Parse(strings[1]);
            if (orgid == 0) res = _context.Organisations.SingleOrDefault(x => x.Name == orgname);
            else res = _context.Organisations.SingleOrDefault(x => x.Name == orgname && x.OrganisationId != orgid);
            if (res != null) return Json("OrganizationName Already Exists.Use Another Organization Name", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckIfValidEmail(string email2)
        {
            string[] strings = email2.Split(new string[] { "||||" }, StringSplitOptions.None);
            Organisation res = null;
            string email = strings[0];
            int OrganisationId = int.Parse(strings[1]);
            if (OrganisationId == 0) res = _context.Organisations.SingleOrDefault(x => x.Email == email);
            else res = _context.Organisations.SingleOrDefault(x => x.Email == email && x.OrganisationId != OrganisationId);
            if (res != null) return Json("Email Id Already Exists.Use Another Email Id", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckIfValidContactNumber(string phonenumber)
        {
            string[] strings = phonenumber.Split(new string[] { "||||" }, StringSplitOptions.None);
            Organisation res = null;
            string phonenum = strings[0];
            int organisationId = int.Parse(strings[1]);
            if (organisationId == 0) res = _context.Organisations.SingleOrDefault(x => x.Phone == phonenum);
            else res = _context.Organisations.SingleOrDefault(x => x.Phone == phonenum && x.OrganisationId != organisationId);
            if (res != null) return Json("Contact Number Already Exists.Use Another Contact Number", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }



        public ActionResult GenerateReport(int selectedtype, int selectedstatus)
        {
            List<Organisation> orglist;

            if (selectedstatus == 0)
            {
                orglist = new Organisation().GetAllOrganizations();
            }
            else if (selectedstatus == 1)
            {
                orglist = new Organisation().GetAllActiveOrganizations();
            }
            else
            {
                orglist = new Organisation().GetAllInActiveOrganizations();
            }
            foreach (Organisation org in orglist)
            {
                org.AssociatedUsers = org.GetAssocaitedOrganisationAdmins();
            }
            return performActionForReportType(selectedtype, orglist);
        }

        private void AddCell(IRow header, int Colidx, string CellValue, ICellStyle cellstyle = null)
        {
            ICell cell = header.CreateCell(Colidx);
            if (cellstyle != null) cell.CellStyle = cellstyle;
            cell.SetCellValue(CellValue);
        }

        public ActionResult ExportToExcel(List<Organisation> matchingOrganizations)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet orgadmins = workbook.CreateSheet("orgAdmins");

            IFont boldFont = workbook.CreateFont();
            boldFont.Boldweight = (short)FontBoldWeight.Bold;
            boldFont.FontName = "Arial, Helvetica, sans-serif";
            boldFont.FontHeight = 10;

            ICellStyle headerstyle = workbook.CreateCellStyle();
            headerstyle.SetFont(boldFont);
            headerstyle.BorderBottom = BorderStyle.Medium;
            headerstyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            headerstyle.FillPattern = FillPattern.SolidForeground;

            IRow headerRow = orgadmins.CreateRow(0);

            int columnIndx = 0;
            AddCell(headerRow, columnIndx++, "Organization Name", headerstyle);
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
            AddCell(headerRow, columnIndx, "Organization Admins", headerstyle);

            int currentRow = 1;
            foreach (Organisation @org in matchingOrganizations)
            {
                columnIndx = 0;
                IRow dataRow = orgadmins.CreateRow(currentRow++);

                AddCell(dataRow, columnIndx++, @org.Name);
                AddCell(dataRow, columnIndx++, @org.AddressLine1);
                string zipcode = "";
                if (@org.ZipId.HasValue)
                {
                    zipcode = @org.ZipId.Value.ToString().PadLeft(5, '0');
                }
                AddCell(dataRow, columnIndx++, zipcode);
                AddCell(dataRow, columnIndx++, @org.City);
                AddCell(dataRow, columnIndx++, @org.State);
                AddCell(dataRow, columnIndx++, @org.Country);


                AddCell(dataRow, columnIndx++, @org.Phone);
                AddCell(dataRow, columnIndx++, @org.ContactPerson);

                AddCell(dataRow, columnIndx++, @org.Fax);
                AddCell(dataRow, columnIndx++, @org.Email);

                AddCell(dataRow, columnIndx++, @org.Website);
                AddCell(dataRow, columnIndx, @org.GetAssocaitedOrganisationAdminNamesAsString());

            }
            string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "OrgAdmins_" + LoginController.ActiveUser_SESSIONID + ".xlsx");

            using (
                var fs = new FileStream(FilePath, FileMode.Create,
                    FileAccess.Write))
            {
                workbook.Write(fs);
            }
            FilePath = "OrgAdmins_" + LoginController.ActiveUser_SESSIONID + ".xlsx";

            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToPDF(List<Organisation> matchingOrganizations)
        {
            string FilePath = Path.Combine(Server.MapPath(UploadDirectory), "OrgAdmins_" + LoginController.ActiveUser_SESSIONID + ".pdf");

            using (var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
            {
                Document document = new Document(PageSize.A2.Rotate(), 25, 25, 30, 30);
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
                sb.Append("Organization Name");
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
                sb.Append("Status");
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
                sb.Append("Organization Admins");
                sb.Append("</td>");



                sb.Append("</tr>");
                //sb.Append("<tr><td colspan=22><hr></td></tr>");
                sb.Append("</thead>");

                sb.Append("<tbody>");
                foreach (Organisation @org in matchingOrganizations)
                {
                    sb.Append("<tr>");

                    sb.Append("<td>");
                    sb.Append(@org.Name);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.AddressLine1);
                    sb.Append("</td>");


                    string zipcode = "";
                    if (@org.ZipId.HasValue)
                    {
                        zipcode = @org.ZipId.Value.ToString().PadLeft(5, '0');
                    }

                    sb.Append("<td>");
                    sb.Append(zipcode);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.City);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.State);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.Country);
                    sb.Append("</td>");



                    sb.Append("<td>");
                    sb.Append(@org.Phone);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.ContactPerson);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.Fax);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.Email);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@org.Website);
                    sb.Append("</td>");


                    sb.Append("<td>");
                    sb.Append(@org.GetAssocaitedOrganisationAdminNamesAsString());
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
            FilePath = "OrgAdmins_" + LoginController.ActiveUser_SESSIONID + ".pdf";

            return Json(FilePath, JsonRequestBehavior.AllowGet);
        }

        private ActionResult performActionForReportType(int selectedtype, List<Organisation> organisations)
        {
            if (selectedtype == 0)
            {
                return ExportToExcel(organisations);
            }
            else
            {
                return ExportToPDF(organisations);
            }
        }

    }
}
