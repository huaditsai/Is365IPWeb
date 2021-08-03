using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Is365IPWeb
{
    public class IPVersion
    {
        public string Instance { get; set; } //The short name of the Office 365 service instance               
        public string Latest { get; set; } //The latest version for endpoints of the specified instance.
        public string[] Versions { get; set; } //A list of all previous versions for the specified instance. This element is only included if the AllVersions parameter is true.
    }

    public class EndPointList
    {
        public string Id { get; set; } //The immutable id number of the endpoint set.
        public string ServiceArea { get; set; } //The service area that this is part of: Common, Exchange, SharePoint, or Skype.
        public string[] URLs { get; set; } //URLs for the endpoint set. A JSON array of DNS records. Omitted if blank.
        public string TcpPorts { get; set; } //TCP ports for the endpoint set. All ports elements are formatted as a comma-separated list of ports or port ranges separated by a dash character (-). Ports apply to all IP addresses and all URLs in the endpoint set for a given category. Omitted if blank.
        public string UdpPorts { get; set; } //UDP ports for the IP address ranges in this endpoint set. Omitted if blank.
        public string[] IPs { get; set; } //The IP address ranges associated with this endpoint set as associated with the listed TCP or UDP ports. A JSON array of IP address ranges. Omitted if blank.
        public string Category { get; set; } //The connectivity category for the endpoint set. Valid values are Optimize, Allow, and Default.
        public bool ExpressRoute { get; set; } //True if this endpoint set is routed over ExpressRoute, False if not.
        public bool Required { get; set; } //True if this endpoint set is required to have connectivity for Office 365 to be supported.
        public string Notes { get; set; } //For optional endpoints, this text describes Office 365 functionality that would be unavailable if IP addresses or URLs in this endpoint set cannot be accessed at the network layer. Omitted if blank.
    }

    public class EndpointHelper
    {
        private readonly ILogger<EndpointHelper> _logger;
        private readonly IWebHostEnvironment _env;
        public EndpointHelper(IWebHostEnvironment webHostEnvironment)
        {
            _env = webHostEnvironment;
        }

        public async Task<List<EndPointList>> GetEndpointList()
        {
            List<EndPointList> endpoints;

            //1. Check version
            var currentVersionFilePath = Path.Combine(_env.WebRootPath, @"Files\CurrentVersion.json");
            var currentVersion = JsonConvert.DeserializeObject<IPVersion>(System.IO.File.ReadAllText(currentVersionFilePath));
            var lastVersion = await GetLastVersion();

            if (currentVersion.Latest != lastVersion.Latest)
            {
                //2. Download new endpoint list
                endpoints = await GetLastEndpoints();

                //3. Update current version file
                if (endpoints != null && endpoints.Count > 0)
                    File.WriteAllText(currentVersionFilePath, JsonConvert.SerializeObject(lastVersion), System.Text.Encoding.UTF8);
            }
            else //Read endpoints file
            {
                endpoints = await GetCurrentEndpoints();
            }

            return endpoints;
        }

        public async Task<IPVersion> GetLastVersion()
        {
            Guid guid = Guid.NewGuid();
            try
            {
                using HttpClient httpClient = new HttpClient();
                string url = $"https://endpoints.office.com/version/Worldwide?AllVersions=false&clientrequestid={guid}";
                HttpResponseMessage response = await httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                if (responseBody != null)
                {
                    var lastVersion = JsonConvert.DeserializeObject<IPVersion>(responseBody);
                    return lastVersion;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error when getting endpoint version info");
            }

            return null;
        }

        public async Task<List<EndPointList>> GetLastEndpoints()
        {
            Guid guid = Guid.NewGuid();
            try
            {
                using HttpClient httpClient = new HttpClient();
                string url = $"https://endpoints.office.com/endpoints/worldwide?clientrequestid={guid}";
                HttpResponseMessage response = await httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                if (responseBody != null)
                {
                    var endPointsFilePath = Path.Combine(_env.WebRootPath, @"Files\EndPoints.json");
                    await System.IO.File.WriteAllTextAsync(endPointsFilePath, responseBody, System.Text.Encoding.UTF8);

                    var endpoints = JsonConvert.DeserializeObject<List<EndPointList>>(responseBody);
                    return endpoints;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error when downloading new Office 365 endpoints");
            }

            return null;
        }

        public async Task<List<EndPointList>> GetCurrentEndpoints()
        {
            try
            {
                var endPointsFilePath = Path.Combine(_env.WebRootPath, @"Files\EndPoints.json");
                var content = await System.IO.File.ReadAllTextAsync(endPointsFilePath);
                var endpoints = JsonConvert.DeserializeObject<List<EndPointList>>(content);
                return endpoints;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error when getting current endpoint version info");
            }

            return null;
        }
    }
}
