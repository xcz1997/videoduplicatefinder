using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;

namespace VDF.Web.Server.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class FileSystemController : ControllerBase {
        
        public class FileSystemNode {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public bool IsDirectory { get; set; }
            public bool HasChildren { get; set; }
        }

        [HttpGet("drives")]
        public IActionResult GetDrives() {
            try {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new FileSystemNode {
                        Name = d.Name,
                        Path = d.Name,
                        IsDirectory = true,
                        HasChildren = true
                    })
                    .DistinctBy(d => d.Path) // Remove duplicates
                    .ToList();
                return Ok(drives);
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("list")]
        public IActionResult GetDirectoryContents([FromQuery] string path) {
            if (string.IsNullOrEmpty(path)) return GetDrives();

            try {
                var dirInfo = new DirectoryInfo(path);
                var dirs = dirInfo.GetDirectories()
                    .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                    .Select(d => new FileSystemNode {
                        Name = d.Name,
                        Path = d.FullName,
                        IsDirectory = true,
                        HasChildren = true // Simplified assumption
                    })
                    .DistinctBy(d => d.Path) // Remove duplicates
                    .ToList();

                return Ok(dirs);
            } catch (Exception ex) {
                return StatusCode(500, $"Cannot access path: {ex.Message}");
            }
        }
    }
}
