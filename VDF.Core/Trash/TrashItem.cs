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
	/// Represents an item in the trash folder, combining metadata with runtime information.
	/// </summary>
	public class TrashItem {
		/// <summary>
		/// The metadata stored with the trashed file.
		/// </summary>
		[JsonPropertyName("metadata")]
		public TrashMetadata Metadata { get; set; } = new();

		/// <summary>
		/// Whether the trashed file still exists on disk.
		/// </summary>
		[JsonIgnore]
		public bool FileExists { get; set; }

		/// <summary>
		/// Whether the original location is available for restoration.
		/// </summary>
		[JsonIgnore]
		public bool CanRestore { get; set; }

		/// <summary>
		/// Human-readable file size.
		/// </summary>
		[JsonIgnore]
		public string SizeDisplay => FormatFileSize(Metadata.FileSize);

		/// <summary>
		/// Time since deletion as a human-readable string.
		/// </summary>
		[JsonIgnore]
		public string DeletedAgo => FormatTimeAgo(Metadata.DeletedAt);

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

		private static string FormatTimeAgo(DateTime time) {
			var span = DateTime.UtcNow - time;
			if (span.TotalDays >= 1)
				return $"{(int)span.TotalDays} day(s) ago";
			if (span.TotalHours >= 1)
				return $"{(int)span.TotalHours} hour(s) ago";
			if (span.TotalMinutes >= 1)
				return $"{(int)span.TotalMinutes} minute(s) ago";
			return "just now";
		}
	}
}
