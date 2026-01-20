using Microsoft.AspNetCore.Mvc;
using VDF.Core.Trash;
using VDF.GUI.Data;
using VDF.Web.Server.Services;

namespace VDF.Web.Server.Controllers {
	[ApiController]
	[Route("api/[controller]")]
	public class TrashController : ControllerBase {
		private readonly ScanService _scanService;
		private TrashManager? _trashManager;

		public TrashController(ScanService scanService) {
			_scanService = scanService;
		}

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

		/// <summary>
		/// Get all items in the trash.
		/// </summary>
		[HttpGet]
		public IActionResult GetTrashItems() {
			var manager = GetTrashManager();
			var items = manager.GetTrashItems();
			return Ok(new {
				items = items.Select(i => new {
					id = i.Metadata.Id,
					fileName = i.Metadata.FileName,
					originalPath = i.Metadata.OriginalPath,
					fileSize = i.Metadata.FileSize,
					sizeDisplay = i.SizeDisplay,
					deletedAt = i.Metadata.DeletedAt,
					deletedAgo = i.DeletedAgo,
					scanId = i.Metadata.ScanId,
					groupId = i.Metadata.GroupId,
					fileExists = i.FileExists,
					canRestore = i.CanRestore
				}),
				totalSize = manager.TotalSize,
				itemCount = manager.ItemCount
			});
		}

		/// <summary>
		/// Get a single trash item by ID.
		/// </summary>
		[HttpGet("{id}")]
		public IActionResult GetTrashItem(string id) {
			var manager = GetTrashManager();
			var item = manager.GetTrashItem(id);
			if (item == null)
				return NotFound(new { error = "Trash item not found" });

			return Ok(new {
				id = item.Metadata.Id,
				fileName = item.Metadata.FileName,
				originalPath = item.Metadata.OriginalPath,
				fileSize = item.Metadata.FileSize,
				sizeDisplay = item.SizeDisplay,
				deletedAt = item.Metadata.DeletedAt,
				deletedAgo = item.DeletedAgo,
				scanId = item.Metadata.ScanId,
				groupId = item.Metadata.GroupId,
				trashPath = item.Metadata.TrashPath,
				fileExists = item.FileExists,
				canRestore = item.CanRestore
			});
		}

		/// <summary>
		/// Restore a single item from trash to its original location.
		/// </summary>
		[HttpPost("{id}/restore")]
		public IActionResult RestoreItem(string id, [FromBody] RestoreOptions? options = null) {
			var manager = GetTrashManager();
			var success = manager.RestoreFromTrash(id, options?.OverwriteExisting ?? false);
			if (!success)
				return BadRequest(new { error = "Failed to restore item" });

			return Ok(new { success = true, message = "Item restored successfully" });
		}

		/// <summary>
		/// Restore multiple items from trash.
		/// </summary>
		[HttpPost("restore")]
		public IActionResult RestoreItems([FromBody] RestoreBatchRequest request) {
			var manager = GetTrashManager();
			var results = new List<object>();
			int successCount = 0, failCount = 0;

			foreach (var id in request.Ids) {
				var success = manager.RestoreFromTrash(id, request.OverwriteExisting);
				if (success) {
					successCount++;
					results.Add(new { id, success = true });
				}
				else {
					failCount++;
					results.Add(new { id, success = false, error = "Failed to restore" });
				}
			}

			return Ok(new {
				results,
				successCount,
				failCount,
				message = $"Restored {successCount} items, {failCount} failed"
			});
		}

		/// <summary>
		/// Permanently delete a single item from trash.
		/// </summary>
		[HttpDelete("{id}")]
		public IActionResult DeleteItem(string id) {
			var manager = GetTrashManager();
			var success = manager.PermanentDelete(id);
			if (!success)
				return BadRequest(new { error = "Failed to delete item" });

			return Ok(new { success = true, message = "Item permanently deleted" });
		}

		/// <summary>
		/// Empty the entire trash.
		/// </summary>
		[HttpDelete]
		public IActionResult EmptyTrash() {
			var manager = GetTrashManager();
			var count = manager.EmptyTrash();
			return Ok(new {
				success = true,
				deletedCount = count,
				message = $"Deleted {count} items from trash"
			});
		}

		/// <summary>
		/// Clean up old items from trash.
		/// </summary>
		[HttpPost("cleanup")]
		public IActionResult CleanupOldItems([FromBody] CleanupRequest? request = null) {
			var days = request?.Days ?? SettingsFile.Instance.TrashRetentionDays;
			var manager = GetTrashManager();
			var count = manager.CleanupOldItems(days);
			return Ok(new {
				success = true,
				deletedCount = count,
				message = $"Cleaned up {count} items older than {days} days"
			});
		}

		/// <summary>
		/// Get preview/content of a trash item (for images and videos).
		/// </summary>
		[HttpGet("{id}/preview")]
		public IActionResult GetPreview(string id) {
			var manager = GetTrashManager();
			var item = manager.GetTrashItem(id);
			if (item == null)
				return NotFound(new { error = "Trash item not found" });

			if (!item.FileExists)
				return NotFound(new { error = "File no longer exists in trash" });

			var trashPath = item.Metadata.TrashPath;
			if (string.IsNullOrEmpty(trashPath) || !System.IO.File.Exists(trashPath))
				return NotFound(new { error = "Trash file not found" });

			// Determine content type based on file extension
			var ext = System.IO.Path.GetExtension(trashPath).ToLowerInvariant();
			var contentType = ext switch {
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".bmp" => "image/bmp",
				".webp" => "image/webp",
				".mp4" => "video/mp4",
				".mkv" => "video/x-matroska",
				".avi" => "video/x-msvideo",
				".mov" => "video/quicktime",
				".wmv" => "video/x-ms-wmv",
				".flv" => "video/x-flv",
				".webm" => "video/webm",
				".m4v" => "video/x-m4v",
				".mpeg" or ".mpg" => "video/mpeg",
				".3gp" => "video/3gpp",
				_ => "application/octet-stream"
			};

			var fileStream = System.IO.File.OpenRead(trashPath);
			return File(fileStream, contentType, enableRangeProcessing: true);
		}
	}

	public class RestoreOptions {
		public bool OverwriteExisting { get; set; }
	}

	public class RestoreBatchRequest {
		public List<string> Ids { get; set; } = new();
		public bool OverwriteExisting { get; set; }
	}

	public class CleanupRequest {
		public int Days { get; set; }
	}
}
