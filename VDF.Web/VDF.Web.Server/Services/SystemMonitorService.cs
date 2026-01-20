using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using VDF.Core.FFTools;

namespace VDF.Web.Server.Services {
    public class SystemMonitorService : IDisposable {
        #region Data Models

        public class MetricPoint {
            public DateTime Time { get; set; }
            public double Cpu { get; set; }
            public double? Gpu { get; set; }
        }

        public class GpuInfo {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public GpuVendor Vendor { get; set; }
            public FFHardwareAccelerationMode RecommendedHwAccel { get; set; }
            public bool IsAvailable { get; set; }
            public int Priority { get; set; } // Lower is higher priority
        }

        public enum GpuVendor {
            Unknown,
            Nvidia,
            Intel,
            Apple,
            Amd
        }

        public class HwAccelRecommendation {
            public FFHardwareAccelerationMode Mode { get; set; }
            public string GpuName { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
        }

        #endregion

        #region Fields

        private readonly ConcurrentQueue<MetricPoint> _history = new();
        private readonly Timer _timer;
        private const int MaxHistorySize = 60;

        // System CPU tracking
        private long _lastIdleTime;
        private long _lastTotalTime;
        private double _cachedCpuUsage;
        private DateTime _lastCpuQuery = DateTime.MinValue;
        private const int CpuQueryIntervalMs = 1000;

        private readonly List<GpuInfo> _detectedGpus = new();
        private bool _gpuDetectionDone = false;
        private string _selectedGpuId = string.Empty;
        private readonly object _gpuLock = new();

        // GPU usage caching - avoid calling external processes every second
        private double? _cachedGpuUsage = null;
        private DateTime _lastGpuQuery = DateTime.MinValue;
        private const int GpuQueryIntervalMs = 3000; // Query GPU every 3 seconds
        private bool _gpuQueryInProgress = false;
        private readonly object _gpuQueryLock = new();

        // Apple Silicon detection method cache
        private enum AppleGpuMethod { None, Gpuinfo, Macmon, Ioreg }
        private AppleGpuMethod _appleGpuMethod = AppleGpuMethod.None;
        private bool _appleGpuMethodChecked = false;

        // Process timeout
        private const int ProcessTimeoutMs = 500;

        // User-selected monitor method
        private string _userMonitorMethod = "auto";

        #endregion

        #region Constructor

        public SystemMonitorService() {
            // Load user preference
            try {
                VDF.GUI.Data.SettingsFile.LoadSettings();
                _userMonitorMethod = VDF.GUI.Data.SettingsFile.Instance.GpuMonitorMethod ?? "auto";
            }
            catch { }

            // Detect GPUs asynchronously
            Task.Run(DetectAllGpus);

            // Start collection timer (every 1 second)
            _timer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));
        }

        #endregion

        #region GPU Detection

        private void DetectAllGpus() {
            lock (_gpuLock) {
                _detectedGpus.Clear();

                // Detect NVIDIA GPU
                var nvidia = DetectNvidiaGpu();
                if (nvidia != null) _detectedGpus.Add(nvidia);

                // Detect Intel GPU
                var intel = DetectIntelGpu();
                if (intel != null) _detectedGpus.Add(intel);

                // Detect Apple Silicon GPU
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    var apple = DetectAppleSiliconGpu();
                    if (apple != null) _detectedGpus.Add(apple);
                }

                // Detect AMD GPU (Windows)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    var amd = DetectAmdGpu();
                    if (amd != null) _detectedGpus.Add(amd);
                }

                // Sort by priority
                _detectedGpus.Sort((a, b) => a.Priority.CompareTo(b.Priority));

                // Select default GPU based on hardware acceleration setting
                SelectDefaultGpu();

                _gpuDetectionDone = true;
            }
        }

        private void SelectDefaultGpu() {
            if (_detectedGpus.Count == 0) return;

            var hwAccelMode = VDF.GUI.Data.SettingsFile.Instance.HardwareAccelerationMode;

            // Find GPU matching hardware acceleration mode
            GpuInfo? matched = hwAccelMode switch {
                FFHardwareAccelerationMode.cuda => _detectedGpus.Find(g => g.Vendor == GpuVendor.Nvidia),
                FFHardwareAccelerationMode.qsv => _detectedGpus.Find(g => g.Vendor == GpuVendor.Intel),
                FFHardwareAccelerationMode.videotoolbox => _detectedGpus.Find(g => g.Vendor == GpuVendor.Apple),
                FFHardwareAccelerationMode.vaapi => _detectedGpus.Find(g => g.Vendor == GpuVendor.Intel || g.Vendor == GpuVendor.Amd),
                FFHardwareAccelerationMode.vdpau => _detectedGpus.Find(g => g.Vendor == GpuVendor.Nvidia || g.Vendor == GpuVendor.Amd),
                _ => null
            };

            _selectedGpuId = matched?.Id ?? _detectedGpus[0].Id;
        }

        private GpuInfo? DetectNvidiaGpu() {
            try {
                var output = RunProcessWithTimeout("nvidia-smi", "--query-gpu=name --format=csv,noheader,nounits", 2000);
                if (!string.IsNullOrEmpty(output)) {
                    return new GpuInfo {
                        Id = "nvidia",
                        Name = output.Split('\n')[0].Trim(),
                        Vendor = GpuVendor.Nvidia,
                        RecommendedHwAccel = FFHardwareAccelerationMode.cuda,
                        IsAvailable = true,
                        Priority = 1 // NVIDIA has highest priority for video processing
                    };
                }
            }
            catch { }
            return null;
        }

        private GpuInfo? DetectIntelGpu() {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    var gpuPath = "/sys/class/drm/card0/device/vendor";
                    if (File.Exists(gpuPath)) {
                        var vendor = File.ReadAllText(gpuPath).Trim();
                        if (vendor == "0x8086") {
                            return new GpuInfo {
                                Id = "intel",
                                Name = "Intel GPU",
                                Vendor = GpuVendor.Intel,
                                RecommendedHwAccel = FFHardwareAccelerationMode.vaapi,
                                IsAvailable = true,
                                Priority = 2
                            };
                        }
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    // Use wmic instead of PowerShell - much faster
                    var output = RunProcessWithTimeout("wmic", "path win32_VideoController get name", 2000);
                    if (!string.IsNullOrEmpty(output) && output.Contains("Intel", StringComparison.OrdinalIgnoreCase)) {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines) {
                            if (line.Contains("Intel", StringComparison.OrdinalIgnoreCase)) {
                                return new GpuInfo {
                                    Id = "intel",
                                    Name = line.Trim(),
                                    Vendor = GpuVendor.Intel,
                                    RecommendedHwAccel = FFHardwareAccelerationMode.qsv,
                                    IsAvailable = true,
                                    Priority = 2
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private GpuInfo? DetectAppleSiliconGpu() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return null;

            try {
                var output = RunProcessWithTimeout("sysctl", "-n machdep.cpu.brand_string", 1000);
                if (!string.IsNullOrEmpty(output) && output.Contains("Apple")) {
                    // Get chip name (M1, M2, M3, etc.)
                    var chipName = "Apple Silicon GPU";
                    var match = Regex.Match(output, @"Apple\s+(M\d+(?:\s+\w+)?)");
                    if (match.Success) {
                        chipName = $"Apple {match.Groups[1].Value} GPU";
                    }

                    return new GpuInfo {
                        Id = "apple",
                        Name = chipName,
                        Vendor = GpuVendor.Apple,
                        RecommendedHwAccel = FFHardwareAccelerationMode.videotoolbox,
                        IsAvailable = true,
                        Priority = 1
                    };
                }
            }
            catch { }
            return null;
        }

        private GpuInfo? DetectAmdGpu() {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    // Use wmic instead of PowerShell - much faster
                    var output = RunProcessWithTimeout("wmic", "path win32_VideoController get name", 2000);
                    if (!string.IsNullOrEmpty(output)) {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines) {
                            if (line.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                                line.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) {
                                return new GpuInfo {
                                    Id = "amd",
                                    Name = line.Trim(),
                                    Vendor = GpuVendor.Amd,
                                    RecommendedHwAccel = FFHardwareAccelerationMode.d3d11va,
                                    IsAvailable = true,
                                    Priority = 2
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region Metrics Collection

        private void CollectMetrics(object? state) {
            try {
                var now = DateTime.UtcNow;
                var cpuUsage = CalculateCpuUsage(now);

                // Get GPU usage (cached or fresh)
                var gpuUsage = GetCachedOrFreshGpuUsage(now);

                var point = new MetricPoint {
                    Time = now,
                    Cpu = cpuUsage,
                    Gpu = gpuUsage
                };

                _history.Enqueue(point);

                while (_history.Count > MaxHistorySize) {
                    _history.TryDequeue(out _);
                }
            }
            catch { }
        }

        private double? GetCachedOrFreshGpuUsage(DateTime now) {
            // Check if we should query GPU
            var timeSinceLastQuery = (now - _lastGpuQuery).TotalMilliseconds;

            if (timeSinceLastQuery >= GpuQueryIntervalMs) {
                lock (_gpuQueryLock) {
                    // Double-check after acquiring lock
                    if (!_gpuQueryInProgress && (now - _lastGpuQuery).TotalMilliseconds >= GpuQueryIntervalMs) {
                        _gpuQueryInProgress = true;

                        // Query GPU asynchronously to not block the timer
                        Task.Run(() => {
                            try {
                                var usage = GetSelectedGpuUsage();
                                _cachedGpuUsage = usage;
                                _lastGpuQuery = DateTime.UtcNow;
                            }
                            finally {
                                _gpuQueryInProgress = false;
                            }
                        });
                    }
                }
            }

            return _cachedGpuUsage;
        }

        private double CalculateCpuUsage(DateTime now) {
            // Return cached value if queried recently
            if ((now - _lastCpuQuery).TotalMilliseconds < CpuQueryIntervalMs) {
                return _cachedCpuUsage;
            }

            try {
                double? usage = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    usage = GetLinuxCpuUsage();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    usage = GetMacOsCpuUsage();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    usage = GetWindowsCpuUsage();
                }

                if (usage.HasValue) {
                    _cachedCpuUsage = usage.Value;
                    _lastCpuQuery = now;
                }

                return _cachedCpuUsage;
            }
            catch {
                return _cachedCpuUsage;
            }
        }

        private double? GetLinuxCpuUsage() {
            try {
                var lines = File.ReadAllLines("/proc/stat");
                var cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu "));
                if (cpuLine == null) return null;

                var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) return null;

                // cpu user nice system idle iowait irq softirq
                long user = long.Parse(parts[1]);
                long nice = long.Parse(parts[2]);
                long system = long.Parse(parts[3]);
                long idle = long.Parse(parts[4]);
                long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;

                long totalIdle = idle + iowait;
                long total = user + nice + system + idle + iowait;
                if (parts.Length > 6) total += long.Parse(parts[6]); // irq
                if (parts.Length > 7) total += long.Parse(parts[7]); // softirq

                if (_lastTotalTime > 0) {
                    long totalDiff = total - _lastTotalTime;
                    long idleDiff = totalIdle - _lastIdleTime;

                    if (totalDiff > 0) {
                        var usage = (1.0 - (double)idleDiff / totalDiff) * 100;
                        _lastIdleTime = totalIdle;
                        _lastTotalTime = total;
                        return Math.Min(100, Math.Max(0, usage));
                    }
                }

                _lastIdleTime = totalIdle;
                _lastTotalTime = total;
                return null;
            }
            catch {
                return null;
            }
        }

        private double? GetMacOsCpuUsage() {
            // Try macmon first (faster and more accurate for Apple Silicon)
            var macmonUsage = GetCpuUsageFromMacmon();
            if (macmonUsage.HasValue) {
                return macmonUsage;
            }

            // Fallback to top command
            var output = RunProcessWithTimeout("/bin/bash", "-c \"top -l 2 -n 0 -s 1 | grep 'CPU usage' | tail -1\"", 2500);
            if (!string.IsNullOrEmpty(output)) {
                // Format: CPU usage: 5.26% user, 3.94% sys, 90.79% idle
                var match = Regex.Match(output, @"([\d.]+)%\s+idle");
                if (match.Success && double.TryParse(match.Groups[1].Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var idle)) {
                    return 100 - idle;
                }
            }
            return null;
        }

        private double? GetWindowsCpuUsage() {
            // Use wmic for faster response than PerformanceCounter
            var output = RunProcessWithTimeout("wmic", "cpu get loadpercentage", 1000);
            if (!string.IsNullOrEmpty(output)) {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines) {
                    if (int.TryParse(line.Trim(), out var load)) {
                        return load;
                    }
                }
            }
            return null;
        }

        private double? GetSelectedGpuUsage() {
            if (!_gpuDetectionDone || string.IsNullOrEmpty(_selectedGpuId)) return null;

            lock (_gpuLock) {
                var gpu = _detectedGpus.Find(g => g.Id == _selectedGpuId);
                if (gpu == null) return null;

                return gpu.Vendor switch {
                    GpuVendor.Nvidia => GetNvidiaGpuUsage(),
                    GpuVendor.Intel => GetIntelGpuUsage(),
                    GpuVendor.Apple => GetAppleSiliconGpuUsage(),
                    GpuVendor.Amd => GetAmdGpuUsage(),
                    _ => null
                };
            }
        }

        #endregion

        #region GPU Usage Methods

        private string? RunProcessWithTimeout(string fileName, string arguments, int timeoutMs) {
            try {
                var psi = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                // Read output with timeout
                var outputTask = process.StandardOutput.ReadToEndAsync();
                if (process.WaitForExit(timeoutMs)) {
                    if (process.ExitCode == 0 && outputTask.Wait(100)) {
                        return outputTask.Result?.Trim();
                    }
                }
                else {
                    // Timeout - kill the process
                    try { process.Kill(); } catch { }
                }
            }
            catch { }
            return null;
        }

        private double? GetNvidiaGpuUsage() {
            var output = RunProcessWithTimeout("nvidia-smi", "--query-gpu=utilization.gpu --format=csv,noheader,nounits", ProcessTimeoutMs);
            if (!string.IsNullOrEmpty(output) && double.TryParse(output, out var usage)) {
                return usage;
            }
            return null;
        }

        private double? GetIntelGpuUsage() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                return GetIntelGpuUsageLinux();
            }
            // Skip Windows Performance Counter - too slow
            // Return null to indicate no real-time usage data available
            return null;
        }

        private double? GetIntelGpuUsageLinux() {
            try {
                var freqPath = "/sys/class/drm/card0/gt_cur_freq_mhz";
                var maxFreqPath = "/sys/class/drm/card0/gt_max_freq_mhz";
                if (File.Exists(freqPath) && File.Exists(maxFreqPath)) {
                    var curFreq = double.Parse(File.ReadAllText(freqPath).Trim());
                    var maxFreq = double.Parse(File.ReadAllText(maxFreqPath).Trim());
                    if (maxFreq > 0) {
                        return (curFreq / maxFreq) * 100;
                    }
                }
            }
            catch { }
            return null;
        }

        private double? GetAppleSiliconGpuUsage() {
            // If user specified a method, use it directly
            if (_userMonitorMethod != "auto") {
                return _userMonitorMethod switch {
                    "macmon" => GetGpuUsageViaMacmon(),
                    "ioreg" => GetGpuUsageViaIoreg(),
                    "none" => null,
                    _ => null
                };
            }

            // Auto detection
            if (_appleGpuMethodChecked) {
                return _appleGpuMethod switch {
                    AppleGpuMethod.Macmon => GetGpuUsageViaMacmon(),
                    AppleGpuMethod.Gpuinfo => GetGpuUsageViaGpuinfo(),
                    AppleGpuMethod.Ioreg => GetGpuUsageViaIoreg(),
                    _ => null
                };
            }

            // Try macmon first (most accurate for Apple Silicon)
            var macmon = GetGpuUsageViaMacmon();
            if (macmon.HasValue) {
                _appleGpuMethod = AppleGpuMethod.Macmon;
                _appleGpuMethodChecked = true;
                return macmon;
            }

            // Try gpuinfo
            var gpuinfo = GetGpuUsageViaGpuinfo();
            if (gpuinfo.HasValue) {
                _appleGpuMethod = AppleGpuMethod.Gpuinfo;
                _appleGpuMethodChecked = true;
                return gpuinfo;
            }

            // Fallback to ioreg
            var ioreg = GetGpuUsageViaIoreg();
            if (ioreg.HasValue) {
                _appleGpuMethod = AppleGpuMethod.Ioreg;
                _appleGpuMethodChecked = true;
                return ioreg;
            }

            _appleGpuMethodChecked = true;
            _appleGpuMethod = AppleGpuMethod.None;
            return null;
        }

        private double? GetGpuUsageViaGpuinfo() {
            var output = RunProcessWithTimeout("gpuinfo", "-p", ProcessTimeoutMs);
            if (!string.IsNullOrEmpty(output) && double.TryParse(output, out var usage)) {
                return usage;
            }
            return null;
        }

        private double? GetGpuUsageViaMacmon() {
            var output = RunMacmonOnce();
            if (!string.IsNullOrEmpty(output)) {
                var match = Regex.Match(output, @"""gpu_usage"":\s*\[\s*[\d.]+\s*,\s*([\d.]+)\s*\]");
                if (match.Success && double.TryParse(match.Groups[1].Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var usage)) {
                    return usage * 100;
                }
            }
            return null;
        }

        // Cached macmon output for both CPU and GPU
        private string? _cachedMacmonOutput = null;
        private DateTime _lastMacmonQuery = DateTime.MinValue;
        private const int MacmonQueryIntervalMs = 1000;

        private string? RunMacmonOnce() {
            // Return cached output if recent
            var now = DateTime.UtcNow;
            if ((now - _lastMacmonQuery).TotalMilliseconds < MacmonQueryIntervalMs && _cachedMacmonOutput != null) {
                return _cachedMacmonOutput;
            }

            try {
                var psi = new ProcessStartInfo {
                    FileName = "macmon",
                    Arguments = "pipe -i 500",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                // Read first line only
                var line = process.StandardOutput.ReadLine();

                // Kill the process immediately after getting first line
                try { process.Kill(); } catch { }

                if (!string.IsNullOrEmpty(line)) {
                    _cachedMacmonOutput = line;
                    _lastMacmonQuery = now;
                    return line;
                }
            }
            catch { }
            return null;
        }

        private double? GetCpuUsageFromMacmon() {
            var output = RunMacmonOnce();
            if (!string.IsNullOrEmpty(output)) {
                // Parse ecpu_usage and pcpu_usage
                // Format: "ecpu_usage":[freq, usage], "pcpu_usage":[freq, usage]
                double totalUsage = 0;
                int count = 0;

                var ecpuMatch = Regex.Match(output, @"""ecpu_usage"":\s*\[\s*[\d.]+\s*,\s*([\d.]+)\s*\]");
                if (ecpuMatch.Success && double.TryParse(ecpuMatch.Groups[1].Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var ecpuUsage)) {
                    totalUsage += ecpuUsage;
                    count++;
                }

                var pcpuMatch = Regex.Match(output, @"""pcpu_usage"":\s*\[\s*[\d.]+\s*,\s*([\d.]+)\s*\]");
                if (pcpuMatch.Success && double.TryParse(pcpuMatch.Groups[1].Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var pcpuUsage)) {
                    totalUsage += pcpuUsage;
                    count++;
                }

                if (count > 0) {
                    // Average of E-CPU and P-CPU, convert to percentage
                    return (totalUsage / count) * 100;
                }
            }
            return null;
        }

        private double? GetGpuUsageViaIoreg() {
            var output = RunProcessWithTimeout("/bin/bash", "-c \"ioreg -r -c IOAccelerator | grep -E 'Device Utilization'\"", ProcessTimeoutMs);
            if (!string.IsNullOrEmpty(output)) {
                // Match formats like: "Device Utilization %" = 30 or Device Utilization % = 30
                var match = Regex.Match(output, @"""?Device Utilization\s*%?""?\s*=\s*(\d+)");
                if (match.Success && double.TryParse(match.Groups[1].Value, out var usage)) {
                    return usage;
                }
            }
            return null;
        }

        private double? GetAmdGpuUsage() {
            // AMD GPU usage - skip Windows Performance Counter (too slow)
            return null;
        }

        #endregion

        #region Public API

        public List<GpuInfo> GetDetectedGpus() {
            lock (_gpuLock) {
                return new List<GpuInfo>(_detectedGpus);
            }
        }

        public void SelectGpu(string gpuId) {
            lock (_gpuLock) {
                if (_detectedGpus.Any(g => g.Id == gpuId)) {
                    _selectedGpuId = gpuId;
                    // Reset cache to force refresh
                    _lastGpuQuery = DateTime.MinValue;
                    _cachedGpuUsage = null;
                }
            }
        }

        public string GetSelectedGpuId() {
            return _selectedGpuId;
        }

        public GpuInfo? GetSelectedGpu() {
            lock (_gpuLock) {
                return _detectedGpus.Find(g => g.Id == _selectedGpuId);
            }
        }

        public HwAccelRecommendation GetRecommendedHwAccel() {
            lock (_gpuLock) {
                // Priority: NVIDIA CUDA > Apple VideoToolbox > Intel QSV > AMD > None
                var nvidia = _detectedGpus.Find(g => g.Vendor == GpuVendor.Nvidia);
                if (nvidia != null) {
                    return new HwAccelRecommendation {
                        Mode = FFHardwareAccelerationMode.cuda,
                        GpuName = nvidia.Name,
                        Reason = "NVIDIA CUDA provides excellent hardware decoding performance"
                    };
                }

                var apple = _detectedGpus.Find(g => g.Vendor == GpuVendor.Apple);
                if (apple != null) {
                    return new HwAccelRecommendation {
                        Mode = FFHardwareAccelerationMode.videotoolbox,
                        GpuName = apple.Name,
                        Reason = "VideoToolbox is optimized for Apple Silicon"
                    };
                }

                var intel = _detectedGpus.Find(g => g.Vendor == GpuVendor.Intel);
                if (intel != null) {
                    var mode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? FFHardwareAccelerationMode.qsv
                        : FFHardwareAccelerationMode.vaapi;
                    return new HwAccelRecommendation {
                        Mode = mode,
                        GpuName = intel.Name,
                        Reason = $"Intel {(mode == FFHardwareAccelerationMode.qsv ? "Quick Sync" : "VAAPI")} provides good hardware acceleration"
                    };
                }

                var amd = _detectedGpus.Find(g => g.Vendor == GpuVendor.Amd);
                if (amd != null) {
                    var mode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? FFHardwareAccelerationMode.d3d11va
                        : FFHardwareAccelerationMode.vaapi;
                    return new HwAccelRecommendation {
                        Mode = mode,
                        GpuName = amd.Name,
                        Reason = "AMD GPU hardware acceleration"
                    };
                }

                return new HwAccelRecommendation {
                    Mode = FFHardwareAccelerationMode.auto,
                    GpuName = "Auto",
                    Reason = "Let FFmpeg automatically detect the best option"
                };
            }
        }

        public MetricPoint[] GetHistory() {
            return _history.ToArray();
        }

        public MetricPoint GetCurrent() {
            var arr = _history.ToArray();
            return arr.Length > 0 ? arr[^1] : new MetricPoint { Time = DateTime.UtcNow, Cpu = 0, Gpu = null };
        }

        public bool IsGpuAvailable => _gpuDetectionDone && _detectedGpus.Count > 0;

        public class MonitorMethodInfo {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Platform { get; set; } = string.Empty;
            public string Dependency { get; set; } = string.Empty;
            public bool IsAvailable { get; set; }
        }

        public List<MonitorMethodInfo> GetAvailableMonitorMethods() {
            var methods = new List<MonitorMethodInfo>();

            // Auto - always available
            methods.Add(new MonitorMethodInfo {
                Id = "auto",
                Name = "Auto",
                Description = "Automatically detect the best method",
                Platform = "All",
                Dependency = "",
                IsAvailable = true
            });

            // None - always available
            methods.Add(new MonitorMethodInfo {
                Id = "none",
                Name = "None",
                Description = "Disable GPU monitoring",
                Platform = "All",
                Dependency = "",
                IsAvailable = true
            });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                // macmon - check if available
                var macmonAvailable = RunProcessWithTimeout("which", "macmon", 500) != null;
                methods.Add(new MonitorMethodInfo {
                    Id = "macmon",
                    Name = "macmon",
                    Description = "Apple Silicon monitor (recommended)",
                    Platform = "macOS",
                    Dependency = "brew install vladkens/tap/macmon",
                    IsAvailable = macmonAvailable
                });

                // ioreg - always available on macOS
                methods.Add(new MonitorMethodInfo {
                    Id = "ioreg",
                    Name = "ioreg",
                    Description = "System IORegistry (built-in)",
                    Platform = "macOS",
                    Dependency = "",
                    IsAvailable = true
                });
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                // nvidia-smi - check if available
                var nvidiaSmiAvailable = RunProcessWithTimeout("nvidia-smi", "--version", 1000) != null;
                methods.Add(new MonitorMethodInfo {
                    Id = "nvidia-smi",
                    Name = "nvidia-smi",
                    Description = "NVIDIA GPU monitoring",
                    Platform = "Windows/Linux",
                    Dependency = "NVIDIA Driver",
                    IsAvailable = nvidiaSmiAvailable
                });
            }

            return methods;
        }

        public void SetMonitorMethod(string method) {
            _userMonitorMethod = method;
            // Reset detection cache to force re-detection with new method
            _appleGpuMethodChecked = false;
            _appleGpuMethod = AppleGpuMethod.None;
            _cachedGpuUsage = null;
            _lastGpuQuery = DateTime.MinValue;
        }

        #endregion

        public void Dispose() {
            _timer.Dispose();
        }
    }
}
