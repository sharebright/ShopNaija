using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcApplication1.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewData["Message"] = "Shop Naija";

            return View();
        }

        public ActionResult ComingSoon()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
