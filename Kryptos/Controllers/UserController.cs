using Kryptos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kryptos.Controllers
{
    public class UserController : Controller
    {
        //
        // GET: /Sample/

        kryptoEntities1 ke = new kryptoEntities1();

        public ActionResult List()
        {
            return View(ke.UserLoginInformations.ToList());
        }

    }
}
