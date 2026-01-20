using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VDF.Core;
using VDF.Core.History;
using VDF.Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace VDF.Web.Server.Services {
    public class ScanService {
        public ScanEngine Engine { get; }
        
        // Scan phase enum
        public enum ScanPhase {
            Idle,
            EnumeratingFiles,
            BuildingHashes,
            Comparing,
            RetrievingThumbnails,
            Finished
        }

        // Recent file entry for display
        public class RecentFileEntry {
            public string Path { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        // Simple DTO for frontend status
        public class ScanStatus {
            public bool IsScanning { get; set; }
            public string CurrentActivity { get; set; } = "Idle";
            public float Progress { get; set; }
            public int ProcessedFiles { get; set; }
            public int TotalFiles { get; set; }
            public TimeSpan Elapsed { get; set; }
            public TimeSpan Remaining { get; set; }
            public int DuplicatesFound { get; set; }
            public ScanPhase Phase { get; set; } = ScanPhase.Idle;
            public string PhaseDescription { get; set; } = "Idle";
        }

        private ScanStatus _currentStatus = new ScanStatus();

        // Ring buffer for recent files (max 100 entries)
        private const int MaxRecentFiles = 100;
        private readonly Queue<RecentFileEntry> _recentFiles = new();
        private readonly object _recentFilesLock = new();

        // History manager for tracking scan history
        private ScanHistoryManager? _historyManager;
        private DateTime _scanStartTime;
        private int _totalFilesScanned;
        private List<string> _scanFolders = new();

        /// <summary>
        /// Gets the ScanHistoryManager instance.
        /// </summary>
        public ScanHistoryManager GetHistoryManager() {
            if (_historyManager == null) {
                var historyFolder = ScanHistoryManager.ResolveHistoryFolder(
                    VDF.GUI.Data.SettingsFile.Instance.HistoryFolderPath,
                    VDF.GUI.Data.SettingsFile.Instance.DataFolder);
                _historyManager = new ScanHistoryManager(historyFolder);
            }
            return _historyManager;
        }

        /// <summary>
        /// Gets the current scan ID (from history manager).
        /// </summary>
        public string? CurrentScanId => _historyManager?.CurrentScanId;

        public ScanService() {
            Engine = new ScanEngine();
            // Settings are loaded in Program.cs, just apply them to the engine
            ApplySettings(VDF.GUI.Data.SettingsFile.Instance);

            Engine.Progress += Engine_Progress;
            Engine.FilesEnumerated += Engine_FilesEnumerated;
            Engine.BuildingHashesDone += Engine_BuildingHashesDone;
            Engine.ScanDone += Engine_ScanDone;
            Engine.ScanAborted += Engine_ScanAborted;
            Engine.ThumbnailsRetrieved += Engine_ThumbnailsRetrieved;
        }

        public VDF.GUI.Data.SettingsFile GetSettings() {
            return VDF.GUI.Data.SettingsFile.Instance;
        }

        public void SaveSettings(VDF.GUI.Data.SettingsFile newSettings) {
            // Copy properties from DTO to Singleton Instance
            var s = VDF.GUI.Data.SettingsFile.Instance;

            // Basic settings
            s.IncludeSubDirectories = newSettings.IncludeSubDirectories;
            s.IncludeImages = newSettings.IncludeImages;
            s.Percent = newSettings.Percent;
            s.PercentDurationDifference = newSettings.PercentDurationDifference;
            s.MaxDegreeOfParallelism = newSettings.MaxDegreeOfParallelism;
            s.Thumbnails = newSettings.Thumbnails;
            s.IgnoreReadOnlyFolders = newSettings.IgnoreReadOnlyFolders;
            s.UsePHash = newSettings.UsePHash;
            s.IncludeNonExistingFiles = newSettings.IncludeNonExistingFiles;

            // Misc settings
            s.GeneratePreviewThumbnails = newSettings.GeneratePreviewThumbnails;
            s.IgnoreReparsePoints = newSettings.IgnoreReparsePoints;
            s.ExcludeHardLinks = newSettings.ExcludeHardLinks;
            s.ScanAgainstEntireDatabase = newSettings.ScanAgainstEntireDatabase;

            // Advanced settings
            s.UseExifCreationDate = newSettings.UseExifCreationDate;
            s.IgnoreBlackPixels = newSettings.IgnoreBlackPixels;
            s.IgnoreWhitePixels = newSettings.IgnoreWhitePixels;
            s.CompareHorizontallyFlipped = newSettings.CompareHorizontallyFlipped;
            s.UseNativeFfmpegBinding = newSettings.UseNativeFfmpegBinding;
            s.ExtendedFFToolsLogging = newSettings.ExtendedFFToolsLogging;
            s.AlwaysRetryFailedSampling = newSettings.AlwaysRetryFailedSampling;
            s.BackupAfterListChanged = newSettings.BackupAfterListChanged;
            s.AskToSaveResultsOnExit = newSettings.AskToSaveResultsOnExit;

            // Performance settings
            s.HardwareAccelerationMode = newSettings.HardwareAccelerationMode;

            // Custom settings
            s.CustomFFArguments = newSettings.CustomFFArguments ?? string.Empty;
            s.DataFolder = newSettings.DataFolder ?? string.Empty;
            s.CustomDatabaseFolder = newSettings.CustomDatabaseFolder ?? string.Empty;
            var oldCacheFolder = s.ThumbnailCacheFolder;
            s.ThumbnailCacheFolder = newSettings.ThumbnailCacheFolder ?? string.Empty;

            // Re-initialize thumbnail cache if folder changed
            if (oldCacheFolder != s.ThumbnailCacheFolder) {
                try {
                    VDF.GUI.Utils.ThumbCacheHelpers.Provider?.Dispose();
                    VDF.GUI.Utils.ThumbCacheHelpers.Provider = VDF.GUI.Utils.ThumbCacheHelpers.OpenPersistentCache();
                }
                catch { VDF.GUI.Utils.ThumbCacheHelpers.Provider = null; }
            }

            // Folder lists
            s.Includes.Clear();
            if (newSettings.Includes != null) {
                foreach (var item in newSettings.Includes) s.Includes.Add(item);
            }
            s.Blacklists.Clear();
            if (newSettings.Blacklists != null) {
                foreach (var item in newSettings.Blacklists) s.Blacklists.Add(item);
            }

            // Media server templates
            s.SelectedMediaTemplates.Clear();
            if (newSettings.SelectedMediaTemplates != null) {
                foreach (var item in newSettings.SelectedMediaTemplates) s.SelectedMediaTemplates.Add(item);
            }

            // File path filter settings
            s.FilterByFilePathNotContains = newSettings.FilterByFilePathNotContains;
            s.FilePathNotContainsTexts.Clear();
            if (newSettings.FilePathNotContainsTexts != null) {
                foreach (var item in newSettings.FilePathNotContainsTexts) s.FilePathNotContainsTexts.Add(item);
            }
            s.FilterByFilePathContains = newSettings.FilterByFilePathContains;
            s.FilePathContainsTexts.Clear();
            if (newSettings.FilePathContainsTexts != null) {
                foreach (var item in newSettings.FilePathContainsTexts) s.FilePathContainsTexts.Add(item);
            }

            // File size filter
            s.FilterByFileSize = newSettings.FilterByFileSize;
            s.MinimumFileSize = newSettings.MinimumFileSize;
            s.MaximumFileSize = newSettings.MaximumFileSize;

            VDF.GUI.Data.SettingsFile.SaveSettings();
            ApplySettings(s);
        }

        private void ApplySettings(VDF.GUI.Data.SettingsFile s) {
            // Basic settings
            Engine.Settings.IncludeSubDirectories = s.IncludeSubDirectories;
            Engine.Settings.IncludeImages = s.IncludeImages;
            Engine.Settings.Percent = s.Percent;
            Engine.Settings.PercentDurationDifference = s.PercentDurationDifference;
            Engine.Settings.MaxDegreeOfParallelism = s.MaxDegreeOfParallelism;
            Engine.Settings.ThumbnailCount = s.Thumbnails;
            Engine.Settings.IgnoreReadOnlyFolders = s.IgnoreReadOnlyFolders;
            Engine.Settings.UsePHashing = s.UsePHash;
            Engine.Settings.IncludeNonExistingFiles = s.IncludeNonExistingFiles;

            // Misc settings
            Engine.Settings.IgnoreReparsePoints = s.IgnoreReparsePoints;
            Engine.Settings.ExcludeHardLinks = s.ExcludeHardLinks;
            Engine.Settings.ScanAgainstEntireDatabase = s.ScanAgainstEntireDatabase;

            // Advanced settings
            Engine.Settings.UseExifCreationDate = s.UseExifCreationDate;
            Engine.Settings.IgnoreBlackPixels = s.IgnoreBlackPixels;
            Engine.Settings.IgnoreWhitePixels = s.IgnoreWhitePixels;
            Engine.Settings.CompareHorizontallyFlipped = s.CompareHorizontallyFlipped;
            Engine.Settings.UseNativeFfmpegBinding = s.UseNativeFfmpegBinding;
            Engine.Settings.ExtendedFFToolsLogging = s.ExtendedFFToolsLogging;
            Engine.Settings.AlwaysRetryFailedSampling = s.AlwaysRetryFailedSampling;

            // Performance settings
            Engine.Settings.HardwareAccelerationMode = s.HardwareAccelerationMode;

            // Custom settings
            Engine.Settings.CustomFFArguments = s.CustomFFArguments ?? string.Empty;
            Engine.Settings.CustomDatabaseFolder = s.CustomDatabaseFolder ?? string.Empty;

            // Sync lists
            Engine.Settings.IncludeList.Clear();
            foreach (var item in s.Includes) Engine.Settings.IncludeList.Add(item);
            Engine.Settings.BlackList.Clear();
            foreach (var item in s.Blacklists) Engine.Settings.BlackList.Add(item);

            // File path filters
            Engine.Settings.FilterByFilePathNotContains = s.FilterByFilePathNotContains;
            Engine.Settings.FilePathNotContainsTexts.Clear();
            foreach (var item in s.FilePathNotContainsTexts) Engine.Settings.FilePathNotContainsTexts.Add(item);
            Engine.Settings.FilterByFilePathContains = s.FilterByFilePathContains;
            Engine.Settings.FilePathContainsTexts.Clear();
            foreach (var item in s.FilePathContainsTexts) Engine.Settings.FilePathContainsTexts.Add(item);

            // File size filter
            Engine.Settings.FilterByFileSize = s.FilterByFileSize;
            Engine.Settings.MinimumFileSize = s.MinimumFileSize;
            Engine.Settings.MaximumFileSize = s.MaximumFileSize;

            // Media server template exclusion patterns
            Engine.Settings.ExcludeFilePatterns.Clear();
            if (s.SelectedMediaTemplates != null && s.SelectedMediaTemplates.Count > 0) {
                var patterns = VDF.GUI.Data.MediaServerTemplates.GetPatternsForTemplates(s.SelectedMediaTemplates);
                foreach (var pattern in patterns) {
                    Engine.Settings.ExcludeFilePatterns.Add(pattern);
                }
                VDF.Core.Utils.Logger.Instance.Info($"Applied {s.SelectedMediaTemplates.Count} template(s) with {Engine.Settings.ExcludeFilePatterns.Count} exclusion patterns");
            }

            // Also add FilePathNotContainsTexts patterns to ExcludeFilePatterns for early filtering
            // This ensures patterns like "poster.*" are filtered at file enumeration stage
            if (s.FilterByFilePathNotContains && s.FilePathNotContainsTexts != null) {
                int addedCount = 0;
                foreach (var pattern in s.FilePathNotContainsTexts) {
                    // Only add file-name-like patterns (containing wildcards)
                    if ((pattern.Contains('*') || pattern.Contains('?')) && !pattern.Contains('/')) {
                        Engine.Settings.ExcludeFilePatterns.Add(pattern);
                        addedCount++;
                    }
                }
                VDF.Core.Utils.Logger.Instance.Info($"FilterByFilePathNotContains enabled with {s.FilePathNotContainsTexts.Count} patterns ({addedCount} added to ExcludeFilePatterns)");
            }
        }

        public ScanStatus GetStatus() {
            // Update duplicates count in real-time
            _currentStatus.DuplicatesFound = Engine.Duplicates.Count;
            return _currentStatus;
        }

        public List<RecentFileEntry> GetRecentFiles() {
            lock (_recentFilesLock) {
                return _recentFiles.ToList();
            }
        }

        private void AddRecentFile(string path, string status) {
            lock (_recentFilesLock) {
                if (_recentFiles.Count >= MaxRecentFiles) {
                    _recentFiles.Dequeue();
                }
                _recentFiles.Enqueue(new RecentFileEntry {
                    Path = path,
                    Status = status,
                    Timestamp = DateTime.Now
                });
            }
        }

        private void ClearRecentFiles() {
            lock (_recentFilesLock) {
                _recentFiles.Clear();
            }
        }

        public void StartScan(List<string> paths) {
            if (_currentStatus.IsScanning) return;

            // Re-apply all settings before starting scan to ensure they are up to date
            ApplySettings(VDF.GUI.Data.SettingsFile.Instance);

            Engine.Settings.IncludeList.Clear();
            foreach (var path in paths) {
                Engine.Settings.IncludeList.Add(path);
            }

            // Record scan start time and folders for history
            _scanStartTime = DateTime.UtcNow;
            _scanFolders = new List<string>(paths);
            _totalFilesScanned = 0;

            _currentStatus.IsScanning = true;
            _currentStatus.CurrentActivity = "Starting scan...";
            _currentStatus.Progress = 0;
            _currentStatus.Phase = ScanPhase.EnumeratingFiles;
            _currentStatus.PhaseDescription = "Enumerating files...";
            ClearRecentFiles();

            // Run in background
            Task.Run(() => Engine.StartSearch());
        }

        public void StopScan() {
            if (_currentStatus.IsScanning) {
                Engine.Stop();
                _currentStatus.CurrentActivity = "Stopping...";
            }
        }

        private void Engine_Progress(object? sender, ScanProgressChangedEventArgs e) {
            _currentStatus.CurrentActivity = e.CurrentFile;
            _currentStatus.Progress = (float)e.CurrentPosition / e.MaxPosition * 100;
            _currentStatus.ProcessedFiles = e.CurrentPosition;
            _currentStatus.TotalFiles = e.MaxPosition;
            _currentStatus.Elapsed = e.Elapsed;
            _currentStatus.Remaining = e.Remaining;

            // Track total files scanned for history
            _totalFilesScanned = Math.Max(_totalFilesScanned, e.MaxPosition);

            // Add to recent files with current phase status
            var status = _currentStatus.Phase switch {
                ScanPhase.BuildingHashes => "Hashing",
                ScanPhase.Comparing => "Comparing",
                _ => "Processing"
            };
            AddRecentFile(e.CurrentFile, status);
        }

        private void Engine_FilesEnumerated(object? sender, EventArgs e) {
            _currentStatus.Phase = ScanPhase.BuildingHashes;
            _currentStatus.PhaseDescription = "Building hashes...";
        }

        private void Engine_BuildingHashesDone(object? sender, EventArgs e) {
            _currentStatus.Phase = ScanPhase.Comparing;
            _currentStatus.PhaseDescription = "Comparing files...";
        }

        private void Engine_ScanDone(object? sender, EventArgs e) {
            _currentStatus.Phase = ScanPhase.RetrievingThumbnails;
            _currentStatus.PhaseDescription = "Retrieving thumbnails...";
            _currentStatus.CurrentActivity = "Retrieving thumbnails...";
            // Retrieve thumbnails for preview
            Engine.RetrieveThumbnails();
        }

        private void Engine_ThumbnailsRetrieved(object? sender, EventArgs e) {
            // Cache all thumbnails to persistent storage
            CacheThumbnails();

            // Save scan history if enabled
            SaveScanHistoryIfEnabled();

            _currentStatus.IsScanning = false;
            _currentStatus.CurrentActivity = "Scan Finished";
            _currentStatus.Progress = 100;
            _currentStatus.Phase = ScanPhase.Finished;
            _currentStatus.PhaseDescription = "Finished";
        }

        /// <summary>
        /// Saves the scan results to history if history is enabled.
        /// </summary>
        private void SaveScanHistoryIfEnabled() {
            var settings = VDF.GUI.Data.SettingsFile.Instance;
            if (!settings.EnableScanHistory) return;

            try {
                var duplicates = Engine.Duplicates.ToList();
                if (duplicates.Count == 0) {
                    Logger.Instance.Info("No duplicates found, not saving to history");
                    return;
                }

                var historyManager = GetHistoryManager();
                var scanDuration = DateTime.UtcNow - _scanStartTime;
                var scanId = historyManager.SaveScanHistory(
                    duplicates,
                    _scanFolders,
                    scanDuration,
                    _totalFilesScanned,
                    settings.SaveThumbnailsInHistory);

                if (scanId != null) {
                    Logger.Instance.Info($"Saved scan history with ID: {scanId}{(settings.SaveThumbnailsInHistory ? " (with thumbnails)" : "")}");
                }
            }
            catch (Exception ex) {
                Logger.Instance.Info($"Failed to save scan history: {ex.Message}");
            }
        }

        /// <summary>
        /// Cache all thumbnails to persistent storage after scan completes.
        /// This mirrors the GUI behavior where thumbnails are cached when ThumbnailsUpdated fires.
        /// </summary>
        private void CacheThumbnails() {
            if (VDF.GUI.Utils.ThumbCacheHelpers.Provider == null) return;

            var duplicates = Engine.Duplicates.ToList();
            int cached = 0;

            foreach (var item in duplicates) {
                if (item.ImageList == null || item.ImageList.Count == 0) continue;

                var cacheKey = item.ThumbnailCacheKey;

                // Skip if already cached
                if (VDF.GUI.Utils.ThumbCacheHelpers.Provider.Contains(cacheKey)) continue;

                try {
                    VDF.GUI.Utils.ThumbCacheHelpers.Provider.AppendIfMissing(cacheKey, stream => {
                        if (item.ImageList.Count == 1) {
                            // Single thumbnail
                            item.ImageList[0].SaveAsJpeg(stream, new JpegEncoder { Quality = 90 });
                        } else {
                            // Multiple thumbnails - join horizontally
                            int height = item.ImageList[0].Height;
                            int totalWidth = item.ImageList.Sum(img => img.Width);

                            using var joined = new Image<Rgba32>(totalWidth, height);
                            joined.Mutate(ctx => {
                                int offsetX = 0;
                                foreach (var img in item.ImageList) {
                                    ctx.DrawImage(img, new SixLabors.ImageSharp.Point(offsetX, 0), 1f);
                                    offsetX += img.Width;
                                }
                            });
                            joined.SaveAsJpeg(stream, new JpegEncoder { Quality = 90 });
                        }
                    });
                    cached++;
                }
                catch {
                    // Ignore cache errors for individual items
                }
            }

            // Flush index to disk immediately to ensure persistence
            try {
                VDF.GUI.Utils.ThumbCacheHelpers.Provider?.FlushIndex();
            }
            catch { /* Ignore flush errors */ }

            Logger.Instance.Info($"Cached {cached} thumbnails to persistent storage");
        }

        private void Engine_ScanAborted(object? sender, EventArgs e) {
            _currentStatus.IsScanning = false;
            _currentStatus.CurrentActivity = "Scan Aborted";
            _currentStatus.Phase = ScanPhase.Idle;
            _currentStatus.PhaseDescription = "Aborted";
        }
    }
}
