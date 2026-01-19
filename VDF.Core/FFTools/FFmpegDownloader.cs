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

using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using VDF.Core.Utils;

namespace VDF.Core.FFTools;

/// <summary>
/// Service for downloading and installing FFmpeg binaries for different platforms
/// </summary>
public static class FFmpegDownloader {
	/// <summary>
	/// FFmpeg download source information
	/// </summary>
	public readonly record struct FFmpegSource(
		string Url,
		string Description,
		FFmpegSourceType SourceType
	);

	public enum FFmpegSourceType {
		DirectDownload,
		GitHubRelease,
		PackageManager,
		Manual
	}

	public enum Platform {
		Windows,
		MacOS,
		Linux,
		Unknown
	}

	public enum Architecture {
		X64,
		X86,
		Arm64,
		Arm,
		Unknown
	}

	/// <summary>
	/// Get the current platform
	/// </summary>
	public static Platform GetCurrentPlatform() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return Platform.Windows;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return Platform.MacOS;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return Platform.Linux;
		return Platform.Unknown;
	}

	/// <summary>
	/// Get the current process architecture
	/// </summary>
	public static Architecture GetCurrentArchitecture() {
		return RuntimeInformation.ProcessArchitecture switch {
			System.Runtime.InteropServices.Architecture.X64 => Architecture.X64,
			System.Runtime.InteropServices.Architecture.X86 => Architecture.X86,
			System.Runtime.InteropServices.Architecture.Arm64 => Architecture.Arm64,
			System.Runtime.InteropServices.Architecture.Arm => Architecture.Arm,
			_ => Architecture.Unknown
		};
	}

	/// <summary>
	/// Get FFmpeg download sources for the current platform
	/// </summary>
	public static List<FFmpegSource> GetDownloadSources() {
		var platform = GetCurrentPlatform();
		var arch = GetCurrentArchitecture();

		return platform switch {
			Platform.Windows => GetWindowsSources(arch),
			Platform.MacOS => GetMacOSSources(arch),
			Platform.Linux => GetLinuxSources(arch),
			_ => []
		};
	}

	static List<FFmpegSource> GetWindowsSources(Architecture arch) {
		var sources = new List<FFmpegSource>();

		// BtbN GitHub releases - most popular and reliable
		string archStr = arch switch {
			Architecture.X64 => "win64",
			Architecture.X86 => "win32",
			Architecture.Arm64 => "win64", // Use x64 for ARM64 Windows (emulation)
			_ => "win64"
		};

		sources.Add(new FFmpegSource(
			$"https://github.com/BtbN/FFmpeg-Builds/releases/latest",
			$"BtbN FFmpeg Builds (Recommended) - Download ffmpeg-master-latest-{archStr}-gpl-shared.zip",
			FFmpegSourceType.GitHubRelease
		));

		// Gyan.dev - Alternative source
		sources.Add(new FFmpegSource(
			"https://www.gyan.dev/ffmpeg/builds/",
			"Gyan.dev FFmpeg Builds - Download ffmpeg-release-full-shared.7z",
			FFmpegSourceType.DirectDownload
		));

		// Official FFmpeg download page
		sources.Add(new FFmpegSource(
			"https://ffmpeg.org/download.html#build-windows",
			"Official FFmpeg Download Page",
			FFmpegSourceType.Manual
		));

		return sources;
	}

	static List<FFmpegSource> GetMacOSSources(Architecture arch) {
		var sources = new List<FFmpegSource>();

		// Homebrew - most common on macOS
		sources.Add(new FFmpegSource(
			"brew install ffmpeg",
			"Homebrew (Recommended) - Run: brew install ffmpeg",
			FFmpegSourceType.PackageManager
		));

		// MacPorts
		sources.Add(new FFmpegSource(
			"sudo port install ffmpeg",
			"MacPorts - Run: sudo port install ffmpeg",
			FFmpegSourceType.PackageManager
		));

		// evermeet.cx - Pre-built static binaries
		string archStr = arch == Architecture.Arm64 ? "arm64" : "x86_64";
		sources.Add(new FFmpegSource(
			$"https://evermeet.cx/ffmpeg/",
			$"evermeet.cx - Pre-built FFmpeg binaries for macOS ({archStr})",
			FFmpegSourceType.DirectDownload
		));

		// Official FFmpeg download page
		sources.Add(new FFmpegSource(
			"https://ffmpeg.org/download.html#build-mac",
			"Official FFmpeg Download Page",
			FFmpegSourceType.Manual
		));

		return sources;
	}

	static List<FFmpegSource> GetLinuxSources(Architecture arch) {
		var sources = new List<FFmpegSource>();

		// Package managers - distribution specific
		sources.Add(new FFmpegSource(
			"sudo apt install ffmpeg",
			"Debian/Ubuntu - Run: sudo apt install ffmpeg",
			FFmpegSourceType.PackageManager
		));

		sources.Add(new FFmpegSource(
			"sudo dnf install ffmpeg",
			"Fedora - Run: sudo dnf install ffmpeg",
			FFmpegSourceType.PackageManager
		));

		sources.Add(new FFmpegSource(
			"sudo pacman -S ffmpeg",
			"Arch Linux - Run: sudo pacman -S ffmpeg",
			FFmpegSourceType.PackageManager
		));

		// Snap package - universal
		sources.Add(new FFmpegSource(
			"sudo snap install ffmpeg",
			"Snap (Universal) - Run: sudo snap install ffmpeg",
			FFmpegSourceType.PackageManager
		));

		// Flatpak
		sources.Add(new FFmpegSource(
			"flatpak install flathub org.freedesktop.Platform.ffmpeg-full",
			"Flatpak - Run: flatpak install flathub org.freedesktop.Platform.ffmpeg-full",
			FFmpegSourceType.PackageManager
		));

		// BtbN builds for Linux
		string archStr = arch switch {
			Architecture.X64 => "linux64",
			Architecture.Arm64 => "linuxarm64",
			_ => "linux64"
		};

		sources.Add(new FFmpegSource(
			$"https://github.com/BtbN/FFmpeg-Builds/releases/latest",
			$"BtbN FFmpeg Builds - Download ffmpeg-master-latest-{archStr}-gpl-shared.tar.xz",
			FFmpegSourceType.GitHubRelease
		));

		// Official FFmpeg download page
		sources.Add(new FFmpegSource(
			"https://ffmpeg.org/download.html#build-linux",
			"Official FFmpeg Download Page",
			FFmpegSourceType.Manual
		));

		return sources;
	}

	/// <summary>
	/// Get installation instructions for the current platform
	/// </summary>
	public static string GetInstallationInstructions() {
		var platform = GetCurrentPlatform();
		var arch = GetCurrentArchitecture();
		var binPath = Path.Combine(CoreUtils.CurrentFolder, "bin");

		return platform switch {
			Platform.Windows => GetWindowsInstructions(arch, binPath),
			Platform.MacOS => GetMacOSInstructions(arch),
			Platform.Linux => GetLinuxInstructions(arch),
			_ => "Unknown platform. Please visit https://ffmpeg.org/download.html for download options."
		};
	}

	static string GetWindowsInstructions(Architecture arch, string binPath) {
		string archStr = arch switch {
			Architecture.X64 => "win64",
			Architecture.X86 => "win32",
			Architecture.Arm64 => "win64 (ARM64 uses x64 emulation)",
			_ => "win64"
		};

		return $@"
=== FFmpeg Installation for Windows ({archStr}) ===

Option 1: Automatic (Recommended)
---------------------------------
1. Download from: https://github.com/BtbN/FFmpeg-Builds/releases/latest
2. Choose: ffmpeg-master-latest-{archStr}-gpl-shared.zip
3. Extract the following files to: {binPath}
   - ffmpeg.exe
   - ffprobe.exe
   - avcodec-*.dll
   - avformat-*.dll
   - avutil-*.dll
   - swresample-*.dll
   - swscale-*.dll

Option 2: Using Winget
----------------------
Open PowerShell and run:
  winget install FFmpeg.FFmpeg

Option 3: Using Chocolatey
--------------------------
Open PowerShell as Administrator and run:
  choco install ffmpeg-shared

Note: After installation, restart the application.
";
	}

	static string GetMacOSInstructions(Architecture arch) {
		string archNote = arch == Architecture.Arm64
			? "Apple Silicon (M1/M2/M3)"
			: "Intel Mac";

		return $@"
=== FFmpeg Installation for macOS ({archNote}) ===

Option 1: Homebrew (Recommended)
--------------------------------
Open Terminal and run:
  brew install ffmpeg

If Homebrew is not installed, first run:
  /bin/bash -c ""$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)""

Option 2: MacPorts
------------------
Open Terminal and run:
  sudo port install ffmpeg

Option 3: Manual Download
-------------------------
1. Visit: https://evermeet.cx/ffmpeg/
2. Download the latest ffmpeg and ffprobe binaries
3. Move them to /usr/local/bin/ or the application's bin folder

Note: After installation, restart the application.
";
	}

	static string GetLinuxInstructions(Architecture arch) {
		string archStr = arch switch {
			Architecture.X64 => "x86_64",
			Architecture.Arm64 => "aarch64/arm64",
			_ => "x86_64"
		};

		return $@"
=== FFmpeg Installation for Linux ({archStr}) ===

Choose the appropriate command for your distribution:

Debian/Ubuntu:
  sudo apt update && sudo apt install ffmpeg

Fedora:
  sudo dnf install ffmpeg

CentOS/RHEL (with EPEL and RPM Fusion):
  sudo dnf install epel-release
  sudo dnf install --nogpgcheck https://mirrors.rpmfusion.org/free/el/rpmfusion-free-release-$(rpm -E %rhel).noarch.rpm
  sudo dnf install ffmpeg

Arch Linux:
  sudo pacman -S ffmpeg

openSUSE:
  sudo zypper install ffmpeg

Snap (Universal):
  sudo snap install ffmpeg

AppImage/Portable:
  Download from https://github.com/BtbN/FFmpeg-Builds/releases/latest
  Extract and add to PATH or application's bin folder

Note: After installation, restart the application.
";
	}

	/// <summary>
	/// Check if FFmpeg is available on the system
	/// </summary>
	public static bool IsFFmpegAvailable() {
		return FFToolsUtils.GetPath(FFToolsUtils.FFTool.FFmpeg) != null;
	}

	/// <summary>
	/// Check if FFprobe is available on the system
	/// </summary>
	public static bool IsFFprobeAvailable() {
		return FFToolsUtils.GetPath(FFToolsUtils.FFTool.FFProbe) != null;
	}

	/// <summary>
	/// Download FFmpeg for Windows from BtbN builds
	/// </summary>
	/// <param name="progress">Progress callback (0-100)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if successful</returns>
	public static async Task<(bool Success, string Message)> DownloadFFmpegForWindows(
		IProgress<int>? progress = null,
		CancellationToken cancellationToken = default) {

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			return (false, "This download method is only available on Windows. Please use package managers on other platforms.");
		}

		var arch = GetCurrentArchitecture();
		string archStr = arch switch {
			Architecture.X64 => "win64",
			Architecture.X86 => "win32",
			_ => "win64"
		};

		// BtbN latest release URL pattern
		string downloadUrl = $"https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-{archStr}-gpl-shared.zip";
		string binPath = Path.Combine(CoreUtils.CurrentFolder, "bin");

		try {
			// Create bin directory if not exists
			Directory.CreateDirectory(binPath);

			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromMinutes(10);

			progress?.Report(5);

			// Download the file
			var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();

			var totalBytes = response.Content.Headers.ContentLength ?? -1L;
			var tempFile = Path.GetTempFileName();

			await using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
			await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true)) {
				var buffer = new byte[8192];
				long totalRead = 0;
				int bytesRead;

				while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0) {
					await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
					totalRead += bytesRead;

					if (totalBytes > 0) {
						int percentComplete = (int)((totalRead * 80 / totalBytes) + 10); // 10-90% for download
						progress?.Report(Math.Min(percentComplete, 90));
					}
				}
			}

			progress?.Report(92);

			// Extract the zip file
			using (var archive = ZipFile.OpenRead(tempFile)) {
				var rootFolder = archive.Entries.FirstOrDefault()?.FullName.Split('/')[0];

				foreach (var entry in archive.Entries) {
					// Extract only necessary files from bin folder
					if (entry.FullName.Contains("/bin/") && !string.IsNullOrEmpty(entry.Name)) {
						string destPath = Path.Combine(binPath, entry.Name);
						entry.ExtractToFile(destPath, overwrite: true);
					}
					// Also extract DLLs from the main folder (shared libraries)
					else if ((entry.Name.EndsWith(".dll") || entry.Name.EndsWith(".exe")) &&
					         !string.IsNullOrEmpty(entry.Name)) {
						string destPath = Path.Combine(binPath, entry.Name);
						entry.ExtractToFile(destPath, overwrite: true);
					}
				}
			}

			progress?.Report(98);

			// Cleanup
			File.Delete(tempFile);

			progress?.Report(100);

			// Verify installation
			if (IsFFmpegAvailable() && IsFFprobeAvailable()) {
				return (true, $"FFmpeg successfully installed to: {binPath}");
			}

			return (false, "FFmpeg was downloaded but could not be verified. Please check the bin folder manually.");
		}
		catch (HttpRequestException ex) {
			return (false, $"Download failed: {ex.Message}. Please try manual installation.");
		}
		catch (Exception ex) {
			return (false, $"Installation failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Get a user-friendly status message about FFmpeg availability
	/// </summary>
	public static string GetFFmpegStatus() {
		var ffmpegPath = FFToolsUtils.GetPath(FFToolsUtils.FFTool.FFmpeg);
		var ffprobePath = FFToolsUtils.GetPath(FFToolsUtils.FFTool.FFProbe);

		if (ffmpegPath != null && ffprobePath != null) {
			return $"FFmpeg: {ffmpegPath}\nFFprobe: {ffprobePath}";
		}

		if (ffmpegPath == null && ffprobePath == null) {
			return "FFmpeg and FFprobe are not found. Please install FFmpeg.";
		}

		if (ffmpegPath == null) {
			return $"FFmpeg not found. FFprobe: {ffprobePath}";
		}

		return $"FFprobe not found. FFmpeg: {ffmpegPath}";
	}
}
