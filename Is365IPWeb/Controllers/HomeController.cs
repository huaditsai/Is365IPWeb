using Is365IPWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        private IHttpContextAccessor _accessor;

        private readonly EndpointHelper _endpointHelper;
        private readonly List<EndPointList> _endpointList;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor accessor)
        {
            _logger = logger;
            _env = webHostEnvironment;

            _accessor = accessor;

            _endpointHelper = new EndpointHelper(_env);
            _endpointList = _endpointHelper.GetEndpointList().Result;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        public IActionResult Index(IndexViewModel model)
        {
            //var ClientIPAddr = HttpContext.Connection.RemoteIpAddress?.ToString();
            string ipAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            ViewBag.IPAddress = ipAddress;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIPInList(IndexViewModel model)
        {
            bool conversionSuccessful = System.Net.IPAddress.TryParse(model.IP, out System.Net.IPAddress inputIP);
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
                                model.ServiceName = endPoint.serviceAreaDisplayName;
                                return RedirectToAction("Index", "Home", model);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error when checking IP", e);
                    return StatusCode(500, e.Message);
                }
                
            }
            else if (!conversionSuccessful)
            {
                _logger.LogError("Invailed IP input");
                return StatusCode(400, "Invailed IP input");
            }

            return RedirectToAction("Index", "Home", model);
            
        }

        [HttpPost]
        public IActionResult CheckIPAjax([FromBody] IndexViewModel model)
        {
            bool conversionSuccessful = System.Net.IPAddress.TryParse(model.IP, out System.Net.IPAddress inputIP);
            if (conversionSuccessful)
            {
                try
                {
                    foreach (var endPoint in _endpointList.Where(i => i.IPs != null))
                    {
                        foreach (var ipRange in endPoint.IPs)
                        {
                            if (IpHelper.IsInSubnet(inputIP, ipRange))
                            {
                                model.IsO365IP = true;
                                model.ServiceName = endPoint.serviceAreaDisplayName;
                                return Content(JsonConvert.SerializeObject(model), "application/json");
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    _logger.LogError("Error when checking IP", e);
                    return StatusCode(500, e.Message);
                }                
            }
            else if (!conversionSuccessful)
            {
                _logger.LogError("Invailed IP input");
                return StatusCode(400, "Invailed IP input");
                //return Json(new Dictionary<string, string> { { model.IP, "Invailed IP" } });
            }

            return Content(JsonConvert.SerializeObject(model), "application/json");
        }

        public IActionResult URLDeEnCode()
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
