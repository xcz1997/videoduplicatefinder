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
//

using System.Linq;

namespace VDF.GUI.Data {
	/// <summary>
	/// Defines file exclusion templates for various media server platforms.
	/// These templates help exclude metadata and artwork files that are associated
	/// with video files but should not be scanned as duplicates.
	/// </summary>
	public static class MediaServerTemplates {
		/// <summary>
		/// Template definition containing name key and exclusion patterns
		/// </summary>
		public class Template {
			public string Id { get; init; } = string.Empty;
			public string NameKey { get; init; } = string.Empty;
			public string DescriptionKey { get; init; } = string.Empty;
			public string[] Patterns { get; init; } = [];
		}

		/// <summary>
		/// All available media server templates
		/// </summary>
		public static readonly Template[] AllTemplates = [
			new Template {
				Id = "jellyfin",
				NameKey = "Settings.Template.Jellyfin",
				DescriptionKey = "Settings.Template.Jellyfin.Desc",
				Patterns = [
					// NFO metadata files
					"*.nfo",
					// Poster/Cover images
					"poster.*", "*-poster.*", "cover.*", "folder.*", "default.*", "movie.*",
					// Backdrop/Fanart images
					"backdrop.*", "backdrop?.*", "backdrop-?.*", "fanart.*", "background.*", "art.*",
					// Banner images
					"banner.*", "*-banner.*",
					// Logo images
					"logo.*", "clearlogo.*", "*-logo.*", "*-clearlogo.*",
					// Thumbnail/Landscape images
					"thumb.*", "*-thumb.*", "landscape.*", "*-landscape.*",
					// Clearart images
					"clearart.*", "*-clearart.*",
					// Disc art
					"disc.*", "cdart.*", "*-disc.*", "*-cdart.*",
					// Season images (TV Shows)
					"season??.*", "season??-*.*", "season-specials*.*",
					// Theme music/video
					"theme.*", "theme-music/*",
					// Extra fanart folder
					"extrafanart/*"
				]
			},
			new Template {
				Id = "emby",
				NameKey = "Settings.Template.Emby",
				DescriptionKey = "Settings.Template.Emby.Desc",
				Patterns = [
					// NFO metadata files (Kodi compatible)
					"*.nfo",
					// Primary/Poster images
					"poster.*", "*-poster.*", "*-cover.*", "folder.*",
					// Backdrop images (numbered)
					"backdrop.*", "backdrop?.*", "fanart.*", "background.*", "art.*",
					// Clearart
					"clearart.*", "*-clearart.*",
					// Banner
					"banner.*", "*-banner.*",
					// Logo
					"logo.*", "clearlogo.*", "*-logo.*", "*-clearlogo.*",
					// Disc art
					"disc.*", "cdart.*", "*-disc.*", "*-cdart.*",
					// Thumbnail
					"thumb.*", "*-thumb.*", "landscape.*", "*-landscape.*",
					// Season images
					"season??-poster.*", "season??-fanart.*", "season??-banner.*", "season??-landscape.*",
					"season-specials-poster.*", "season-specials-fanart.*", "season-specials-banner.*"
				]
			},
			new Template {
				Id = "plex",
				NameKey = "Settings.Template.Plex",
				DescriptionKey = "Settings.Template.Plex.Desc",
				Patterns = [
					// Poster images
					"poster.*", "poster-?.*", "cover.*", "default.*", "folder.*", "movie.*",
					// Background/Fanart
					"*-fanart.*", "art.*", "backdrop.*", "background.*", "fanart.*",
					// Logo
					"logo.*", "clearlogo.*", "logo-?.*", "clearlogo-?.*",
					// TV Show specific
					"show.*", "show-?.*",
					// Season posters (alphabetic suffix for multiple)
					"season??.*", "season???.*",
					// Square art (mobile apps)
					"square.*", "squareArt.*", "backgroundSquare.*"
				]
			},
			new Template {
				Id = "kodi",
				NameKey = "Settings.Template.Kodi",
				DescriptionKey = "Settings.Template.Kodi.Desc",
				Patterns = [
					// NFO files
					"*.nfo",
					// Poster
					"poster.*", "*-poster.*", "folder.*",
					// Fanart
					"fanart.*", "*-fanart.*",
					// Banner
					"banner.*", "*-banner.*",
					// Clearart & Clearlogo
					"clearart.*", "*-clearart.*", "clearlogo.*", "*-clearlogo.*",
					// Landscape
					"landscape.*", "*-landscape.*",
					// Disc art
					"disc.*", "cdart.*",
					// Thumb
					"thumb.*", "*-thumb.*",
					// Extrafanart folder
					"extrafanart/*",
					// Extrathumb folder
					"extrathumbs/*"
				]
			},
			new Template {
				Id = "subtitles",
				NameKey = "Settings.Template.Subtitles",
				DescriptionKey = "Settings.Template.Subtitles.Desc",
				Patterns = [
					// Common subtitle formats
					"*.srt", "*.sub", "*.ass", "*.ssa", "*.idx", "*.vtt",
					// PGS/SUP subtitles
					"*.sup",
					// DVD subtitles
					"*.sub", "*.idx",
					// Lyrics
					"*.lrc"
				]
			},
			new Template {
				Id = "trailers",
				NameKey = "Settings.Template.Trailers",
				DescriptionKey = "Settings.Template.Trailers.Desc",
				Patterns = [
					// Common trailer naming conventions
					"*-trailer.*", "*-trailer?.*",
					"*-Trailer.*", "*-Trailer?.*",
					"trailer.*", "trailer?.*",
					// Trailers folder
					"trailers/*",
					// Featurettes and extras
					"*-featurette.*", "*-featurette?.*",
					"*-behindthescenes.*", "*-deleted.*", "*-interview.*",
					"*-scene.*", "*-short.*", "*-other.*"
				]
			}
		];

		/// <summary>
		/// Gets unique patterns from selected templates (removes duplicates)
		/// </summary>
		public static IEnumerable<string> GetPatternsForTemplates(IEnumerable<string> templateIds) {
			var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var templateId in templateIds) {
				var template = AllTemplates.FirstOrDefault(t => t.Id == templateId);
				if (template != null) {
					foreach (var pattern in template.Patterns) {
						patterns.Add(pattern);
					}
				}
			}
			return patterns;
		}

		/// <summary>
		/// Gets a template by its ID
		/// </summary>
		public static Template? GetTemplate(string id) =>
			AllTemplates.FirstOrDefault(t => t.Id == id);
	}
}
