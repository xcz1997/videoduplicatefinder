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
using VDF.Core.Trash;

namespace VDF.Core.History {
	/// <summary>
	/// Records a deletion operation within a scan history.
	/// </summary>
	public class DeletionRecord {
		/// <summary>
		/// When the deletion was performed.
		/// </summary>
		[JsonPropertyName("timestamp")]
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// Individual items that were deleted in this operation.
		/// </summary>
		[JsonPropertyName("items")]
		public List<DeletionItem> Items { get; set; } = new();
	}

	/// <summary>
	/// Information about a single deleted file.
	/// </summary>
	public class DeletionItem {
		/// <summary>
		/// Original path of the deleted file.
		/// </summary>
		[JsonPropertyName("path")]
		public string Path { get; set; } = string.Empty;

		/// <summary>
		/// File size in bytes.
		/// </summary>
		[JsonPropertyName("fileSize")]
		public long FileSize { get; set; }

		/// <summary>
		/// The delete action that was performed.
		/// </summary>
		[JsonPropertyName("action")]
		public DeleteAction Action { get; set; }

		/// <summary>
		/// If moved to trash, the ID in the trash folder.
		/// </summary>
		[JsonPropertyName("trashId")]
		public string? TrashId { get; set; }

		/// <summary>
		/// The duplicate group this item belonged to.
		/// </summary>
		[JsonPropertyName("groupId")]
		public Guid GroupId { get; set; }

		/// <summary>
		/// Whether this file has been restored.
		/// </summary>
		[JsonPropertyName("restored")]
		public bool Restored { get; set; }

		/// <summary>
		/// When the file was restored, if applicable.
		/// </summary>
		[JsonPropertyName("restoredAt")]
		public DateTime? RestoredAt { get; set; }
	}
}
