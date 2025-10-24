using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
      

        public SensorsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        [HttpGet("devices")]
        public async Task<IActionResult> GetSensorStatus()
        {
            var http = _httpClientFactory.CreateClient("iot");

            var tenatId = "dab37251-fa80-4a67-b7dd-6d65419b709f";
            var resp = await http.GetAsync($"api/tenants/{tenatId}/devices");
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode, "Failed to retrieve sensor data from IoT service.");
            }
            Console.WriteLine(resp.Content.ReadAsStringAsync());
            var content = await resp.Content.ReadAsStringAsync();


            return Ok(content);
        }

        [HttpPost("Rules")]
        public async Task<IActionResult> CreateSensorRule([FromBody] object ruleData)
        {
            var http = _httpClientFactory.CreateClient("rules");

            var resp = await http.PostAsJsonAsync("/Rules", ruleData);
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode, "Failed to create sensor rule in IoT service.");
            }

            var content = await resp.Content.ReadAsStringAsync();
            return Ok(content);
        }

        [HttpGet("Rules")]
        public async Task<IActionResult> GetSensorRules()
        {
            var http = _httpClientFactory.CreateClient("rules");

            var resp = await http.GetAsync("/Rules");
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode, "Failed to retrieve sensor rules from IoT service.");
            }

            var content = await resp.Content.ReadAsStringAsync();
            return Ok(content);
        }

        [HttpPut("Rules/{ruleId}")]
        public async Task<IActionResult> UpdateSensorRule(string ruleId, [FromBody] object ruleData)
        {
            var http = _httpClientFactory.CreateClient("rules");

            var resp = await http.PutAsJsonAsync($"/Rules/{ruleId}", ruleData);
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode, "Failed to update sensor rule in IoT service.");
            }

            var content = await resp.Content.ReadAsStringAsync();
            return Ok(content);
        }
        
        [HttpDelete("Rules/{ruleId}")]
        public async Task<IActionResult> DeleteSensorRule(string ruleId)
        {
            var http = _httpClientFactory.CreateClient("rules");

            var resp = await http.DeleteAsync($"/Rules/{ruleId}");
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode, "Failed to delete sensor rule in IoT service.");
            }

            return NoContent();
        }


        [HttpGet("Alerts")]
        public async Task<IActionResult> GetSensorAlerts()
        {
            var http = _httpClientFactory.CreateClient("rules");

            var resp = await http.GetAsync("/alerts");
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode, "Failed to retrieve sensor rules from IoT service.");
            }

            var content = await resp.Content.ReadAsStringAsync();
            return Ok(content);
        }

    }
}
