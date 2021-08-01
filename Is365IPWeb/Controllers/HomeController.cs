using Is365IPWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Is365IPWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;


        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _env = webHostEnvironment;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        public IActionResult Index(IndexViewModel model)
        {
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIPInList(IndexViewModel model)
        {
            System.Net.IPAddress inputIP;
            bool conversionSuccessful = System.Net.IPAddress.TryParse(model.IP, out inputIP);
            if (ModelState.IsValid && conversionSuccessful)
            {
                try
                {
                    EndpointHelper endpointHelper = new EndpointHelper(_env);
                    var endpointList = await endpointHelper.GetEndpointList();
                    foreach (var endPoint in endpointList.Where(i => i.IPs != null))
                    {
                        foreach (var ipRange in endPoint.IPs)
                        {
                            if (IpHelper.IsInSubnet(inputIP, ipRange))
                            {
                                model.IsO365IP = true;
                            }
                        }
                    }
                }
                catch
                {
                    throw;
                }
                return RedirectToAction("Index", "Home", model);
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
