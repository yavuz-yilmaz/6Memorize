using System.Diagnostics;
using _6Memorize.Models;
using Microsoft.AspNetCore.Mvc;

namespace _6Memorize.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
    }
}
