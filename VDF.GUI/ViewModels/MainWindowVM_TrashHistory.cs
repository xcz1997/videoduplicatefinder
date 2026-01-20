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

using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using VDF.Core.History;
using VDF.Core.Trash;
using VDF.GUI.Data;
using System.Linq;

namespace VDF.GUI.ViewModels {

	/// <summary>
	/// View model for displaying a trash item in the GUI.
	/// </summary>
	public class TrashItemVM : ReactiveObject {
		private readonly TrashItem _item;

		public TrashItemVM(TrashItem item) {
			_item = item;
		}

		public string Id => _item.Metadata.Id;
		public string FileName => _item.Metadata.FileName;
		public string OriginalPath => _item.Metadata.OriginalPath;
		public long FileSize => _item.Metadata.FileSize;
		public string SizeDisplay => _item.SizeDisplay;
		public DateTime DeletedAt => _item.Metadata.DeletedAt;
		public string DeletedAgo => _item.DeletedAgo;
		public bool FileExists => _item.FileExists;
		public bool CanRestore => _item.CanRestore;
		public string? ScanId => _item.Metadata.ScanId;
		public Guid? GroupId => _item.Metadata.GroupId;

		public TrashItem GetItem() => _item;
	}

	/// <summary>
	/// View model for displaying a history summary in the GUI.
	/// </summary>
	public class HistoryEntryVM : ReactiveObject {
		private readonly HistorySummary _entry;

		public HistoryEntryVM(HistorySummary entry) {
			_entry = entry;
		}

		public string ScanId => _entry.ScanId;
		public DateTime Timestamp => _entry.Timestamp;
		public string TimestampDisplay => _entry.Timestamp.ToString("yyyy-MM-dd HH:mm");
		public List<string> Folders => _entry.Folders;
		public string FoldersDisplay => _entry.Folders.Count > 0
			? (_entry.Folders.Count == 1 ? _entry.Folders[0] : $"{_entry.Folders.Count} folders")
			: "No folders";
		public int DuplicateGroups => _entry.DuplicateGroups;
		public int DuplicateItems => _entry.DuplicateItems;
		public long TotalDuplicateSize => _entry.TotalDuplicateSize;
		public string TotalSizeDisplay => FormatFileSize(_entry.TotalDuplicateSize);
		public int DeletedCount => _entry.DeletedCount;
		public long DeletedSize => _entry.DeletedSize;
		public string DeletedSizeDisplay => FormatFileSize(_entry.DeletedSize);

		public HistorySummary GetEntry() => _entry;

		private static string FormatFileSize(long bytes) {
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			double len = bytes;
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1) {
				order++;
				len /= 1024;
			}
			return $"{len:0.##} {sizes[order]}";
		}
	}

	// Partial class extension for MainWindowVM
	public partial class MainWindowVM {
		// Trash view properties
		private ObservableCollection<TrashItemVM> _trashItems = new();
		public ObservableCollection<TrashItemVM> TrashItems {
			get => _trashItems;
			set => this.RaiseAndSetIfChanged(ref _trashItems, value);
		}

		private TrashItemVM? _selectedTrashItem;
		public TrashItemVM? SelectedTrashItem {
			get => _selectedTrashItem;
			set {
				this.RaiseAndSetIfChanged(ref _selectedTrashItem, value);
				this.RaisePropertyChanged(nameof(HasSelectedTrashItems));
			}
		}

		public bool HasSelectedTrashItems => SelectedTrashItem != null;

		private int _trashItemCount;
		public int TrashItemCount {
			get => _trashItemCount;
			set => this.RaiseAndSetIfChanged(ref _trashItemCount, value);
		}

		private string _trashTotalSize = "0 B";
		public string TrashTotalSize {
			get => _trashTotalSize;
			set => this.RaiseAndSetIfChanged(ref _trashTotalSize, value);
		}

		// History view properties
		private ObservableCollection<HistoryEntryVM> _historyEntries = new();
		public ObservableCollection<HistoryEntryVM> HistoryEntries {
			get => _historyEntries;
			set => this.RaiseAndSetIfChanged(ref _historyEntries, value);
		}

		private HistoryEntryVM? _selectedHistoryEntry;
		public HistoryEntryVM? SelectedHistoryEntry {
			get => _selectedHistoryEntry;
			set {
				this.RaiseAndSetIfChanged(ref _selectedHistoryEntry, value);
				this.RaisePropertyChanged(nameof(HasSelectedHistoryEntry));
			}
		}

		public bool HasSelectedHistoryEntry => SelectedHistoryEntry != null;

		private int _historyEntryCount;
		public int HistoryEntryCount {
			get => _historyEntryCount;
			set => this.RaiseAndSetIfChanged(ref _historyEntryCount, value);
		}

		// Trash Commands
		public ReactiveCommand<Unit, Unit> RefreshTrashCommand => ReactiveCommand.Create(() => {
			RefreshTrashItems();
		});

		public ReactiveCommand<Unit, Unit> RestoreSelectedTrashCommand => ReactiveCommand.Create(() => {
			if (SelectedTrashItem == null) return;

			try {
				var trashManager = GetTrashManager();
				var result = trashManager.RestoreFromTrash(SelectedTrashItem.Id);
				if (result) {
					LogItems.Insert(0, $"Restored: {SelectedTrashItem.OriginalPath}");
					RefreshTrashItems();
				} else {
					LogItems.Insert(0, $"Failed to restore {SelectedTrashItem.FileName}");
				}
			} catch (Exception ex) {
				LogItems.Insert(0, $"Error restoring file: {ex.Message}");
			}
		});

		public ReactiveCommand<Unit, Unit> DeleteSelectedTrashPermanentlyCommand => ReactiveCommand.Create(() => {
			if (SelectedTrashItem == null) return;

			try {
				var trashManager = GetTrashManager();
				var result = trashManager.PermanentDelete(SelectedTrashItem.Id);
				if (result) {
					LogItems.Insert(0, $"Permanently deleted: {SelectedTrashItem.FileName}");
					RefreshTrashItems();
				} else {
					LogItems.Insert(0, $"Failed to delete {SelectedTrashItem.FileName}");
				}
			} catch (Exception ex) {
				LogItems.Insert(0, $"Error deleting file: {ex.Message}");
			}
		});

		public ReactiveCommand<Unit, Unit> EmptyTrashCommand => ReactiveCommand.Create(() => {
			try {
				var trashManager = GetTrashManager();
				var deletedCount = trashManager.EmptyTrash();
				LogItems.Insert(0, $"Emptied trash: {deletedCount} files deleted");
				RefreshTrashItems();
			} catch (Exception ex) {
				LogItems.Insert(0, $"Error emptying trash: {ex.Message}");
			}
		});

		// History Commands
		public ReactiveCommand<Unit, Unit> RefreshHistoryCommand => ReactiveCommand.Create(() => {
			RefreshHistoryEntries();
		});

		public ReactiveCommand<Unit, Unit> ViewHistoryDetailsCommand => ReactiveCommand.Create(() => {
			if (SelectedHistoryEntry == null) return;
			// TODO: Open history details dialog
			LogItems.Insert(0, $"View details for scan: {SelectedHistoryEntry.ScanId}");
		});

		public ReactiveCommand<Unit, Unit> DeleteSelectedHistoryCommand => ReactiveCommand.Create(() => {
			if (SelectedHistoryEntry == null) return;

			try {
				var historyManager = GetHistoryManager();
				historyManager.DeleteHistory(SelectedHistoryEntry.ScanId);
				LogItems.Insert(0, $"Deleted history entry: {SelectedHistoryEntry.ScanId}");
				RefreshHistoryEntries();
			} catch (Exception ex) {
				LogItems.Insert(0, $"Error deleting history: {ex.Message}");
			}
		});

		public ReactiveCommand<Unit, Unit> CleanupHistoryCommand => ReactiveCommand.Create(() => {
			try {
				var historyManager = GetHistoryManager();
				var deletedCount = historyManager.CleanupOldHistory(SettingsFile.Instance.MaxHistoryDays);
				LogItems.Insert(0, $"Cleaned up {deletedCount} old history entries");
				RefreshHistoryEntries();
			} catch (Exception ex) {
				LogItems.Insert(0, $"Error cleaning up history: {ex.Message}");
			}
		});

		// Helper methods
		private void RefreshTrashItems() {
			try {
				var trashManager = GetTrashManager();
				var items = trashManager.GetTrashItems();

				TrashItems.Clear();
				long totalSize = 0;
				foreach (var item in items) {
					TrashItems.Add(new TrashItemVM(item));
					totalSize += item.Metadata.FileSize;
				}

				TrashItemCount = TrashItems.Count;
				TrashTotalSize = FormatFileSizeStatic(totalSize);
			} catch (Exception ex) {
				LogItems.Insert(0, $"Error refreshing trash: {ex.Message}");
			}
		}

		private void RefreshHistoryEntries() {
			try {
				var historyManager = GetHistoryManager();
				var entries = historyManager.GetHistoryList();

				HistoryEntries.Clear();
				// Sort entries by timestamp descending
				var sortedEntries = entries.OrderByDescending(e => e.Timestamp);
				foreach (var entry in sortedEntries) {
					HistoryEntries.Add(new HistoryEntryVM(entry));
				}

				HistoryEntryCount = HistoryEntries.Count;
			} catch (Exception ex) {
				LogItems.Insert(0, $"Error refreshing history: {ex.Message}");
			}
		}

		private static string FormatFileSizeStatic(long bytes) {
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			double len = bytes;
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1) {
				order++;
				len /= 1024;
			}
			return $"{len:0.##} {sizes[order]}";
		}
	}
}
