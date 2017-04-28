using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Kryptos.Models;

namespace Kryptos.Controllers
{
    public class DatatablesController : Controller
    {
        //
        // GET: /Datatables/


        private kryptoEntities1 _context = new kryptoEntities1();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult LoadUserInfo()
        {
            return Json(new {aaData = _context.UserLoginInformations.ToList()}, JsonRequestBehavior.AllowGet);
        }




    }
}
