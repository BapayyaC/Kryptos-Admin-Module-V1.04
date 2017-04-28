using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Kryptos.Models;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;


namespace Kryptos.Controllers
{
    public class UsersExportController : Controller
    {
        readonly string _uploadDirectory;
        public UsersExportController()
        {
            _uploadDirectory = ConfigurationManager.AppSettings["UploadDirectory"];
        }
        //
        // GET: /UsersExpport/
        kryptoEntities1 _context = new kryptoEntities1();

        public ActionResult List()
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

        public ActionResult GetAllOrganizations()
        {
            return Json(_context.Organisations.Where(x=>x.Status==1).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMatchingFacilityMasters(int selectedOrg)
        {
            return Json(_context.FacilityMasters.Where(x => x.OrganisationId == selectedOrg && x.Status==1),
                JsonRequestBehavior.AllowGet);
        }

        public ActionResult Index()
        {
            return View();
        }

        private void AddCell(IRow header, int colidx, string cellValue, ICellStyle cellstyle = null)
        {
            ICell cell = header.CreateCell(colidx);
            if (cellstyle != null) cell.CellStyle = cellstyle;
            cell.SetCellValue(cellValue);
        }

        public ActionResult ExportToExcel(List<UserLoginInformation> matchingUsers)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet users = workbook.CreateSheet("Users");

            IFont boldFont = workbook.CreateFont();
            boldFont.Boldweight = (short)FontBoldWeight.Bold;
            boldFont.FontName = "Arial, Helvetica, sans-serif";
            boldFont.FontHeight = 10;

            ICellStyle headerstyle = workbook.CreateCellStyle();
            headerstyle.SetFont(boldFont);
            headerstyle.BorderBottom = BorderStyle.Medium;
            headerstyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            headerstyle.FillPattern = FillPattern.SolidForeground;

            IRow headerRow = users.CreateRow(0);

            int columnIndx = 0;
            AddCell(headerRow, columnIndx++, "Title", headerstyle);
            AddCell(headerRow, columnIndx++, "User Name", headerstyle);
            AddCell(headerRow, columnIndx++, "Email Id", headerstyle);
            AddCell(headerRow, columnIndx++, "Contact No", headerstyle);
            AddCell(headerRow, columnIndx++, "Organization", headerstyle);
            AddCell(headerRow, columnIndx++, "Primary Facility", headerstyle);
            AddCell(headerRow, columnIndx++, "User Role", headerstyle);
            AddCell(headerRow, columnIndx++, "Status", headerstyle);
            AddCell(headerRow, columnIndx++, "Date of Registration", headerstyle);
            AddCell(headerRow, columnIndx++, "Last Activation Date", headerstyle);
            AddCell(headerRow, columnIndx++, "Last Deactivated Date", headerstyle);
            AddCell(headerRow, columnIndx++, "ZipCode", headerstyle);
            AddCell(headerRow, columnIndx++, "City", headerstyle);
            AddCell(headerRow, columnIndx++, "State", headerstyle);
            AddCell(headerRow, columnIndx++, "Country", headerstyle);
            AddCell(headerRow, columnIndx++, "Is Super Admin", headerstyle);
            AddCell(headerRow, columnIndx++, "Is Organizational Admin", headerstyle);
            AddCell(headerRow, columnIndx++, "Is Facility Admin", headerstyle);
            AddCell(headerRow, columnIndx++, "Is Normal User", headerstyle);
            AddCell(headerRow, columnIndx, "Other Facilities", headerstyle);



            int currentRow = 1;
            foreach (UserLoginInformation @userLogin in matchingUsers)
            {
                columnIndx = 0;
                IRow dataRow = users.CreateRow(currentRow++);

                AddCell(dataRow, columnIndx++, userLogin.TitleName);
                AddCell(dataRow, columnIndx++, userLogin.FirstName + ", " + userLogin.LastName);
                AddCell(dataRow, columnIndx++, userLogin.EmailId);
                AddCell(dataRow, columnIndx++, userLogin.ContactNumber);
                AddCell(dataRow, columnIndx++, userLogin.Facility.OrganisationName);
                AddCell(dataRow, columnIndx++, userLogin.Facility.FacilityMasterName);

                string loginrole;
                if (userLogin.IsSuperAdmin)
                {
                    loginrole = "Super Admin";
                }
                else if (userLogin.IsOrganisationAdmin)
                {
                    loginrole = "Organisation Admin";
                }
                else if (userLogin.IsFacilityAdmin)
                {
                    loginrole = "Facility Admin";
                }
                else
                {
                    loginrole = "Normal User";
                }
                AddCell(dataRow, columnIndx++, loginrole);
                AddCell(dataRow, columnIndx++, @userLogin.IsActive.ToString());
                AddCell(dataRow, columnIndx++, @userLogin.CreatedDate.ToString());
                AddCell(dataRow, columnIndx++, @userLogin.ActivatedDate.ToString());
                AddCell(dataRow, columnIndx++, @userLogin.DeactivatedDate.ToString());
                string zipcode = "";
                if (@userLogin.ZipId.HasValue)
                {
                    zipcode = @userLogin.ZipId.Value.ToString().PadLeft(5, '0');
                }
                AddCell(dataRow, columnIndx++, zipcode);
                AddCell(dataRow, columnIndx++, @userLogin.City);
                AddCell(dataRow, columnIndx++, @userLogin.State);
                AddCell(dataRow, columnIndx++, @userLogin.Country);
                AddCell(dataRow, columnIndx++, @userLogin.IsSuperAdmin.ToString());
                AddCell(dataRow, columnIndx++, @userLogin.IsOrganisationAdmin.ToString());
                AddCell(dataRow, columnIndx++, @userLogin.IsFacilityAdmin.ToString());
                AddCell(dataRow, columnIndx++, @userLogin.IsNormalUser.ToString());
                AddCell(dataRow, columnIndx, @userLogin.GetUserFacilityNamesAsString());
            }

            string filePath = Path.Combine(Server.MapPath(_uploadDirectory), "users_" + LoginController.ActiveUser_SESSIONID + ".xlsx");
            using (
                var fs = new FileStream(filePath, FileMode.Create,
                    FileAccess.Write))
            {
                workbook.Write(fs);
            }
            filePath = "users_" + LoginController.ActiveUser_SESSIONID + ".xlsx";
            return Json(filePath, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToPDF(List<UserLoginInformation> matchingUsers)
        {
            string filePath = Path.Combine(Server.MapPath(_uploadDirectory), "users_" + LoginController.ActiveUser_SESSIONID + ".pdf");
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                Document document = new Document(PageSize.A1.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                StringBuilder sb = new StringBuilder();
                sb.Append("<table style=\"color:black;font-family: Arial, Helvetica, sans-serif;font-size: 8px;width:100%;max-width:100%;margin-bottom:20px;border:1px solid #ddd;\">");
                sb.Append("<thead>");
                sb.Append("<tr style=\"color: black;font-family:Arial, Helvetica, sans-serif; font-size:10px;font-weight:bold;border-bottom: 1px solid black; \">");

                sb.Append("<td>");
                sb.Append("Title");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("User Name");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Email Id");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Contact No");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Organization");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Primary Facility");
                sb.Append("</td>");


                sb.Append("<td>");
                sb.Append("User Role");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Status");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Date of Registration");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Last Activation Date");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Last Deactivated Date");
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
                sb.Append("IsSuperAdmin");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("IsOrganizationalAdmin");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("IsFacilityAdmin");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("IsNormalUser");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append("Other Facilities");
                sb.Append("</td>");

                sb.Append("</tr>");
                sb.Append("</thead>");

                sb.Append("<tbody>");
                foreach (UserLoginInformation @userLogin in matchingUsers)
                {
                    sb.Append("<tr>");

                    sb.Append("<td>");
                    sb.Append(userLogin.TitleName);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(userLogin.FirstName + ", " + userLogin.LastName);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(userLogin.EmailId);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(userLogin.ContactNumber);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(userLogin.Facility.OrganisationName);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(userLogin.Facility.FacilityMasterName);
                    sb.Append("</td>");

                    string loginrole;
                    if (userLogin.IsSuperAdmin)
                    {
                        loginrole = "Super Admin";
                    }
                    else if (userLogin.IsOrganisationAdmin)
                    {
                        loginrole = "Organisation Admin";
                    }
                    else if (userLogin.IsFacilityAdmin)
                    {
                        loginrole = "Facility Admin";
                    }
                    else
                    {
                        loginrole = "Normal User";
                    }

                    sb.Append("<td>");
                    sb.Append(loginrole);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.IsActive);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.CreatedDate);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.ActivatedDate);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.DeactivatedDate);
                    sb.Append("</td>");

                    string zipcode = "";
                    if (@userLogin.ZipId.HasValue)
                    {
                        zipcode = @userLogin.ZipId.Value.ToString().PadLeft(5, '0');
                    }

                    sb.Append("<td>");
                    sb.Append(zipcode);
                    sb.Append("</td>");


                    sb.Append("<td>");
                    sb.Append(@userLogin.City);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.State);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.Country);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.IsSuperAdmin);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.IsOrganisationAdmin);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.IsFacilityAdmin);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.IsNormalUser);
                    sb.Append("</td>");

                    sb.Append("<td>");
                    sb.Append(@userLogin.GetUserFacilityNamesAsString());
                    sb.Append("</td>");


                    sb.Append("</tr>");
                }
                sb.Append("</tbody>");
                sb.Append("</table>");

                TextReader reader = new StringReader(sb.ToString());
                using (var htmlWorker = new HTMLWorker(document))
                {
                    htmlWorker.Parse(reader);
                }
                document.Close();
                writer.Close();
                fs.Close();
            }
            filePath = "users_" + LoginController.ActiveUser_SESSIONID + ".pdf";
            return Json(filePath, JsonRequestBehavior.AllowGet);
        }


        public ActionResult GenerateReport(int selectedOrg, int selectedFacility, int selectedstatus, int selectedtype)
        {
            bool includeFacInCondition = false;

            var includeOrgInCondition = PerformActionForSelectedOrganisation(selectedOrg);
            if (includeOrgInCondition)
            {
                includeFacInCondition = performActionForSelectedFacility(selectedFacility);
            }
            var includestatusAsCondition = performActionForSelectedstatus(selectedstatus);

            List<UserLoginInformation> users = new List<UserLoginInformation>();

            if (!includeOrgInCondition)
            {
                if (!includestatusAsCondition)
                {
                    users = _context.UserLoginInformations.Where(x=>x.Status==1).ToList();
                }
                else
                {
                    if (selectedstatus == 1)
                    {
                        foreach (Organisation @orgObj in _context.Organisations.Where(x=>x.Status==1).ToList())
                        {
                            foreach (FacilityMaster @facility in orgObj.GetAssocaitedFacilities())
                            {
                                users.AddRange(@facility.GetAssocaitedActiveUsers());
                            }
                        }
                    }
                    else if (selectedstatus == 2)
                    {
                        foreach (Organisation @orgObj in _context.Organisations.Where(x=>x.Status==1).ToList())
                        {
                            foreach (FacilityMaster @facility in orgObj.GetAssocaitedFacilities())
                            {
                                users.AddRange(@facility.GetAssocaitedInActiveUsers());
                            }
                        }
                    }
                    else
                    {
                        foreach (Organisation @orgObj in _context.Organisations.Where(x=>x.Status==1).ToList())
                        {
                            foreach (FacilityMaster @facility in orgObj.GetAssocaitedFacilities())
                            {
                                users.AddRange(@facility.GetAssocaitedUsers());
                            }
                        }
                    }
                }
            }
            else
            {
                if (includeFacInCondition)
                {
                    FacilityMaster facility = _context.FacilityMasters.Find(selectedFacility);
                    if (selectedstatus == 1)
                    {
                        users.AddRange(@facility.GetAssocaitedActiveUsers());
                    }
                    else if (selectedstatus == 2)
                    {
                        users.AddRange(@facility.GetAssocaitedInActiveUsers());
                    }
                    else
                    {
                        users.AddRange(@facility.GetAssocaitedUsers());
                    }
                }
                else
                {
                    Organisation orgObj = _context.Organisations.Find(selectedOrg);
                    if (selectedstatus == 1)
                    {
                        foreach (FacilityMaster @facility in orgObj.GetAssocaitedFacilities())
                        {
                            users.AddRange(@facility.GetAssocaitedActiveUsers());
                        }
                    }
                    else if (selectedstatus == 2)
                    {
                        foreach (FacilityMaster @facility in orgObj.GetAssocaitedFacilities())
                        {
                            users.AddRange(@facility.GetAssocaitedInActiveUsers());
                        }
                    }
                    else
                    {
                        foreach (FacilityMaster @facility in orgObj.GetAssocaitedFacilities())
                        {
                            users.AddRange(@facility.GetAssocaitedUsers());
                        }
                    }
                }
            }
            return PerformActionForReportType(selectedtype, users);
        }

        private bool PerformActionForSelectedOrganisation(int selectedOrg)
        {
            return selectedOrg != 0;
        }

        private bool performActionForSelectedFacility(int selectedFacility)
        {
            return selectedFacility != 0;
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

        private ActionResult PerformActionForReportType(int selectedtype, List<UserLoginInformation> users)
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
    }
}
