using Microsoft.AspNetCore.Mvc;
using VDF.Web.Server.Services;
using VDF.GUI.Data;
using VDF.GUI.Utils;
using VDF.Core.Trash;
using VDF.Core.Utils;
using VDF.Core.History;

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

        /// <summary>
        /// Gets all data path information including resolved paths and sizes.
        /// </summary>
        [HttpGet("paths-info")]
        public IActionResult GetPathsInfo() {
            var s = SettingsFile.Instance;

            // Resolve actual paths
            var dataFolder = DataPaths.GetDataFolder(s.DataFolder);
            var databaseFolder = DataPaths.GetDatabaseFolder(s.CustomDatabaseFolder, s.DataFolder);
            var cacheFolder = ThumbCacheHelpers.GetCacheFolder();
            var historyFolder = ScanHistoryManager.ResolveHistoryFolder(s.HistoryFolderPath, s.DataFolder);
            var trashFolder = TrashManager.ResolveTrashFolder(
                s.TrashFolderPath,
                s.TrashFolderRelativeToScan,
                _scanService.Engine.Settings.IncludeList);

            return Ok(new PathsInfoResponse {
                DataFolder = new PathInfo {
                    ConfiguredPath = s.DataFolder,
                    ResolvedPath = dataFolder,
                    Size = GetDirectorySize(dataFolder),
                    SizeFormatted = FormatSize(GetDirectorySize(dataFolder))
                },
                DatabaseFolder = new PathInfo {
                    ConfiguredPath = s.CustomDatabaseFolder,
                    ResolvedPath = databaseFolder,
                    Size = GetDirectorySize(databaseFolder),
                    SizeFormatted = FormatSize(GetDirectorySize(databaseFolder))
                },
                CacheFolder = new PathInfo {
                    ConfiguredPath = s.ThumbnailCacheFolder,
                    ResolvedPath = cacheFolder,
                    Size = ThumbCacheHelpers.GetCacheSize(),
                    SizeFormatted = ThumbCacheHelpers.FormatCacheSize(ThumbCacheHelpers.GetCacheSize())
                },
                HistoryFolder = new PathInfo {
                    ConfiguredPath = s.HistoryFolderPath,
                    ResolvedPath = historyFolder,
                    Size = GetDirectorySize(historyFolder),
                    SizeFormatted = FormatSize(GetDirectorySize(historyFolder))
                },
                TrashFolder = new PathInfo {
                    ConfiguredPath = s.TrashFolderPath,
                    ResolvedPath = trashFolder,
                    Size = GetDirectorySize(trashFolder),
                    SizeFormatted = FormatSize(GetDirectorySize(trashFolder))
                }
            });
        }

        private static long GetDirectorySize(string path) {
            if (!Directory.Exists(path)) return 0;
            try {
                return new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(fi => fi.Length);
            }
            catch {
                return 0;
            }
        }

        private static string FormatSize(long bytes) {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
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

        /// <summary>
        /// Gets the current delete policy settings.
        /// </summary>
        [HttpGet("delete-policy")]
        public IActionResult GetDeletePolicy() {
            var s = SettingsFile.Instance;
            return Ok(new DeletePolicySettings {
                DefaultDeleteAction = s.DefaultDeleteAction,
                TrashFolderPath = s.TrashFolderPath,
                TrashFolderRelativeToScan = s.TrashFolderRelativeToScan,
                AutoExcludeTrashFolder = s.AutoExcludeTrashFolder,
                TrashRetentionDays = s.TrashRetentionDays,
                EnableScanHistory = s.EnableScanHistory,
                MaxHistoryDays = s.MaxHistoryDays,
                SaveThumbnailsInHistory = s.SaveThumbnailsInHistory,
                HistoryFolderPath = s.HistoryFolderPath
            });
        }

        /// <summary>
        /// Updates the delete policy settings.
        /// </summary>
        [HttpPut("delete-policy")]
        public IActionResult UpdateDeletePolicy([FromBody] DeletePolicySettings settings) {
            if (settings == null) {
                return BadRequest(new { Error = "Settings cannot be null" });
            }

            // Validate paths
            if (string.IsNullOrWhiteSpace(settings.TrashFolderPath)) {
                return BadRequest(new { Error = "Trash folder path cannot be empty" });
            }
            if (string.IsNullOrWhiteSpace(settings.HistoryFolderPath)) {
                return BadRequest(new { Error = "History folder path cannot be empty" });
            }

            // Validate retention days
            if (settings.TrashRetentionDays < 1 || settings.TrashRetentionDays > 365) {
                return BadRequest(new { Error = "Trash retention days must be between 1 and 365" });
            }
            if (settings.MaxHistoryDays < 1 || settings.MaxHistoryDays > 365) {
                return BadRequest(new { Error = "History retention days must be between 1 and 365" });
            }

            var s = SettingsFile.Instance;
            s.DefaultDeleteAction = settings.DefaultDeleteAction;
            s.TrashFolderPath = settings.TrashFolderPath;
            s.TrashFolderRelativeToScan = settings.TrashFolderRelativeToScan;
            s.AutoExcludeTrashFolder = settings.AutoExcludeTrashFolder;
            s.TrashRetentionDays = settings.TrashRetentionDays;
            s.EnableScanHistory = settings.EnableScanHistory;
            s.MaxHistoryDays = settings.MaxHistoryDays;
            s.SaveThumbnailsInHistory = settings.SaveThumbnailsInHistory;
            s.HistoryFolderPath = settings.HistoryFolderPath;

            SettingsFile.SaveSettings();

            return Ok(new { Success = true, Message = "Delete policy settings updated" });
        }

        /// <summary>
        /// Gets available delete actions.
        /// </summary>
        [HttpGet("delete-actions")]
        public IActionResult GetDeleteActions() {
            var actions = Enum.GetValues<DeleteAction>()
                .Select(a => new {
                    Value = (int)a,
                    Name = a.ToString(),
                    Description = a switch {
                        DeleteAction.MoveToTrash => "Move files to trash folder (can be restored)",
                        DeleteAction.PermanentDelete => "Permanently delete files (cannot be restored)",
                        _ => a.ToString()
                    }
                });
            return Ok(actions);
        }
    }

    /// <summary>
    /// DTO for delete policy settings.
    /// </summary>
    public class DeletePolicySettings {
        public DeleteAction DefaultDeleteAction { get; set; }
        public string TrashFolderPath { get; set; } = ".trash";
        public bool TrashFolderRelativeToScan { get; set; } = true;
        public bool AutoExcludeTrashFolder { get; set; } = true;
        public int TrashRetentionDays { get; set; } = 30;
        public bool EnableScanHistory { get; set; } = true;
        public int MaxHistoryDays { get; set; } = 30;
        public bool SaveThumbnailsInHistory { get; set; }
        public string HistoryFolderPath { get; set; } = "history";
    }

    /// <summary>
    /// Information about a single path.
    /// </summary>
    public class PathInfo {
        public string ConfiguredPath { get; set; } = string.Empty;
        public string ResolvedPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string SizeFormatted { get; set; } = "0 B";
    }

    /// <summary>
    /// Response containing all data path information.
    /// </summary>
    public class PathsInfoResponse {
        public PathInfo DataFolder { get; set; } = new();
        public PathInfo DatabaseFolder { get; set; } = new();
        public PathInfo CacheFolder { get; set; } = new();
        public PathInfo HistoryFolder { get; set; } = new();
        public PathInfo TrashFolder { get; set; } = new();
    }
}
