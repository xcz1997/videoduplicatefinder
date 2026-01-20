using Microsoft.AspNetCore.Mvc;
using VDF.Web.Server.Services;
using VDF.GUI.Utils;
using VDF.GUI.Data;
using VDF.Core.FFTools;
using VDF.Core.History;
using VDF.Core.Trash;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;
using System.Linq;
using System.Text.Json;

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

        [HttpGet("recent-files")]
        public IActionResult GetRecentFiles() {
            return Ok(_scanService.GetRecentFiles());
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
                        HasThumbnail = d.ImageList.Count > 0 || HasCachedThumbnail(d.ThumbnailCacheKey),
                        ThumbnailCount = d.ImageList.Count,
                        d.IsImage,
                        // Best quality flags
                        d.IsBestSize,
                        d.IsBestDuration,
                        d.IsBestFrameSize,
                        d.IsBestFps,
                        d.IsBestBitRateKbs,
                        d.IsBestAudioSampleRate,
                        // Additional info for display
                        d.Fps,
                        d.BitRateKbs,
                        d.AudioSampleRate,
                        // Cache key for thumbnail retrieval
                        ThumbnailCacheKey = d.ThumbnailCacheKey
                    }).ToList()
                }).ToList();

            return Ok(groups);
        }

        private static bool HasCachedThumbnail(string cacheKey) {
            return ThumbCacheHelpers.Provider?.Contains(cacheKey) ?? false;
        }

        [HttpGet("thumbnail")]
        public IActionResult GetThumbnail([FromQuery] string path) {
            var item = _scanService.Engine.Duplicates.FirstOrDefault(d => d.Path == path);
            if (item == null)
                return NotFound();

            // Try to get from persistent cache first
            var cacheKey = item.ThumbnailCacheKey;
            if (ThumbCacheHelpers.Provider != null) {
                using var cachedStream = ThumbCacheHelpers.Provider.OpenKey(cacheKey);
                if (cachedStream != null) {
                    using var ms = new MemoryStream();
                    cachedStream.CopyTo(ms);
                    return File(ms.ToArray(), "image/jpeg");
                }
            }

            // Fall back to in-memory ImageList
            if (item.ImageList.Count == 0)
                return NotFound();

            // Join multiple thumbnails horizontally (like GUI does)
            byte[] bytes;
            using var memStream = new MemoryStream();

            if (item.ImageList.Count == 1) {
                // Single thumbnail
                item.ImageList[0].SaveAsJpeg(memStream);
            } else {
                // Multiple thumbnails - join horizontally
                int height = item.ImageList[0].Height;
                int totalWidth = item.ImageList.Sum(img => img.Width);

                using var joined = new Image<Rgba32>(totalWidth, height);
                joined.Mutate(ctx => {
                    int offsetX = 0;
                    foreach (var img in item.ImageList) {
                        ctx.DrawImage(img, new SixLabors.ImageSharp.Point(offsetX, 0), 1f);
                        offsetX += img.Width;
                    }
                });
                joined.SaveAsJpeg(memStream, new JpegEncoder { Quality = 90 });
            }

            bytes = memStream.ToArray();

            // Save to persistent cache for future use
            if (ThumbCacheHelpers.Provider != null) {
                ThumbCacheHelpers.Provider.AppendIfMissing(cacheKey, stream => {
                    stream.Write(bytes, 0, bytes.Length);
                });
            }

            return File(bytes, "image/jpeg");
        }

        /// <summary>
        /// Get a single thumbnail by index (for preview modal)
        /// </summary>
        [HttpGet("thumbnail-single")]
        public IActionResult GetThumbnailSingle([FromQuery] string path, [FromQuery] int index = 0, [FromQuery] bool fullsize = false) {
            var item = _scanService.Engine.Duplicates.FirstOrDefault(d => d.Path == path);
            if (item == null)
                return NotFound();

            // For images, return the original file
            if (item.IsImage) {
                if (!System.IO.File.Exists(path))
                    return NotFound();

                var ext = Path.GetExtension(path).ToLowerInvariant();
                var contentType = ext switch {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    _ => "application/octet-stream"
                };
                return File(System.IO.File.OpenRead(path), contentType);
            }

            // For videos
            if (index < 0 || index >= item.ThumbnailTimestamps.Count)
                index = 0;

            // Generate cache key for this specific frame
            // Format: {pathHash}_full_{index} for fullsize, {pathHash}_thumb_{index} for small
            var baseCacheKey = item.ThumbnailCacheKey;
            var frameCacheKey = fullsize ? $"{baseCacheKey}_full_{index}" : $"{baseCacheKey}_thumb_{index}";

            // Try to get from cache first
            if (ThumbCacheHelpers.Provider != null) {
                using var cachedStream = ThumbCacheHelpers.Provider.OpenKey(frameCacheKey);
                if (cachedStream != null) {
                    using var ms = new MemoryStream();
                    cachedStream.CopyTo(ms);
                    return File(ms.ToArray(), "image/jpeg");
                }
            }

            // If fullsize requested, use FFmpeg to get full resolution frame
            if (fullsize && item.ThumbnailTimestamps.Count > 0) {
                if (!System.IO.File.Exists(path))
                    return NotFound();

                try {
                    var timestamp = item.ThumbnailTimestamps[index];
                    var bytes = FfmpegEngine.GetThumbnail(new FfmpegSettings {
                        File = path,
                        Position = timestamp,
                        GrayScale = 0,
                        Fullsize = 1
                    }, VDF.GUI.Data.SettingsFile.Instance.ExtendedFFToolsLogging);

                    if (bytes != null && bytes.Length > 0) {
                        // Cache the fullsize frame
                        if (ThumbCacheHelpers.Provider != null) {
                            ThumbCacheHelpers.Provider.AppendIfMissing(frameCacheKey, stream => {
                                stream.Write(bytes, 0, bytes.Length);
                            });
                        }
                        return File(bytes, "image/jpeg");
                    }
                }
                catch {
                    // Fall back to cached thumbnail on error
                }
            }

            // Fall back to cached thumbnail from ImageList
            if (item.ImageList.Count == 0)
                return NotFound();

            if (index >= item.ImageList.Count)
                index = 0;

            using var memStream = new MemoryStream();
            item.ImageList[index].SaveAsJpeg(memStream, new JpegEncoder { Quality = 95 });
            var thumbBytes = memStream.ToArray();

            // Cache the small thumbnail frame
            if (ThumbCacheHelpers.Provider != null) {
                ThumbCacheHelpers.Provider.AppendIfMissing(frameCacheKey, stream => {
                    stream.Write(thumbBytes, 0, thumbBytes.Length);
                });
            }

            return File(thumbBytes, "image/jpeg");
        }

        [HttpPost("clear-database")]
        public IActionResult ClearDatabase() {
            if (_scanService.GetStatus().IsScanning)
                return BadRequest("Cannot clear database while scanning");

            VDF.Core.ScanEngine.ClearDatabase();
            // Also clear in-memory duplicates
            _scanService.Engine.Duplicates.Clear();
            return Ok(new { message = "Database cleared successfully" });
        }

        [HttpPost("cleanup-database")]
        public IActionResult CleanupDatabase() {
            if (_scanService.GetStatus().IsScanning)
                return BadRequest("Cannot cleanup database while scanning");

            _scanService.Engine.CleanupDatabase();
            return Ok(new { message = "Database cleanup started" });
        }

        [HttpPost("save-results")]
        public IActionResult SaveResults() {
            if (_scanService.GetStatus().IsScanning)
                return BadRequest("Cannot save results while scanning");

            var duplicates = _scanService.Engine.Duplicates.ToList();
            if (duplicates.Count == 0)
                return Ok(new { success = true, message = "No results to save", count = 0 });

            try {
                // Get backup file path (same logic as GUI)
                var settings = VDF.GUI.Data.SettingsFile.Instance;
                var backupPath = Directory.Exists(settings.CustomDatabaseFolder)
                    ? Path.Combine(settings.CustomDatabaseFolder, "backup.scanresults.json")
                    : Path.Combine(VDF.Core.Utils.CoreUtils.CurrentFolder, "backup.scanresults.json");

                // Serialize duplicates to JSON
                var options = new JsonSerializerOptions {
                    WriteIndented = true,
                    IncludeFields = true
                };

                // Create a simplified structure for saving
                var saveData = duplicates.Select(d => new {
                    d.Path,
                    d.SizeLong,
                    d.Duration,
                    d.FrameSize,
                    d.Similarity,
                    d.GroupId,
                    d.IsImage,
                    d.Fps,
                    d.BitRateKbs,
                    d.AudioSampleRate,
                    d.IsBestSize,
                    d.IsBestDuration,
                    d.IsBestFrameSize,
                    d.IsBestFps,
                    d.IsBestBitRateKbs,
                    d.IsBestAudioSampleRate,
                    d.ThumbnailCacheKey
                }).ToList();

                var json = JsonSerializer.Serialize(saveData, options);
                System.IO.File.WriteAllText(backupPath, json);

                // Also save the database
                VDF.Core.ScanEngine.SaveDatabase();

                return Ok(new {
                    success = true,
                    message = "Results saved successfully",
                    count = duplicates.Count,
                    path = backupPath
                });
            }
            catch (Exception ex) {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private TrashManager? _trashManager;

        private TrashManager GetTrashManager() {
            if (_trashManager == null) {
                var settings = SettingsFile.Instance;
                var trashFolder = TrashManager.ResolveTrashFolder(
                    settings.TrashFolderPath,
                    settings.TrashFolderRelativeToScan,
                    _scanService.Engine.Settings.IncludeList);
                _trashManager = new TrashManager(trashFolder);
            }
            return _trashManager;
        }

        private ScanHistoryManager GetHistoryManager() {
            // Use ScanService's history manager to ensure CurrentScanId is shared
            return _scanService.GetHistoryManager();
        }

        [HttpPost("delete")]
        public IActionResult DeleteFiles([FromBody] DeleteFilesRequest request) {
            var paths = request?.Paths;
            if (paths == null || paths.Count == 0)
                return BadRequest("No paths provided");

            if (_scanService.GetStatus().IsScanning)
                return BadRequest("Cannot delete files while scanning");

            var settings = SettingsFile.Instance;
            var useTrashFolder = !request.Permanently && settings.DefaultDeleteAction == DeleteAction.MoveToTrash;
            TrashManager? trashManager = useTrashFolder ? GetTrashManager() : null;
            var historyManager = settings.EnableScanHistory ? GetHistoryManager() : null;
            string? currentScanId = historyManager?.CurrentScanId;

            var results = new List<object>();
            var deletionItems = new List<DeletionItem>();
            var successCount = 0;
            var failCount = 0;
            long totalDeletedSize = 0;

            foreach (var path in paths) {
                // Security check: only allow deletion of files in duplicates list
                var item = _scanService.Engine.Duplicates.FirstOrDefault(d => d.Path == path);
                if (item == null) {
                    results.Add(new { path, success = false, error = "File not in duplicates list" });
                    failCount++;
                    continue;
                }

                try {
                    if (!System.IO.File.Exists(path)) {
                        results.Add(new { path, success = false, error = "File not found" });
                        failCount++;
                        continue;
                    }

                    string? trashId = null;
                    DeleteAction actionTaken = DeleteAction.PermanentDelete;

                    if (useTrashFolder && trashManager != null) {
                        // Move to trash folder
                        trashId = trashManager.MoveToTrash(path, currentScanId, item.GroupId);
                        if (trashId == null) {
                            results.Add(new { path, success = false, error = "Failed to move to trash" });
                            failCount++;
                            continue;
                        }
                        actionTaken = DeleteAction.MoveToTrash;
                    }
                    else {
                        // Permanent delete
                        System.IO.File.Delete(path);
                        actionTaken = DeleteAction.PermanentDelete;
                    }

                    // Remove from duplicates list
                    _scanService.Engine.Duplicates.Remove(item);

                    // Record deletion for history
                    if (historyManager != null && currentScanId != null) {
                        deletionItems.Add(new DeletionItem {
                            Path = path,
                            FileSize = item.SizeLong,
                            Action = actionTaken,
                            TrashId = trashId,
                            GroupId = item.GroupId
                        });
                    }

                    results.Add(new {
                        path,
                        success = true,
                        action = actionTaken.ToString(),
                        trashId,
                        fileSize = item.SizeLong
                    });
                    totalDeletedSize += item.SizeLong;
                    successCount++;
                }
                catch (Exception ex) {
                    results.Add(new { path, success = false, error = ex.Message });
                    failCount++;
                }
            }

            // Record deletions to history
            if (deletionItems.Count > 0 && historyManager != null && currentScanId != null) {
                historyManager.RecordDeletion(currentScanId, deletionItems);
            }

            return Ok(new {
                results,
                successCount,
                failCount,
                totalDeletedSize,
                totalDeletedSizeFormatted = FormatBytes(totalDeletedSize),
                message = $"Deleted {successCount} files, {failCount} failed"
            });
        }

        private static string FormatBytes(long bytes) {
            if (bytes == 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            if (i >= sizes.Length) i = sizes.Length - 1;
            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
        }

        public class DeleteFilesRequest {
            public List<string> Paths { get; set; } = new();
            public bool Permanently { get; set; }
        }

        [HttpGet("original")]
        public IActionResult GetOriginal([FromQuery] string path) {
            // Only allow access to files that are in the duplicates list (security)
            var item = _scanService.Engine.Duplicates.FirstOrDefault(d => d.Path == path);
            if (item == null)
                return NotFound();

            // Only serve original for image files
            if (!item.IsImage)
                return BadRequest("Original preview only available for images");

            if (!System.IO.File.Exists(path))
                return NotFound();

            // Determine content type based on extension
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var contentType = ext switch {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            var fileStream = System.IO.File.OpenRead(path);
            return File(fileStream, contentType);
        }
    }
}
