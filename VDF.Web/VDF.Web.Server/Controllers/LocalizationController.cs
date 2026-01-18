using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;

namespace VDF.Web.Server.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class LocalizationController : ControllerBase {
        private readonly string _localesPath;

        public LocalizationController() {
            // Locales are copied from VDF.GUI project via "CopyToOutputDirectory"
            // They should be in AppDomain.CurrentDomain.BaseDirectory/Assets/Locales/
            _localesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Locales");
        }

        [HttpGet("{lang}")]
        public IActionResult GetLocale(string lang) {
            var filePath = Path.Combine(_localesPath, $"{lang}.json");
            if (!System.IO.File.Exists(filePath)) {
                // Fallback to en if specific lang not found, or return 404
                if (lang.Contains("-")) {
                    // Try generic (e.g. zh-CN -> zh)
                    filePath = Path.Combine(_localesPath, $"{lang.Split('-')[0]}.json");
                }
                
                if (!System.IO.File.Exists(filePath))
                    return NotFound($"Locale {lang} not found");
            }

            var json = System.IO.File.ReadAllText(filePath);
            // Parse and return to ensure valid JSON content type
            return Content(json, "application/json");
        }

        [HttpGet("list")]
        public IActionResult GetAvailableLanguages() {
            if (!Directory.Exists(_localesPath)) return Ok(new string[] { "en" });
            
            var files = Directory.GetFiles(_localesPath, "*.json");
            var langs = files.Select(Path.GetFileNameWithoutExtension).ToList();
            return Ok(langs);
        }
    }
}
