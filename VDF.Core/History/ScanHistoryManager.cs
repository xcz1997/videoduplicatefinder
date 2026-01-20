// /*
//     Copyright (C) 2025 0x90d
//     This file is part of VideoDuplicateFinder
//     VideoDuplicateFinder is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//     VideoDuplicateFinder is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//     You should have received a copy of the GNU Affero General Public License
//     along with VideoDuplicateFinder.  If not, see <http://www.gnu.org/licenses/>.
// */

using System.Linq;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using VDF.Core.Trash;
using VDF.Core.Utils;
using VDF.Core.ViewModels;

namespace VDF.Core.History {
	/// <summary>
	/// Manages scan history recording and retrieval.
	/// </summary>
	public class ScanHistoryManager {
		private readonly string _historyFolder;
		private readonly string _indexPath;
		private readonly string _scansFolder;
		private HistoryIndex _index;
		private readonly object _lock = new();
		private static readonly JsonSerializerOptions _jsonOptions = new() {
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			IncludeFields = true
		};

		/// <summary>
		/// The current active scan ID, set when a scan is saved.
		/// </summary>
		public string? CurrentScanId { get; private set; }

		/// <summary>
		/// Creates a new ScanHistoryManager for the specified history folder.
		/// </summary>
		/// <param name="historyFolder">Full path to the history folder.</param>
		public ScanHistoryManager(string historyFolder) {
			_historyFolder = historyFolder;
			_indexPath = Path.Combine(_historyFolder, "index.json");
			_scansFolder = Path.Combine(_historyFolder, "scans");
			_index = new HistoryIndex();
			Initialize();
		}

		/// <summary>
		/// Gets the path to the history folder.
		/// </summary>
		public string HistoryFolder => _historyFolder;

		/// <summary>
		/// Gets the number of history entries.
		/// </summary>
		public int EntryCount => _index.Entries.Count;

		/// <summary>
		/// Initializes the history folder structure and loads the index.
		/// </summary>
		private void Initialize() {
			try {
				if (!Directory.Exists(_historyFolder)) {
					Directory.CreateDirectory(_historyFolder);
				}
				if (!Directory.Exists(_scansFolder)) {
					Directory.CreateDirectory(_scansFolder);
				}
				LoadIndex();
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to initialize history folder: {ex.Message}");
			}
		}

		/// <summary>
		/// Loads the history index from disk, or rebuilds it if corrupted.
		/// </summary>
		private void LoadIndex() {
			lock (_lock) {
				if (File.Exists(_indexPath)) {
					try {
						var json = File.ReadAllText(_indexPath);
						_index = JsonSerializer.Deserialize<HistoryIndex>(json, _jsonOptions) ?? new HistoryIndex();
						return;
					}
					catch (Exception ex) {
						Logger.Instance.Info($"Failed to load history index, rebuilding: {ex.Message}");
					}
				}
				RebuildIndex();
			}
		}

		/// <summary>
		/// Saves the history index to disk.
		/// </summary>
		private void SaveIndex() {
			lock (_lock) {
				try {
					_index.LastUpdated = DateTime.UtcNow;
					var json = JsonSerializer.Serialize(_index, _jsonOptions);
					File.WriteAllText(_indexPath, json);
				}
				catch (Exception ex) {
					Logger.Instance.Info($"Failed to save history index: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Rebuilds the history index by scanning the scans folder.
		/// </summary>
		public void RebuildIndex() {
			lock (_lock) {
				_index = new HistoryIndex();
				if (!Directory.Exists(_scansFolder)) {
					SaveIndex();
					return;
				}

				foreach (var file in Directory.GetFiles(_scansFolder, "*.json")) {
					try {
						var json = File.ReadAllText(file);
						var entry = JsonSerializer.Deserialize<ScanHistoryEntry>(json, _jsonOptions);
						if (entry != null) {
							_index.Entries.Add(new HistorySummary {
								ScanId = entry.ScanId,
								Timestamp = entry.Timestamp,
								Folders = entry.Folders,
								DuplicateGroups = entry.DuplicateGroups,
								DuplicateItems = entry.DuplicateItems,
								TotalDuplicateSize = entry.TotalDuplicateSize,
								DeletedCount = entry.DeletedCount,
								DeletedSize = entry.DeletedSize,
								HistoryFile = file
							});
						}
					}
					catch (Exception ex) {
						Logger.Instance.Info($"Failed to read history file {file}: {ex.Message}");
					}
				}

				// Sort by timestamp descending
				_index.Entries = _index.Entries.OrderByDescending(e => e.Timestamp).ToList();
				SaveIndex();
			}
		}

		/// <summary>
		/// Saves a scan result to history.
		/// </summary>
		/// <param name="duplicates">The duplicate items found.</param>
		/// <param name="scanFolders">The folders that were scanned.</param>
		/// <param name="scanDuration">How long the scan took.</param>
		/// <param name="totalFilesScanned">Total number of files scanned.</param>
		/// <param name="saveThumbnails">Whether to save thumbnails with the history.</param>
		/// <returns>The scan ID if saved successfully.</returns>
		public string? SaveScanHistory(
			IEnumerable<DuplicateItem> duplicates,
			IEnumerable<string> scanFolders,
			TimeSpan scanDuration,
			int totalFilesScanned,
			bool saveThumbnails = false) {

			var dupList = duplicates.ToList();
			if (dupList.Count == 0) {
				Logger.Instance.Info("No duplicates found, not saving to history");
				return null;
			}

			try {
				var scanId = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss");
				var groups = dupList.GroupBy(d => d.GroupId).ToList();

				var entry = new ScanHistoryEntry {
					ScanId = scanId,
					Timestamp = DateTime.UtcNow,
					Folders = scanFolders.ToList(),
					TotalFiles = totalFilesScanned,
					DuplicateGroups = groups.Count,
					DuplicateItems = dupList.Count,
					TotalDuplicateSize = dupList.Sum(d => d.SizeLong),
					Duration = scanDuration,
					Duplicates = dupList,
					Deletions = new List<DeletionRecord>(),
					HasThumbnails = saveThumbnails
				};

				var filePath = Path.Combine(_scansFolder, $"{scanId}.json");
				var json = JsonSerializer.Serialize(entry, _jsonOptions);
				File.WriteAllText(filePath, json);

				// Save thumbnails if requested
				if (saveThumbnails) {
					SaveThumbnailsPack(scanId, dupList);
				}

				lock (_lock) {
					_index.Entries.Insert(0, new HistorySummary {
						ScanId = scanId,
						Timestamp = entry.Timestamp,
						Folders = entry.Folders,
						DuplicateGroups = entry.DuplicateGroups,
						DuplicateItems = entry.DuplicateItems,
						TotalDuplicateSize = entry.TotalDuplicateSize,
						DeletedCount = 0,
						DeletedSize = 0,
						HistoryFile = filePath,
						HasThumbnails = saveThumbnails
					});
					SaveIndex();
				}

				CurrentScanId = scanId;
				Logger.Instance.Info($"Saved scan history: {scanId} ({dupList.Count} items in {groups.Count} groups){(saveThumbnails ? " with thumbnails" : "")}");
				return scanId;
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to save scan history: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Saves thumbnails to a pack file.
		/// Format: [count:4bytes] [entry1] [entry2] ...
		/// Entry: [pathLength:4bytes] [path:utf8] [dataLength:4bytes] [jpegData:bytes]
		/// </summary>
		private void SaveThumbnailsPack(string scanId, List<DuplicateItem> duplicates) {
			var thumbFile = Path.Combine(_scansFolder, $"{scanId}_thumbs.pack");
			try {
				using var fs = new FileStream(thumbFile, FileMode.Create, FileAccess.Write);
				using var writer = new BinaryWriter(fs);

				// Count items with thumbnails
				var itemsWithThumbs = duplicates.Where(d => d.ImageList != null && d.ImageList.Count > 0).ToList();
				writer.Write(itemsWithThumbs.Count);

				foreach (var item in itemsWithThumbs) {
					// Write path
					var pathBytes = System.Text.Encoding.UTF8.GetBytes(item.Path);
					writer.Write(pathBytes.Length);
					writer.Write(pathBytes);

					// Convert ImageList to single combined JPEG
					using var ms = new MemoryStream();
					if (item.ImageList.Count == 1) {
						item.ImageList[0].SaveAsJpeg(ms, new JpegEncoder { Quality = 85 });
					}
					else {
						// Join multiple thumbnails horizontally
						int height = item.ImageList[0].Height;
						int totalWidth = item.ImageList.Sum(img => img.Width);

						using var joined = new Image<Rgba32>(totalWidth, height);
						joined.Mutate(ctx => {
							int offsetX = 0;
							foreach (var img in item.ImageList) {
								ctx.DrawImage(img, new Point(offsetX, 0), 1f);
								offsetX += img.Width;
							}
						});
						joined.SaveAsJpeg(ms, new JpegEncoder { Quality = 85 });
					}

					// Write thumbnail data
					var thumbData = ms.ToArray();
					writer.Write(thumbData.Length);
					writer.Write(thumbData);
				}

				Logger.Instance.Info($"Saved {itemsWithThumbs.Count} thumbnails to {thumbFile}");
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to save thumbnails pack: {ex.Message}");
				// Delete partial file if failed
				try { if (File.Exists(thumbFile)) File.Delete(thumbFile); } catch { }
			}
		}

		/// <summary>
		/// Gets a thumbnail from the history pack file.
		/// </summary>
		/// <param name="scanId">The scan ID.</param>
		/// <param name="path">The file path to get thumbnail for.</param>
		/// <returns>JPEG bytes or null if not found.</returns>
		public byte[]? GetThumbnailFromHistory(string scanId, string path) {
			var thumbFile = Path.Combine(_scansFolder, $"{scanId}_thumbs.pack");
			if (!File.Exists(thumbFile)) return null;

			try {
				using var fs = new FileStream(thumbFile, FileMode.Open, FileAccess.Read);
				using var reader = new BinaryReader(fs);

				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					// Read path
					int pathLen = reader.ReadInt32();
					var pathBytes = reader.ReadBytes(pathLen);
					var itemPath = System.Text.Encoding.UTF8.GetString(pathBytes);

					// Read thumbnail data
					int dataLen = reader.ReadInt32();

					if (itemPath == path) {
						return reader.ReadBytes(dataLen);
					}
					else {
						// Skip this entry
						fs.Seek(dataLen, SeekOrigin.Current);
					}
				}
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to read thumbnail from history: {ex.Message}");
			}

			return null;
		}

		/// <summary>
		/// Records a deletion operation in the history.
		/// </summary>
		/// <param name="scanId">The scan ID to record against.</param>
		/// <param name="deletedItems">The items that were deleted.</param>
		public void RecordDeletion(string scanId, IEnumerable<DeletionItem> deletedItems) {
			var items = deletedItems.ToList();
			if (items.Count == 0) return;

			try {
				var summary = _index.Entries.FirstOrDefault(e => e.ScanId == scanId);
				if (summary == null || string.IsNullOrEmpty(summary.HistoryFile)) {
					Logger.Instance.Info($"Cannot record deletion, scan not found: {scanId}");
					return;
				}

				var json = File.ReadAllText(summary.HistoryFile);
				var entry = JsonSerializer.Deserialize<ScanHistoryEntry>(json, _jsonOptions);
				if (entry == null) return;

				var record = new DeletionRecord {
					Timestamp = DateTime.UtcNow,
					Items = items
				};

				entry.Deletions.Add(record);
				entry.DeletedCount += items.Count;
				entry.DeletedSize += items.Sum(i => i.FileSize);

				json = JsonSerializer.Serialize(entry, _jsonOptions);
				File.WriteAllText(summary.HistoryFile, json);

				lock (_lock) {
					summary.DeletedCount = entry.DeletedCount;
					summary.DeletedSize = entry.DeletedSize;
					SaveIndex();
				}

				Logger.Instance.Info($"Recorded {items.Count} deletions for scan {scanId}");
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to record deletion: {ex.Message}");
			}
		}

		/// <summary>
		/// Records a single deletion operation.
		/// </summary>
		public void RecordDeletion(string scanId, string path, long fileSize, DeleteAction action, string? trashId, Guid groupId) {
			RecordDeletion(scanId, new[] {
				new DeletionItem {
					Path = path,
					FileSize = fileSize,
					Action = action,
					TrashId = trashId,
					GroupId = groupId
				}
			});
		}

		/// <summary>
		/// Marks a deleted item as restored in the history.
		/// </summary>
		public void MarkRestored(string scanId, string trashId) {
			try {
				var summary = _index.Entries.FirstOrDefault(e => e.ScanId == scanId);
				if (summary == null || string.IsNullOrEmpty(summary.HistoryFile)) return;

				var json = File.ReadAllText(summary.HistoryFile);
				var entry = JsonSerializer.Deserialize<ScanHistoryEntry>(json, _jsonOptions);
				if (entry == null) return;

				bool changed = false;
				foreach (var deletion in entry.Deletions) {
					foreach (var item in deletion.Items) {
						if (item.TrashId == trashId && !item.Restored) {
							item.Restored = true;
							item.RestoredAt = DateTime.UtcNow;
							changed = true;
						}
					}
				}

				if (changed) {
					json = JsonSerializer.Serialize(entry, _jsonOptions);
					File.WriteAllText(summary.HistoryFile, json);
					Logger.Instance.Info($"Marked item as restored: {trashId} in scan {scanId}");
				}
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to mark as restored: {ex.Message}");
			}
		}

		/// <summary>
		/// Gets the list of history summaries.
		/// </summary>
		/// <returns>List of history summaries ordered by date descending.</returns>
		public List<HistorySummary> GetHistoryList() {
			return _index.Entries.OrderByDescending(e => e.Timestamp).ToList();
		}

		/// <summary>
		/// Gets detailed history for a specific scan.
		/// </summary>
		/// <param name="scanId">The scan ID.</param>
		/// <returns>The full scan history entry or null if not found.</returns>
		public ScanHistoryEntry? GetHistoryDetails(string scanId) {
			var summary = _index.Entries.FirstOrDefault(e => e.ScanId == scanId);
			if (summary == null || string.IsNullOrEmpty(summary.HistoryFile)) return null;

			try {
				var json = File.ReadAllText(summary.HistoryFile);
				return JsonSerializer.Deserialize<ScanHistoryEntry>(json, _jsonOptions);
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to read history details for {scanId}: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Deletes a history entry.
		/// </summary>
		/// <param name="scanId">The scan ID to delete.</param>
		/// <returns>True if deleted successfully.</returns>
		public bool DeleteHistory(string scanId) {
			try {
				var summary = _index.Entries.FirstOrDefault(e => e.ScanId == scanId);
				if (summary == null) return false;

				if (!string.IsNullOrEmpty(summary.HistoryFile) && File.Exists(summary.HistoryFile)) {
					File.Delete(summary.HistoryFile);
				}

				// Also delete thumbnails if they exist
				var thumbFile = Path.Combine(_scansFolder, $"{scanId}_thumbs.pack");
				if (File.Exists(thumbFile)) {
					File.Delete(thumbFile);
				}

				lock (_lock) {
					_index.Entries.Remove(summary);
					SaveIndex();
				}

				Logger.Instance.Info($"Deleted history: {scanId}");
				return true;
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to delete history: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Cleans up old history entries.
		/// </summary>
		/// <param name="days">Maximum age in days.</param>
		/// <returns>Number of entries deleted.</returns>
		public int CleanupOldHistory(int days) {
			var cutoff = DateTime.UtcNow.AddDays(-days);
			var toDelete = _index.Entries.Where(e => e.Timestamp < cutoff).ToList();
			int count = 0;

			foreach (var entry in toDelete) {
				if (DeleteHistory(entry.ScanId)) {
					count++;
				}
			}

			Logger.Instance.Info($"Cleaned up {count} old history entries (older than {days} days)");
			return count;
		}

		/// <summary>
		/// Gets history entries filtered by date range.
		/// </summary>
		public List<HistorySummary> GetHistoryByDateRange(DateTime from, DateTime to) {
			return _index.Entries
				.Where(e => e.Timestamp >= from && e.Timestamp <= to)
				.OrderByDescending(e => e.Timestamp)
				.ToList();
		}

		/// <summary>
		/// Gets history entries filtered by folder path.
		/// </summary>
		public List<HistorySummary> GetHistoryByFolder(string folderPath) {
			return _index.Entries
				.Where(e => e.Folders.Any(f => f.Contains(folderPath, StringComparison.OrdinalIgnoreCase)))
				.OrderByDescending(e => e.Timestamp)
				.ToList();
		}

		/// <summary>
		/// Gets the history folder path based on settings.
		/// </summary>
		public static string ResolveHistoryFolder(string historyFolderPath, string dataFolder = "") {
			return DataPaths.GetHistoryFolder(historyFolderPath, dataFolder);
		}
	}
}
