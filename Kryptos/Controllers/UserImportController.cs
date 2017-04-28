using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using Kryptos.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Configuration;

namespace Kryptos.Controllers
{
    public class UserImportController : Controller
    {
        //
        // GET: /UserImport/

        readonly string _uploadDirectory;

        public UserImportController()
        {
            _uploadDirectory = ConfigurationManager.AppSettings["UploadDirectory"];
        }

        kryptoEntities1 _context = new kryptoEntities1();

        public List<UserLoginInformation> FinalObjects
        {
            get { return Session["FinalObjects"] as List<UserLoginInformation>; }
            private set { Session["FinalObjects"] = value; }
        }

        public int Facility
        {
            get { return (int)Session["Facility"]; }
            set { Session["Facility"] = value; }
        }

        public ActionResult Index()
        {
            ViewData["current view"] = "User Import";
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

        [HttpPost]
        public ActionResult UploadFiles()
        {
            if (Request.Files.Count <= 0) return Json("No files selected.");
            try
            {
                HttpPostedFileBase file = Request.Files[0];
                if (file != null)
                {
                    try
                    {
                        string fname;
                        if (Request.Browser.Browser.ToUpper() == "IE" ||
                            Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split('\\');
                            fname = testfiles[testfiles.Length - 1];
                        }
                        else
                        {
                            fname = file.FileName;
                        }

                        fname = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_") + DateTime.Now.Millisecond + '_' +
                                fname;

                        fname = Path.Combine(Server.MapPath(_uploadDirectory), fname);
                        file.SaveAs(fname);
                        return ImportFromExcel(fname, int.Parse(Request.Form["FacilityId"]));
                    }
                    catch (Exception)
                    {
                        return Json("File Upload failed");
                    }
                }
                else
                {
                    return Json("No files selected.");
                }
            }
            catch (Exception ex)
            {
                return Json("Error occurred. Error details: " + ex.Message);
            }
        }


        public List<Organisation> getMatchingOrganisationImportForuserrole(UserLoginInformation currentuser)
        {
            List<Organisation> orgs = null;
            if(currentuser.IsSuperAdmin)
            {
                orgs = _context.Organisations.Where(x=>x.Status==1).ToList();
            }
            else if(currentuser.IsOrganisationAdmin || currentuser.IsFacilityAdmin)
            {
                orgs = (from org in _context.Organisations
                        where org.OrganisationId == currentuser.Facility.OrganisationId && org.Status==1
                        select org).ToList();
            }
         
            return orgs;
        }

        public ActionResult GetAllOrganisations()
        {
            return Json(getMatchingOrganisationImportForuserrole(LoginController.ActiveUser), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMatchingFacilityMasters(int selectedOrg)
        {
            return Json(getmatchingFacilitiesImportForUserRole(LoginController.ActiveUser,selectedOrg),
                JsonRequestBehavior.AllowGet);
        }

        public List<FacilityMaster> getmatchingFacilitiesImportForUserRole(UserLoginInformation currentuser,int selectedorg)
        {
            List<FacilityMaster> facilities = null;

            if(currentuser.IsSuperAdmin)
            {
                facilities = _context.FacilityMasters.Where(x => x.OrganisationId == selectedorg && x.Status==1).ToList();

            }

            else if(currentuser.IsOrganisationAdmin)
            {
                facilities = _context.FacilityMasters.Where(x => x.OrganisationId == selectedorg && x.Status==1).ToList();
            }
           
            else if(currentuser.IsFacilityAdmin)
            {
                facilities = (from fac in _context.FacilityMasters
                              where fac.FacilityMasterId == currentuser.FacilityId && fac.Status==1
                              select fac).ToList();
            }
            return facilities;
        }


        private ActionResult ImportFromExcel(string fileName, int facility)
        {
            Facility = facility;
            StringBuilder stringBuilder = new StringBuilder();
            int rowIndex = 0;
            List<UserLoginInformation> initialObjects = new List<UserLoginInformation>();
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var workBook = new XSSFWorkbook(fs);
            var selectedSheet = workBook.GetSheetAt(0).SheetName;
            var sh = (XSSFSheet)workBook.GetSheet(selectedSheet);
            IRow row = sh.GetRow(rowIndex++);

            if (row.GetCell(0).StringCellValue != "Title" || row.GetCell(1).StringCellValue != "FirstName" ||
                row.GetCell(2).StringCellValue != "LastName" || row.GetCell(3).StringCellValue != "EmailId" ||
                row.GetCell(4).StringCellValue != "AboutUser" || row.GetCell(5).StringCellValue != "Address" ||
                row.GetCell(6).StringCellValue != "ZipCode" || row.GetCell(7).StringCellValue != "ContactNumber" ||
                row.GetCell(8).StringCellValue != "ActivateUser")
            {
                stringBuilder.AppendLine("Some problem with the Headings in the Excel Sheet...");
                return Json(stringBuilder.ToString(), JsonRequestBehavior.AllowGet);
            }
            row = sh.GetRow(rowIndex++);

            while (row != null)
            {
                int cellIndex = 0;
                UserLoginInformation login = new UserLoginInformation
                {
                    Title = (string)GetCellValue(row.GetCell(cellIndex++)),
                    FirstName = (string)GetCellValue(row.GetCell(cellIndex++)),
                    LastName = (string)GetCellValue(row.GetCell(cellIndex++)),
                    EmailId = (string)GetCellValue(row.GetCell(cellIndex++)),
                    Notes = (string)GetCellValue(row.GetCell(cellIndex++)),
                    Address = (string)GetCellValue(row.GetCell(cellIndex++))
                };
                int zip;
                if (int.TryParse(GetCellValue(row.GetCell(cellIndex++)).ToString(), out zip))
                {
                    login.ZipId = zip;
                }
                login.ContactNumber = GetCellValue(row.GetCell(cellIndex++), CellType.Numeric).ToString();
                
                Boolean useractive;
                if (bool.TryParse(GetCellValue(row.GetCell(cellIndex), CellType.Boolean).ToString(), out useractive))
                    login.IsActive = useractive;


                initialObjects.Add(login);
                row = sh.GetRow(rowIndex++);
            }

            FinalObjects = new List<UserLoginInformation>();
            rowIndex = 0;
            foreach (UserLoginInformation @each in initialObjects)
            {
                StringBuilder local = new StringBuilder();

                var title = @each.Title;

                //Title 
                var isfinal = IsRequiredString(@each.Title, local, "Title Field Is Required");
                isfinal =
                          IsValidEntry(ref title, local, "Name", "Title",
                              "Given Title value doesn't exist in the Database..") && isfinal;
                @each.Title = title;

                //FirstName 
                isfinal = IsRequiredString(@each.FirstName, local, "First Name Field Is Required") && isfinal;
                isfinal =
                          hasMinimumCharacters(@each.FirstName, local, 2, "At Least 2 Characters Required") && isfinal;
                isfinal =
                          IsPatternMatching(@each.FirstName, local, @"^[a-zA-Z][a-zA-Z0-9]*$",
                              "First Name value given is not valid..") && isfinal;

                //LastName 
                isfinal = IsRequiredString(@each.LastName, local, "Last Name Field Is Required") && isfinal;
                isfinal =
                          IsPatternMatching(@each.LastName, local, @"^[a-zA-Z][a-zA-Z0-9]*$",
                              "Last Name value given is not valid..") && isfinal;

                //EmailId 
                isfinal = IsRequiredString(@each.EmailId, local, "Email Id Field Is Required") && isfinal;
                isfinal =
                          IsPatternMatching(@each.EmailId, local,
                              @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}$",
                              "Email value given is not valid..") && isfinal;
                isfinal =
                          IsUnique(@each.EmailId, local, "EmailId", "UserLoginInformations",
                              "Email Id already exists in the database..") && isfinal;

                //ZipId
                if (@each.ZipId.HasValue)
                {
                    var validzip = @each.FillOtherDeatilsFortheMatchingZip();
                    if (!validzip)
                    {
                        local.Append("Not a valid Zip Code...");
                    }
                    else
                    {
                        @each.SecurityQuestion2 = @each.ZipId.ToString().PadLeft(5, '0');
                    }
                    isfinal = validzip && isfinal;
                }
                else
                {
                    isfinal = IsRequiredString("", local, "Zip Code Field Is Required") && isfinal;
                }

                //AboutUser 
                //isfinal = IsRequiredString(@each.Notes, local, "About User Field Is Required") && isfinal;

                //ContactNumber 
                isfinal =
                          IsRequiredString(@each.ContactNumber, local, "Contact Number Field Is Required") && isfinal;
                isfinal =
                          IsPatternMatching(@each.ContactNumber, local, "^[0-9]{8,14}$",
                              "Contact Number given is not valid..") && isfinal;
                isfinal =
                          IsUnique(@each.ContactNumber, local, "ContactNumber", "UserLoginInformations",
                              "Contact Number already exists in the database..") && isfinal;

                if (isfinal)
                {
                    FinalObjects.Add(@each);
                }
                else
                {
                    stringBuilder.AppendLine("There are Few Issues @ row: " + (rowIndex + 2) + "--->" + local);
                }
                rowIndex++;
            }
            return stringBuilder.Length > 0 ? Json(stringBuilder.ToString(), JsonRequestBehavior.AllowGet) : Json(FinalObjects, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveRecords()
        {
            try
            {
                UserLoginInformation loggedinUser = LoginController.ActiveUser;
                try
                {
                    using (TransactionScope transactionScope = new TransactionScope())
                    {
                        try
                        {
                            using (kryptoEntities1 db = new kryptoEntities1())
                            {
                                foreach (var @eachObj in FinalObjects)
                                {
                                    @eachObj.FacilityId = Facility;
                                    @eachObj.Organisations = @eachObj.Facility.OrganisationId.ToString();
                                    @eachObj.CreatedById = loggedinUser.USERID.ToString();
                                    @eachObj.ModifiedById = loggedinUser.USERID.ToString();
                                    @eachObj.CreatedDate = DateTime.Now;
                                    @eachObj.ModifiedDate = DateTime.Now;
                                    db.UserLoginInformations.Add(@eachObj);
                                    db.SaveChanges();
                                    if (@eachObj.IsActive)
                                    {
                                        UserActivate useracive = new UserActivate();
                                        useracive.CreatedById = loggedinUser.USERID.ToString();
                                        useracive.Date = DateTime.Now;
                                        useracive.USERID = @eachObj.USERID;
                                        useracive.IsActive = @eachObj.IsActive;
                                        db.UserActivates.Add(useracive);
                                        db.SaveChanges();
                                    }
                                }
                            }
                            transactionScope.Complete();
                        }
                        catch (Exception wException)
                        {
                            return Json("Record saving failed...", JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                catch (Exception wException)
                {
                    return Json("Record saving failed...", JsonRequestBehavior.AllowGet);
                }
                return Json("Records Saved Sucessfully", JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json("Record saving failed...", JsonRequestBehavior.AllowGet);
            }
        }

        private bool IsRequiredString(string stringvalue, StringBuilder sb,
            string requiredErrormsg = "Field Is Required")
        {
            try
            {
                bool result = true;
                if (string.IsNullOrEmpty(stringvalue))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(requiredErrormsg);
                    result = false;
                }
                return result;
            }
            catch (Exception EX_NAME)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("Some thing wrong with the data in the column ");
                return false;
            }
        }

        private bool hasMinimumCharacters(string stringvalue, StringBuilder sb, int minLength,
            string requiredErrormsg = "Minimum Characters Required")
        {
            try
            {
                bool result = true;
                if (stringvalue != null && stringvalue.Length < minLength)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(requiredErrormsg);
                    result = false;
                }
                return result;
            }
            catch (Exception EX_NAME)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("Some thing wrong with the data in the column ");
                return false;
            }
        }

        private bool IsPatternMatching(string stringvalue, StringBuilder sb,
            string expression = @"^[a-zA-Z][a-zA-Z0-9]*$", string expressionErrorMsg = "Not a valid String")
        {
            try
            {
                var temp = Regex.IsMatch(stringvalue, expression);
                if (!temp)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(expressionErrorMsg);
                }
                return temp;
            }
            catch (Exception EX_NAME)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("Some thing wrong with the data in the column ");
                return false;
            }
        }

        private bool IsUnique(string stringvalue, StringBuilder sb, string propertyName, string tableName,
            string expressionErrorMsg)
        {
            try
            {
                var valid = true;
                var propValue =
                    _context.Database.SqlQuery<string>("SELECT DISTINCT " + propertyName + " FROM dbo." + tableName +
                                                       " WHERE " + propertyName + "= '" + stringvalue + "'")
                        .FirstOrDefault();
                if (!string.IsNullOrEmpty(propValue))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(expressionErrorMsg);
                    valid = false;
                }
                return valid;
            }
            catch (Exception EX_NAME)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("Some thing wrong with the data in the column " + propertyName);
                return false;
            }
        }

        private bool IsValidEntry(ref string stringvalue, StringBuilder sb, string propertyName, string tableName,
            string expressionErrorMsg)
        {
            try
            {
                var valid = true;
                var propValue =
                    _context.Database.SqlQuery<MappingData>("SELECT Id, " + propertyName +
                                                            " from dbo." + tableName + " as Name where " + propertyName +
                                                            " = '" + stringvalue + "'").FirstOrDefault();
                if (propValue == null)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(expressionErrorMsg);
                    valid = false;
                }
                else
                    stringvalue = propValue.Id.ToString();
                return valid;
            }
            catch (Exception EX_NAME)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("Some thing wrong with the data in the column " + propertyName);
                return false;
            }
        }

        private object GetCellValue(ICell currentCell, CellType currentCellType = CellType.String)
        {

            try
            {
                if (currentCell == null ||
                    (currentCell.CellType == CellType.Blank || currentCell.CellType != currentCellType)) return "";
                switch (currentCellType)
                {
                    case CellType.Boolean:
                        return currentCell.BooleanCellValue;
                    case CellType.Numeric:
                        return currentCell.NumericCellValue;
                    case CellType.String:
                        return currentCell.StringCellValue;
                }
                return "";
            }
            catch (Exception EX_NAME)
            {
                return "";
            }
        }

        internal class MappingData
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
