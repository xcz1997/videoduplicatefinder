using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VDF.Core;
using VDF.Core.Utils;

namespace VDF.Web.Server.Services {
    public class ScanService {
        public ScanEngine Engine { get; }
        
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
        }

        private ScanStatus _currentStatus = new ScanStatus();

        public ScanService() {
            Engine = new ScanEngine();
            // Load settings from disk on startup
            try {
                VDF.GUI.Data.SettingsFile.LoadSettings();
                ApplySettings(VDF.GUI.Data.SettingsFile.Instance);
            } catch { /* ignore first load error */ }

            Engine.Progress += Engine_Progress;
            Engine.ScanDone += Engine_ScanDone;
            Engine.ScanAborted += Engine_ScanAborted;
        }

        public VDF.GUI.Data.SettingsFile GetSettings() {
            return VDF.GUI.Data.SettingsFile.Instance;
        }

        public void SaveSettings(VDF.GUI.Data.SettingsFile newSettings) {
            // Copy properties from DTO to Singleton Instance
            // Ideally use AutoMapper, manual for now
            var s = VDF.GUI.Data.SettingsFile.Instance;
            
            s.IncludeSubDirectories = newSettings.IncludeSubDirectories;
            s.IncludeImages = newSettings.IncludeImages;
            s.Percent = newSettings.Percent;
            s.PercentDurationDifference = newSettings.PercentDurationDifference;
            s.MaxDegreeOfParallelism = newSettings.MaxDegreeOfParallelism;
            s.Thumbnails = newSettings.Thumbnails;
            s.IgnoreReadOnlyFolders = newSettings.IgnoreReadOnlyFolders;
            s.UsePHash = newSettings.UsePHash;
            s.IncludeNonExistingFiles = newSettings.IncludeNonExistingFiles;
            // ... Add all other properties

            VDF.GUI.Data.SettingsFile.SaveSettings();
            ApplySettings(s);
        }

        private void ApplySettings(VDF.GUI.Data.SettingsFile s) {
            Engine.Settings.IncludeSubDirectories = s.IncludeSubDirectories;
            Engine.Settings.IncludeImages = s.IncludeImages;
            Engine.Settings.Percent = s.Percent;
            Engine.Settings.PercentDurationDifference = s.PercentDurationDifference;
            Engine.Settings.MaxDegreeOfParallelism = s.MaxDegreeOfParallelism;
            Engine.Settings.ThumbnailCount = s.Thumbnails;
            Engine.Settings.IgnoreReadOnlyFolders = s.IgnoreReadOnlyFolders;
            Engine.Settings.UsePHashing = s.UsePHash;
            Engine.Settings.IncludeNonExistingFiles = s.IncludeNonExistingFiles;
            // Sync lists
            Engine.Settings.IncludeList.Clear();
            foreach (var item in s.Includes) Engine.Settings.IncludeList.Add(item);
            Engine.Settings.BlackList.Clear();
            foreach (var item in s.Blacklists) Engine.Settings.BlackList.Add(item);
        }

        public ScanStatus GetStatus() {
            // Update duplicates count in real-time
            _currentStatus.DuplicatesFound = Engine.Duplicates.Count;
            return _currentStatus;
        }

        public void StartScan(List<string> paths) {
            if (_currentStatus.IsScanning) return;

            Engine.Settings.IncludeList.Clear();
            foreach (var path in paths) {
                Engine.Settings.IncludeList.Add(path);
            }

            _currentStatus.IsScanning = true;
            _currentStatus.CurrentActivity = "Starting scan...";
            _currentStatus.Progress = 0;
            
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
        }

        private void Engine_ScanDone(object? sender, EventArgs e) {
            _currentStatus.IsScanning = false;
            _currentStatus.CurrentActivity = "Scan Finished";
            _currentStatus.Progress = 100;
        }

        private void Engine_ScanAborted(object? sender, EventArgs e) {
            _currentStatus.IsScanning = false;
            _currentStatus.CurrentActivity = "Scan Aborted";
        }
    }
}
