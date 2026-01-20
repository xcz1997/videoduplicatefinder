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
using VDF.Core.Utils;

namespace VDF.Core.Trash {
	/// <summary>
	/// Manages the trash folder for safe file deletion with recovery capability.
	/// </summary>
	public class TrashManager {
		private readonly string _trashFolder;
		private readonly string _indexPath;
		private readonly string _itemsFolder;
		private TrashIndex _index;
		private readonly object _lock = new();
		private static readonly JsonSerializerOptions _jsonOptions = new() {
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		/// <summary>
		/// Event raised when a file operation has progress to report (for large file copies).
		/// </summary>
		public event Action<long, long>? CopyProgress;

		/// <summary>
		/// Creates a new TrashManager for the specified trash folder.
		/// </summary>
		/// <param name="trashFolder">Full path to the trash folder.</param>
		public TrashManager(string trashFolder) {
			_trashFolder = trashFolder;
			_indexPath = Path.Combine(_trashFolder, "index.json");
			_itemsFolder = Path.Combine(_trashFolder, "items");
			_index = new TrashIndex();
			Initialize();
		}

		/// <summary>
		/// Gets the path to the trash folder.
		/// </summary>
		public string TrashFolder => _trashFolder;

		/// <summary>
		/// Gets the number of items in the trash.
		/// </summary>
		public int ItemCount => _index.ItemCount;

		/// <summary>
		/// Gets the total size of all items in the trash.
		/// </summary>
		public long TotalSize => _index.TotalSize;

		/// <summary>
		/// Initializes the trash folder structure and loads the index.
		/// </summary>
		private void Initialize() {
			try {
				if (!Directory.Exists(_trashFolder)) {
					Directory.CreateDirectory(_trashFolder);
				}
				if (!Directory.Exists(_itemsFolder)) {
					Directory.CreateDirectory(_itemsFolder);
				}
				LoadIndex();
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to initialize trash folder: {ex.Message}");
			}
		}

		/// <summary>
		/// Loads the trash index from disk, or rebuilds it if corrupted.
		/// </summary>
		private void LoadIndex() {
			lock (_lock) {
				if (File.Exists(_indexPath)) {
					try {
						var json = File.ReadAllText(_indexPath);
						_index = JsonSerializer.Deserialize<TrashIndex>(json, _jsonOptions) ?? new TrashIndex();
						return;
					}
					catch (Exception ex) {
						Logger.Instance.Info($"Failed to load trash index, rebuilding: {ex.Message}");
					}
				}
				RebuildIndex();
			}
		}

		/// <summary>
		/// Saves the trash index to disk.
		/// </summary>
		private void SaveIndex() {
			lock (_lock) {
				try {
					_index.LastUpdated = DateTime.UtcNow;
					_index.TotalSize = _index.Items.Sum(i => i.FileSize);
					var json = JsonSerializer.Serialize(_index, _jsonOptions);
					File.WriteAllText(_indexPath, json);
				}
				catch (Exception ex) {
					Logger.Instance.Info($"Failed to save trash index: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Rebuilds the trash index by scanning the items folder.
		/// </summary>
		public void RebuildIndex() {
			lock (_lock) {
				_index = new TrashIndex();
				if (!Directory.Exists(_itemsFolder)) {
					SaveIndex();
					return;
				}

				foreach (var itemDir in Directory.GetDirectories(_itemsFolder)) {
					var metadataPath = Path.Combine(itemDir, "metadata.json");
					if (!File.Exists(metadataPath)) continue;

					try {
						var json = File.ReadAllText(metadataPath);
						var metadata = JsonSerializer.Deserialize<TrashMetadata>(json, _jsonOptions);
						if (metadata != null) {
							_index.Items.Add(metadata);
						}
					}
					catch (Exception ex) {
						Logger.Instance.Info($"Failed to read metadata from {itemDir}: {ex.Message}");
					}
				}
				SaveIndex();
			}
		}

		/// <summary>
		/// Moves a file to the trash folder.
		/// </summary>
		/// <param name="filePath">Path to the file to move.</param>
		/// <param name="scanId">Optional scan ID for tracking.</param>
		/// <param name="groupId">Optional group ID from duplicate detection.</param>
		/// <returns>The trash item ID if successful, null otherwise.</returns>
		public string? MoveToTrash(string filePath, string? scanId = null, Guid? groupId = null) {
			if (!File.Exists(filePath)) {
				Logger.Instance.Info($"Cannot move to trash, file not found: {filePath}");
				return null;
			}

			try {
				var fileInfo = new FileInfo(filePath);
				var trashId = Guid.NewGuid().ToString("N");
				var itemFolder = Path.Combine(_itemsFolder, trashId);
				Directory.CreateDirectory(itemFolder);

				var metadata = new TrashMetadata {
					Id = trashId,
					OriginalPath = filePath,
					FileName = fileInfo.Name,
					FileSize = fileInfo.Length,
					DeletedAt = DateTime.UtcNow,
					ScanId = scanId,
					GroupId = groupId,
					TrashPath = Path.Combine(itemFolder, fileInfo.Name)
				};

				// Check if same drive (can use Move) or different drive (need Copy+Delete)
				var sourceRoot = Path.GetPathRoot(filePath);
				var destRoot = Path.GetPathRoot(_trashFolder);

				if (string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase)) {
					// Same drive - use fast move
					File.Move(filePath, metadata.TrashPath);
				}
				else {
					// Different drive - copy then delete
					CopyFileWithProgress(filePath, metadata.TrashPath, fileInfo.Length);

					// Verify the copy
					var destInfo = new FileInfo(metadata.TrashPath);
					if (destInfo.Length != fileInfo.Length) {
						throw new IOException("File copy verification failed - sizes don't match");
					}

					File.Delete(filePath);
				}

				// Save metadata
				var metadataPath = Path.Combine(itemFolder, "metadata.json");
				var json = JsonSerializer.Serialize(metadata, _jsonOptions);
				File.WriteAllText(metadataPath, json);

				lock (_lock) {
					_index.Items.Add(metadata);
					SaveIndex();
				}

				Logger.Instance.Info($"Moved to trash: {filePath} -> {trashId}");
				return trashId;
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to move to trash: {filePath}, error: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Copies a file with progress reporting for large files.
		/// </summary>
		private void CopyFileWithProgress(string source, string destination, long totalSize) {
			const int bufferSize = 1024 * 1024; // 1MB buffer
			var buffer = new byte[bufferSize];
			long totalCopied = 0;
			int bytesRead;

			using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read);
			using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write);

			while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0) {
				destStream.Write(buffer, 0, bytesRead);
				totalCopied += bytesRead;
				CopyProgress?.Invoke(totalCopied, totalSize);
			}
		}

		/// <summary>
		/// Restores a file from the trash to its original location.
		/// </summary>
		/// <param name="trashId">The trash item ID.</param>
		/// <param name="overwriteExisting">Whether to overwrite if file exists at original location.</param>
		/// <returns>True if restoration was successful.</returns>
		public bool RestoreFromTrash(string trashId, bool overwriteExisting = false) {
			var metadata = _index.Items.FirstOrDefault(i => i.Id == trashId);
			if (metadata == null) {
				Logger.Instance.Info($"Trash item not found: {trashId}");
				return false;
			}

			if (!File.Exists(metadata.TrashPath)) {
				Logger.Instance.Info($"Trashed file not found: {metadata.TrashPath}");
				return false;
			}

			try {
				// Create directory if needed
				var dir = Path.GetDirectoryName(metadata.OriginalPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
					Directory.CreateDirectory(dir);
				}

				// Check if file exists at original location
				if (File.Exists(metadata.OriginalPath)) {
					if (!overwriteExisting) {
						Logger.Instance.Info($"File already exists at original location: {metadata.OriginalPath}");
						return false;
					}
					File.Delete(metadata.OriginalPath);
				}

				// Check if same drive
				var sourceRoot = Path.GetPathRoot(metadata.TrashPath);
				var destRoot = Path.GetPathRoot(metadata.OriginalPath);

				if (string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase)) {
					File.Move(metadata.TrashPath, metadata.OriginalPath);
				}
				else {
					var fileInfo = new FileInfo(metadata.TrashPath);
					CopyFileWithProgress(metadata.TrashPath, metadata.OriginalPath, fileInfo.Length);
					File.Delete(metadata.TrashPath);
				}

				// Remove metadata and folder
				var itemFolder = Path.GetDirectoryName(metadata.TrashPath);
				if (!string.IsNullOrEmpty(itemFolder) && Directory.Exists(itemFolder)) {
					Directory.Delete(itemFolder, true);
				}

				lock (_lock) {
					_index.Items.Remove(metadata);
					SaveIndex();
				}

				Logger.Instance.Info($"Restored from trash: {trashId} -> {metadata.OriginalPath}");
				return true;
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to restore from trash: {trashId}, error: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Restores a file to a custom location instead of the original path.
		/// </summary>
		/// <param name="trashId">The trash item ID.</param>
		/// <param name="newPath">The new path to restore to.</param>
		/// <returns>True if restoration was successful.</returns>
		public bool RestoreToPath(string trashId, string newPath) {
			var metadata = _index.Items.FirstOrDefault(i => i.Id == trashId);
			if (metadata == null) {
				Logger.Instance.Info($"Trash item not found: {trashId}");
				return false;
			}

			if (!File.Exists(metadata.TrashPath)) {
				Logger.Instance.Info($"Trashed file not found: {metadata.TrashPath}");
				return false;
			}

			try {
				var dir = Path.GetDirectoryName(newPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
					Directory.CreateDirectory(dir);
				}

				var sourceRoot = Path.GetPathRoot(metadata.TrashPath);
				var destRoot = Path.GetPathRoot(newPath);

				if (string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase)) {
					File.Move(metadata.TrashPath, newPath);
				}
				else {
					var fileInfo = new FileInfo(metadata.TrashPath);
					CopyFileWithProgress(metadata.TrashPath, newPath, fileInfo.Length);
					File.Delete(metadata.TrashPath);
				}

				var itemFolder = Path.GetDirectoryName(metadata.TrashPath);
				if (!string.IsNullOrEmpty(itemFolder) && Directory.Exists(itemFolder)) {
					Directory.Delete(itemFolder, true);
				}

				lock (_lock) {
					_index.Items.Remove(metadata);
					SaveIndex();
				}

				Logger.Instance.Info($"Restored from trash to custom path: {trashId} -> {newPath}");
				return true;
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to restore to path: {trashId}, error: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Permanently deletes a file from the trash.
		/// </summary>
		/// <param name="trashId">The trash item ID.</param>
		/// <returns>True if deletion was successful.</returns>
		public bool PermanentDelete(string trashId) {
			var metadata = _index.Items.FirstOrDefault(i => i.Id == trashId);
			if (metadata == null) {
				Logger.Instance.Info($"Trash item not found: {trashId}");
				return false;
			}

			try {
				var itemFolder = Path.GetDirectoryName(metadata.TrashPath);
				if (!string.IsNullOrEmpty(itemFolder) && Directory.Exists(itemFolder)) {
					Directory.Delete(itemFolder, true);
				}

				lock (_lock) {
					_index.Items.Remove(metadata);
					SaveIndex();
				}

				Logger.Instance.Info($"Permanently deleted from trash: {trashId}");
				return true;
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to permanently delete: {trashId}, error: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Empties the entire trash folder.
		/// </summary>
		/// <returns>Number of items deleted.</returns>
		public int EmptyTrash() {
			int count = 0;
			try {
				if (Directory.Exists(_itemsFolder)) {
					foreach (var dir in Directory.GetDirectories(_itemsFolder)) {
						try {
							Directory.Delete(dir, true);
							count++;
						}
						catch (Exception ex) {
							Logger.Instance.Info($"Failed to delete trash item folder: {dir}, error: {ex.Message}");
						}
					}
				}

				lock (_lock) {
					_index.Items.Clear();
					SaveIndex();
				}

				Logger.Instance.Info($"Emptied trash: {count} items deleted");
			}
			catch (Exception ex) {
				Logger.Instance.Info($"Failed to empty trash: {ex.Message}");
			}
			return count;
		}

		/// <summary>
		/// Deletes items older than the specified number of days.
		/// </summary>
		/// <param name="days">Maximum age in days.</param>
		/// <returns>Number of items deleted.</returns>
		public int CleanupOldItems(int days) {
			var cutoff = DateTime.UtcNow.AddDays(-days);
			var toDelete = _index.Items.Where(i => i.DeletedAt < cutoff).ToList();
			int count = 0;

			foreach (var item in toDelete) {
				if (PermanentDelete(item.Id)) {
					count++;
				}
			}

			Logger.Instance.Info($"Cleaned up {count} old trash items (older than {days} days)");
			return count;
		}

		/// <summary>
		/// Gets all items in the trash.
		/// </summary>
		/// <returns>List of trash items with status information.</returns>
		public List<TrashItem> GetTrashItems() {
			var result = new List<TrashItem>();
			foreach (var metadata in _index.Items.OrderByDescending(i => i.DeletedAt)) {
				var item = new TrashItem {
					Metadata = metadata,
					FileExists = File.Exists(metadata.TrashPath),
					CanRestore = true
				};

				// Check if original directory still exists
				var dir = Path.GetDirectoryName(metadata.OriginalPath);
				if (!string.IsNullOrEmpty(dir)) {
					item.CanRestore = Directory.Exists(dir) || CanCreateDirectory(dir);
				}

				result.Add(item);
			}
			return result;
		}

		/// <summary>
		/// Gets a specific trash item by ID.
		/// </summary>
		/// <param name="trashId">The trash item ID.</param>
		/// <returns>The trash item or null if not found.</returns>
		public TrashItem? GetTrashItem(string trashId) {
			var metadata = _index.Items.FirstOrDefault(i => i.Id == trashId);
			if (metadata == null) return null;

			return new TrashItem {
				Metadata = metadata,
				FileExists = File.Exists(metadata.TrashPath),
				CanRestore = true
			};
		}

		/// <summary>
		/// Checks if a path can have a directory created.
		/// </summary>
		private static bool CanCreateDirectory(string path) {
			try {
				var root = Path.GetPathRoot(path);
				return !string.IsNullOrEmpty(root) && Directory.Exists(root);
			}
			catch {
				return false;
			}
		}

		/// <summary>
		/// Gets the trash folder path based on settings and scan folders.
		/// </summary>
		/// <param name="trashFolderPath">The configured trash folder path.</param>
		/// <param name="relativeToScan">Whether the path is relative to scan folders.</param>
		/// <param name="scanFolders">The folders being scanned.</param>
		/// <returns>The resolved trash folder path.</returns>
		public static string ResolveTrashFolder(string trashFolderPath, bool relativeToScan, IEnumerable<string> scanFolders) {
			if (Path.IsPathRooted(trashFolderPath)) {
				return trashFolderPath;
			}

			if (relativeToScan) {
				// Use first scan folder as base
				var firstFolder = scanFolders.FirstOrDefault();
				if (!string.IsNullOrEmpty(firstFolder)) {
					return Path.Combine(firstFolder, trashFolderPath);
				}
			}

			// Default to app folder
			return Path.Combine(CoreUtils.CurrentFolder, trashFolderPath);
		}
	}
}
