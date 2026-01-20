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

namespace VDF.Core.History {
	/// <summary>
	/// Index file for scan history, providing quick access to history summaries.
	/// </summary>
	public class HistoryIndex {
		/// <summary>
		/// Version of the index format.
		/// </summary>
		[JsonPropertyName("version")]
		public int Version { get; set; } = 1;

		/// <summary>
		/// When the index was last updated.
		/// </summary>
		[JsonPropertyName("lastUpdated")]
		public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Summary entries for all scan histories.
		/// </summary>
		[JsonPropertyName("entries")]
		public List<HistorySummary> Entries { get; set; } = new();
	}

	/// <summary>
	/// Summary information for a single scan history entry.
	/// </summary>
	public class HistorySummary {
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
		/// Path to the detailed history file.
		/// </summary>
		[JsonPropertyName("historyFile")]
		public string HistoryFile { get; set; } = string.Empty;

		/// <summary>
		/// Whether thumbnails are saved with this history entry.
		/// </summary>
		[JsonPropertyName("hasThumbnails")]
		public bool HasThumbnails { get; set; }
	}
}
