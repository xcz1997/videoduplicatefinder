using Microsoft.AspNetCore.Mvc;
using VDF.Web.Server.Services;
using VDF.GUI.Data;

namespace VDF.Web.Server.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase {
        private readonly ScanService _scanService;

        public SettingsController(ScanService scanService) {
            _scanService = scanService;
        }

        [HttpGet]
        public IActionResult GetSettings() {
            return Ok(_scanService.GetSettings());
        }

        [HttpPost]
        public IActionResult SaveSettings([FromBody] SettingsFile settings) {
            _scanService.SaveSettings(settings);
            return Ok();
        }
    }
}
