using Microsoft.AspNetCore.Mvc;
using VDF.Web.Server.Services;

namespace VDF.Web.Server.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class MonitorController : ControllerBase {
        private readonly SystemMonitorService _monitorService;

        public MonitorController(SystemMonitorService monitorService) {
            _monitorService = monitorService;
        }

        [HttpGet("metrics")]
        public IActionResult GetMetrics() {
            var history = _monitorService.GetHistory();
            var selectedGpu = _monitorService.GetSelectedGpu();
            return Ok(new {
                History = history.Select(p => new {
                    Time = p.Time.ToString("o"),
                    p.Cpu,
                    p.Gpu
                }),
                GpuAvailable = _monitorService.IsGpuAvailable,
                SelectedGpuId = _monitorService.GetSelectedGpuId(),
                SelectedGpuName = selectedGpu?.Name ?? ""
            });
        }

        [HttpGet("current")]
        public IActionResult GetCurrent() {
            var current = _monitorService.GetCurrent();
            var selectedGpu = _monitorService.GetSelectedGpu();
            return Ok(new {
                Time = current.Time.ToString("o"),
                current.Cpu,
                current.Gpu,
                GpuAvailable = _monitorService.IsGpuAvailable,
                SelectedGpuId = _monitorService.GetSelectedGpuId(),
                SelectedGpuName = selectedGpu?.Name ?? ""
            });
        }

        [HttpGet("gpus")]
        public IActionResult GetGpus() {
            var gpus = _monitorService.GetDetectedGpus();
            return Ok(new {
                Gpus = gpus.Select(g => new {
                    g.Id,
                    g.Name,
                    Vendor = g.Vendor.ToString(),
                    RecommendedHwAccel = g.RecommendedHwAccel.ToString(),
                    g.IsAvailable,
                    g.Priority
                }),
                SelectedGpuId = _monitorService.GetSelectedGpuId()
            });
        }

        [HttpPost("gpus/select")]
        public IActionResult SelectGpu([FromBody] SelectGpuRequest request) {
            _monitorService.SelectGpu(request.GpuId);
            return Ok(new { Success = true, SelectedGpuId = _monitorService.GetSelectedGpuId() });
        }

        [HttpGet("recommend")]
        public IActionResult GetRecommendation() {
            var recommendation = _monitorService.GetRecommendedHwAccel();
            return Ok(new {
                Mode = (int)recommendation.Mode,
                ModeName = recommendation.Mode.ToString(),
                recommendation.GpuName,
                recommendation.Reason
            });
        }

        public class SelectGpuRequest {
            public string GpuId { get; set; } = string.Empty;
        }

        [HttpGet("monitor-methods")]
        public IActionResult GetMonitorMethods() {
            var methods = _monitorService.GetAvailableMonitorMethods();
            var currentMethod = VDF.GUI.Data.SettingsFile.Instance.GpuMonitorMethod;
            return Ok(new {
                Methods = methods,
                CurrentMethod = currentMethod
            });
        }

        [HttpPost("monitor-method")]
        public IActionResult SetMonitorMethod([FromBody] SetMonitorMethodRequest request) {
            var s = VDF.GUI.Data.SettingsFile.Instance;
            s.GpuMonitorMethod = request.Method;
            VDF.GUI.Data.SettingsFile.SaveSettings();
            _monitorService.SetMonitorMethod(request.Method);
            return Ok(new { Success = true, Method = request.Method });
        }

        public class SetMonitorMethodRequest {
            public string Method { get; set; } = "auto";
        }
    }
}
