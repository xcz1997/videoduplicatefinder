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

using System.Text.Json.Serialization;
using VDF.Core.ViewModels;

namespace VDF.Core.History {
	/// <summary>
	/// Represents a complete scan history entry with all results and deletion records.
	/// </summary>
	public class ScanHistoryEntry {
		/// <summary>
		/// Unique identifier for this scan.
		/// </summary>
		[JsonPropertyName("scanId")]
		public string ScanId { get; set; } = string.Empty;

		/// <summary>
		/// When the scan was performed.
		/// </summary>
		[JsonPropertyName("timestamp")]
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// Folders that were scanned.
		/// </summary>
		[JsonPropertyName("folders")]
		public List<string> Folders { get; set; } = new();

		/// <summary>
		/// Total number of files scanned.
		/// </summary>
		[JsonPropertyName("totalFiles")]
		public int TotalFiles { get; set; }

		/// <summary>
		/// Number of duplicate groups found.
		/// </summary>
		[JsonPropertyName("duplicateGroups")]
		public int DuplicateGroups { get; set; }

		/// <summary>
		/// Number of duplicate items found.
		/// </summary>
		[JsonPropertyName("duplicateItems")]
		public int DuplicateItems { get; set; }

		/// <summary>
		/// Total size of all duplicates in bytes.
		/// </summary>
		[JsonPropertyName("totalDuplicateSize")]
		public long TotalDuplicateSize { get; set; }

		/// <summary>
		/// How long the scan took.
		/// </summary>
		[JsonPropertyName("duration")]
		public TimeSpan Duration { get; set; }

		/// <summary>
		/// All duplicate items found in this scan.
		/// </summary>
		[JsonPropertyName("duplicates")]
		public List<DuplicateItem> Duplicates { get; set; } = new();

		/// <summary>
		/// Deletion records for this scan.
		/// </summary>
		[JsonPropertyName("deletions")]
		public List<DeletionRecord> Deletions { get; set; } = new();

		/// <summary>
		/// Number of files deleted from this scan.
		/// </summary>
		[JsonPropertyName("deletedCount")]
		public int DeletedCount { get; set; }

		/// <summary>
		/// Total size of deleted files in bytes.
		/// </summary>
		[JsonPropertyName("deletedSize")]
		public long DeletedSize { get; set; }

		/// <summary>
		/// Whether thumbnails are saved with this history entry.
		/// </summary>
		[JsonPropertyName("hasThumbnails")]
		public bool HasThumbnails { get; set; }
	}
}
