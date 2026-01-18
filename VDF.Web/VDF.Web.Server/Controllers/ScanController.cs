using Microsoft.AspNetCore.Mvc;
using VDF.Web.Server.Services;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using System.IO;
using System.Linq;

namespace VDF.Web.Server.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class ScanController : ControllerBase {
        private readonly ScanService _scanService;

        public ScanController(ScanService scanService) {
            _scanService = scanService;
        }

        [HttpGet("status")]
        public IActionResult GetStatus() {
            return Ok(_scanService.GetStatus());
        }

        [HttpPost("start")]
        public IActionResult StartScan([FromBody] List<string> paths) {
            if (paths == null || paths.Count == 0)
                return BadRequest("No paths provided");
            
            _scanService.StartScan(paths);
            return Ok();
        }

        [HttpPost("stop")]
        public IActionResult StopScan() {
            _scanService.StopScan();
            return Ok();
        }
        
        [HttpGet("results")]
        public IActionResult GetResults() {
            var duplicates = _scanService.Engine.Duplicates.ToList(); // Snapshot
            
            // Group by GroupId
            var groups = duplicates.GroupBy(d => d.GroupId)
                .Select(g => new {
                    GroupId = g.Key,
                    Items = g.Select(d => new {
                        d.Path,
                        d.SizeLong,
                        Size = d.SizeLong, // Raw size for sorting
                        d.Duration,
                        d.FrameSize,
                        d.Similarity,
                        HasThumbnail = d.ImageList.Count > 0,
                        IsImage = d.IsImage
                    }).ToList()
                }).ToList();

            return Ok(groups);
        }

        [HttpGet("thumbnail")]
        public IActionResult GetThumbnail([FromQuery] string path) {
            var item = _scanService.Engine.Duplicates.FirstOrDefault(d => d.Path == path);
            if (item == null || item.ImageList.Count == 0)
                return NotFound();

            // Return the first thumbnail as JPEG
            using var ms = new MemoryStream();
            item.ImageList[0].SaveAsJpeg(ms);
            return File(ms.ToArray(), "image/jpeg");
        }
    }
}
