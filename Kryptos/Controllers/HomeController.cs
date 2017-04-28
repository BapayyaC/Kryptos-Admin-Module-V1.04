using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Kryptos.Models;

namespace Kryptos.Controllers
{
  
    public class HomeController : Controller
    {
        kryptoEntities1 ke = new kryptoEntities1();
        public ActionResult Index()
        {
            List<UserLoginInformation> users = ke.UserLoginInformations.ToList();
            return View(users);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
