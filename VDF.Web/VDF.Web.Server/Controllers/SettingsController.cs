using Microsoft.AspNetCore.Mvc;
using VDF.Web.Server.Services;
using VDF.GUI.Data;
using VDF.GUI.Utils;

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

        [HttpGet("cache-info")]
        public IActionResult GetCacheInfo() {
            var cacheSize = ThumbCacheHelpers.GetCacheSize();
            var cacheFolder = ThumbCacheHelpers.GetCacheFolder();
            return Ok(new {
                CacheSize = cacheSize,
                CacheSizeFormatted = ThumbCacheHelpers.FormatCacheSize(cacheSize),
                CacheFolder = cacheFolder,
                DefaultCacheFolder = ThumbCacheHelpers.GetDefaultCacheFolder()
            });
        }

        [HttpPost("clear-cache")]
        public IActionResult ClearCache() {
            ThumbCacheHelpers.ClearCache();
            return Ok(new { Success = true, Message = "Thumbnail cache cleared successfully" });
        }

        [HttpPatch("includes")]
        public IActionResult UpdateIncludes([FromBody] List<string> paths) {
            var s = VDF.GUI.Data.SettingsFile.Instance;
            s.Includes.Clear();
            foreach (var path in paths) {
                s.Includes.Add(path);
            }
            VDF.GUI.Data.SettingsFile.SaveSettings();
            return Ok(new { Success = true });
        }
    }
}
