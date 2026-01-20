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

namespace VDF.Core.Trash {
	/// <summary>
	/// Metadata for a file that has been moved to the trash folder.
	/// Stored alongside the file to enable restoration.
	/// </summary>
	public class TrashMetadata {
		/// <summary>
		/// Unique identifier for this trash item.
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// Original full path of the file before deletion.
		/// </summary>
		[JsonPropertyName("originalPath")]
		public string OriginalPath { get; set; } = string.Empty;

		/// <summary>
		/// Original file name.
		/// </summary>
		[JsonPropertyName("fileName")]
		public string FileName { get; set; } = string.Empty;

		/// <summary>
		/// File size in bytes.
		/// </summary>
		[JsonPropertyName("fileSize")]
		public long FileSize { get; set; }

		/// <summary>
		/// When the file was moved to trash.
		/// </summary>
		[JsonPropertyName("deletedAt")]
		public DateTime DeletedAt { get; set; }

		/// <summary>
		/// ID of the scan that led to this deletion, if any.
		/// </summary>
		[JsonPropertyName("scanId")]
		public string? ScanId { get; set; }

		/// <summary>
		/// Group ID from the duplicate detection, if applicable.
		/// </summary>
		[JsonPropertyName("groupId")]
		public Guid? GroupId { get; set; }

		/// <summary>
		/// The path to the file within the trash folder.
		/// </summary>
		[JsonPropertyName("trashPath")]
		public string TrashPath { get; set; } = string.Empty;
	}
}
