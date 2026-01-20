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
	/// Index file for the trash folder, storing references to all trashed items.
	/// </summary>
	public class TrashIndex {
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
		/// List of all items in the trash.
		/// </summary>
		[JsonPropertyName("items")]
		public List<TrashMetadata> Items { get; set; } = new();

		/// <summary>
		/// Total size of all trashed files in bytes.
		/// </summary>
		[JsonPropertyName("totalSize")]
		public long TotalSize { get; set; }

		/// <summary>
		/// Total number of items in the trash.
		/// </summary>
		[JsonPropertyName("itemCount")]
		public int ItemCount => Items.Count;
	}
}
