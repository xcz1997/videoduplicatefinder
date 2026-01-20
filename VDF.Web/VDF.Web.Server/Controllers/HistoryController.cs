using Microsoft.AspNetCore.Mvc;
using VDF.Core.History;
using VDF.GUI.Data;
using VDF.Web.Server.Services;

namespace VDF.Web.Server.Controllers {
	[ApiController]
	[Route("api/[controller]")]
	public class HistoryController : ControllerBase {
		private readonly ScanService _scanService;
		private ScanHistoryManager? _historyManager;

		public HistoryController(ScanService scanService) {
			_scanService = scanService;
		}

		private ScanHistoryManager GetHistoryManager() {
			if (_historyManager == null) {
				var historyFolder = ScanHistoryManager.ResolveHistoryFolder(
					SettingsFile.Instance.HistoryFolderPath,
					SettingsFile.Instance.DataFolder);
				_historyManager = new ScanHistoryManager(historyFolder);
			}
			return _historyManager;
		}

		/// <summary>
		/// Get the list of all scan history entries.
		/// </summary>
		[HttpGet]
		public IActionResult GetHistoryList() {
			var manager = GetHistoryManager();
			var entries = manager.GetHistoryList();
			return Ok(new {
				entries = entries.Select(e => new {
					scanId = e.ScanId,
					timestamp = e.Timestamp,
					folders = e.Folders,
					duplicateGroups = e.DuplicateGroups,
					duplicateItems = e.DuplicateItems,
					totalDuplicateSize = e.TotalDuplicateSize,
					deletedCount = e.DeletedCount,
					deletedSize = e.DeletedSize
				}),
				totalCount = entries.Count
			});
		}

		/// <summary>
		/// Get detailed information about a specific scan history.
		/// </summary>
		[HttpGet("{scanId}")]
		public IActionResult GetHistoryDetails(string scanId) {
			var manager = GetHistoryManager();
			var entry = manager.GetHistoryDetails(scanId);
			if (entry == null)
				return NotFound(new { error = "History entry not found" });

			return Ok(new {
				scanId = entry.ScanId,
				timestamp = entry.Timestamp,
				folders = entry.Folders,
				totalFiles = entry.TotalFiles,
				duplicateGroups = entry.DuplicateGroups,
				duplicateItems = entry.DuplicateItems,
				totalDuplicateSize = entry.TotalDuplicateSize,
				duration = entry.Duration,
				deletedCount = entry.DeletedCount,
				deletedSize = entry.DeletedSize,
				duplicates = entry.Duplicates.Select(d => new {
					path = d.Path,
					sizeLong = d.SizeLong,
					duration = d.Duration,
					frameSize = d.FrameSize,
					similarity = d.Similarity,
					groupId = d.GroupId,
					isImage = d.IsImage,
					fps = d.Fps,
					bitRateKbs = d.BitRateKbs
				}),
				deletions = entry.Deletions.SelectMany(dr => dr.Items.Select(di => new {
					timestamp = dr.Timestamp,
					path = di.Path,
					fileSize = di.FileSize,
					action = di.Action.ToString(),
					trashId = di.TrashId,
					groupId = di.GroupId,
					restored = di.Restored,
					restoredAt = di.RestoredAt
				}))
			});
		}

		/// <summary>
		/// Get deletion records for a specific scan.
		/// </summary>
		[HttpGet("{scanId}/deletions")]
		public IActionResult GetDeletions(string scanId) {
			var manager = GetHistoryManager();
			var entry = manager.GetHistoryDetails(scanId);
			if (entry == null)
				return NotFound(new { error = "History entry not found" });

			var deletions = entry.Deletions.SelectMany(dr => dr.Items.Select(di => new {
				timestamp = dr.Timestamp,
				path = di.Path,
				fileSize = di.FileSize,
				action = di.Action.ToString(),
				trashId = di.TrashId,
				groupId = di.GroupId,
				restored = di.Restored,
				restoredAt = di.RestoredAt
			}));

			return Ok(new {
				scanId,
				deletions,
				totalCount = deletions.Count(),
				totalSize = entry.DeletedSize
			});
		}

		/// <summary>
		/// Delete a history entry.
		/// </summary>
		[HttpDelete("{scanId}")]
		public IActionResult DeleteHistory(string scanId) {
			var manager = GetHistoryManager();
			var success = manager.DeleteHistory(scanId);
			if (!success)
				return BadRequest(new { error = "Failed to delete history entry" });

			return Ok(new { success = true, message = "History entry deleted" });
		}

		/// <summary>
		/// Clean up old history entries.
		/// </summary>
		[HttpPost("cleanup")]
		public IActionResult CleanupOldHistory([FromBody] HistoryCleanupRequest? request = null) {
			var days = request?.Days ?? SettingsFile.Instance.MaxHistoryDays;
			var manager = GetHistoryManager();
			var count = manager.CleanupOldHistory(days);
			return Ok(new {
				success = true,
				deletedCount = count,
				message = $"Cleaned up {count} history entries older than {days} days"
			});
		}

		/// <summary>
		/// Get history entries filtered by date range.
		/// </summary>
		[HttpGet("bydate")]
		public IActionResult GetHistoryByDateRange([FromQuery] DateTime from, [FromQuery] DateTime to) {
			var manager = GetHistoryManager();
			var entries = manager.GetHistoryByDateRange(from, to);
			return Ok(new {
				entries = entries.Select(e => new {
					scanId = e.ScanId,
					timestamp = e.Timestamp,
					folders = e.Folders,
					duplicateGroups = e.DuplicateGroups,
					duplicateItems = e.DuplicateItems,
					totalDuplicateSize = e.TotalDuplicateSize,
					deletedCount = e.DeletedCount,
					deletedSize = e.DeletedSize
				}),
				totalCount = entries.Count
			});
		}

		/// <summary>
		/// Search history entries by folder path.
		/// </summary>
		[HttpGet("search")]
		public IActionResult SearchHistoryByFolder([FromQuery] string folder) {
			if (string.IsNullOrWhiteSpace(folder))
				return BadRequest(new { error = "Folder parameter is required" });

			var manager = GetHistoryManager();
			var entries = manager.GetHistoryByFolder(folder);
			return Ok(new {
				entries = entries.Select(e => new {
					scanId = e.ScanId,
					timestamp = e.Timestamp,
					folders = e.Folders,
					duplicateGroups = e.DuplicateGroups,
					duplicateItems = e.DuplicateItems,
					totalDuplicateSize = e.TotalDuplicateSize,
					deletedCount = e.DeletedCount,
					deletedSize = e.DeletedSize
				}),
				totalCount = entries.Count
			});
		}
	}

	public class HistoryCleanupRequest {
		public int Days { get; set; }
	}
}
