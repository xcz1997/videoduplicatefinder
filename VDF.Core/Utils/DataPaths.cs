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

namespace VDF.Core.Utils {
	/// <summary>
	/// Provides unified path resolution for all data storage locations.
	/// All paths can be configured individually or default to subdirectories under DataFolder.
	/// </summary>
	public static class DataPaths {
		// Default subdirectory names under DataFolder
		public const string DefaultDatabaseSubdir = "database";
		public const string DefaultCacheSubdir = "cache";
		public const string DefaultHistorySubdir = "history";
		public const string DefaultTrashSubdir = "trash";

		/// <summary>
		/// Gets the root data folder. If dataFolder is empty, returns CurrentFolder.
		/// </summary>
		/// <param name="dataFolder">The configured DataFolder path.</param>
		/// <returns>The resolved root data folder path.</returns>
		public static string GetDataFolder(string dataFolder) {
			if (string.IsNullOrWhiteSpace(dataFolder)) {
				return CoreUtils.CurrentFolder;
			}
			return Path.IsPathRooted(dataFolder)
				? dataFolder
				: Path.Combine(CoreUtils.CurrentFolder, dataFolder);
		}

		/// <summary>
		/// Resolves a path that can be:
		/// - Empty: returns DataFolder/defaultSubdir
		/// - Absolute: returns as-is
		/// - Relative: returns DataFolder/relativePath
		/// </summary>
		/// <param name="customPath">The custom path setting (can be empty).</param>
		/// <param name="dataFolder">The root DataFolder setting.</param>
		/// <param name="defaultSubdir">The default subdirectory name if customPath is empty.</param>
		/// <returns>The resolved absolute path.</returns>
		public static string ResolvePath(string customPath, string dataFolder, string defaultSubdir) {
			var rootFolder = GetDataFolder(dataFolder);

			if (string.IsNullOrWhiteSpace(customPath)) {
				return Path.Combine(rootFolder, defaultSubdir);
			}

			return Path.IsPathRooted(customPath)
				? customPath
				: Path.Combine(rootFolder, customPath);
		}

		/// <summary>
		/// Gets the database folder path.
		/// </summary>
		public static string GetDatabaseFolder(string customDatabaseFolder, string dataFolder) {
			return ResolvePath(customDatabaseFolder, dataFolder, DefaultDatabaseSubdir);
		}

		/// <summary>
		/// Gets the thumbnail cache folder path.
		/// </summary>
		public static string GetCacheFolder(string customCacheFolder, string dataFolder) {
			return ResolvePath(customCacheFolder, dataFolder, DefaultCacheSubdir);
		}

		/// <summary>
		/// Gets the history folder path.
		/// </summary>
		public static string GetHistoryFolder(string customHistoryFolder, string dataFolder) {
			return ResolvePath(customHistoryFolder, dataFolder, DefaultHistorySubdir);
		}

		/// <summary>
		/// Gets the trash folder path (for centralized trash, not per-scan-directory).
		/// </summary>
		public static string GetTrashFolder(string customTrashFolder, string dataFolder) {
			return ResolvePath(customTrashFolder, dataFolder, DefaultTrashSubdir);
		}

		/// <summary>
		/// Gets the database file path (VDF.db).
		/// </summary>
		public static string GetDatabasePath(string customDatabaseFolder, string dataFolder) {
			var folder = GetDatabaseFolder(customDatabaseFolder, dataFolder);
			return Path.Combine(folder, "VDF.db");
		}
	}
}
