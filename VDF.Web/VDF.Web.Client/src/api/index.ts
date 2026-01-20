import axios from 'axios';

const api = axios.create({
  baseURL: '/api', // Use Vite proxy
});

export const fileSystem = {
  getDrives: () => api.get('/filesystem/drives').then(res => res.data),
  getList: (path: string) => api.get('/filesystem/list', { params: { path } }).then(res => res.data),
};

export const scan = {
  getStatus: () => api.get('/scan/status').then(res => res.data),
  start: (paths: string[]) => api.post('/scan/start', paths),
  stop: () => api.post('/scan/stop'),
  getResults: () => api.get('/scan/results').then(res => res.data),
  getRecentFiles: () => api.get('/scan/recent-files').then(res => res.data),
  clearDatabase: () => api.post('/scan/clear-database').then(res => res.data),
  cleanupDatabase: () => api.post('/scan/cleanup-database').then(res => res.data),
  deleteFiles: (paths: string[]) => api.post('/scan/delete', paths).then(res => res.data),
  saveResults: () => api.post('/scan/save-results').then(res => res.data),
};

export const settings = {
  get: () => api.get('/settings').then(res => res.data),
  save: (data: any) => api.post('/settings', data),
  getCacheInfo: () => api.get('/settings/cache-info').then(res => res.data),
  clearCache: () => api.post('/settings/clear-cache').then(res => res.data),
  updateIncludes: (paths: string[]) => api.patch('/settings/includes', paths).then(res => res.data),
};

export interface CacheInfo {
  cacheSize: number;
  cacheSizeFormatted: string;
  cacheFolder: string;
  defaultCacheFolder: string;
}

export const localization = {
  getList: () => api.get('/localization/list').then(res => res.data),
};

export const monitor = {
  getMetrics: () => api.get('/monitor/metrics').then(res => res.data),
  getCurrent: () => api.get('/monitor/current').then(res => res.data),
  getGpus: () => api.get('/monitor/gpus').then(res => res.data),
  selectGpu: (gpuId: string) => api.post('/monitor/gpus/select', { gpuId }).then(res => res.data),
  getRecommendation: () => api.get('/monitor/recommend').then(res => res.data),
  getMonitorMethods: () => api.get('/monitor/monitor-methods').then(res => res.data),
  setMonitorMethod: (method: string) => api.post('/monitor/monitor-method', { method }).then(res => res.data),
};

export interface MetricPoint {
  time: string;
  cpu: number;
  gpu: number | null;
}

export interface GpuInfo {
  id: string;
  name: string;
  vendor: string;
  recommendedHwAccel: string;
  isAvailable: boolean;
  priority: number;
}

export interface GpusResponse {
  gpus: GpuInfo[];
  selectedGpuId: string;
}

export interface MetricsResponse {
  history: MetricPoint[];
  gpuAvailable: boolean;
  selectedGpuId: string;
  selectedGpuName: string;
}

export interface HwAccelRecommendation {
  mode: number;
  modeName: string;
  gpuName: string;
  reason: string;
}

export interface MonitorMethodInfo {
  id: string;
  name: string;
  description: string;
  platform: string;
  dependency: string;
  isAvailable: boolean;
}

export interface MonitorMethodsResponse {
  methods: MonitorMethodInfo[];
  currentMethod: string;
}

export interface FileSystemNode {
  name: string;
  path: string;
  isDirectory: boolean;
  hasChildren: boolean;
}

export enum ScanPhase {
  Idle = 0,
  EnumeratingFiles = 1,
  BuildingHashes = 2,
  Comparing = 3,
  RetrievingThumbnails = 4,
  Finished = 5
}

export interface ScanStatus {
  isScanning: boolean;
  currentActivity: string;
  progress: number;
  processedFiles: number;
  totalFiles: number;
  duplicatesFound: number;
  phase: ScanPhase;
  phaseDescription: string;
}

export interface RecentFileEntry {
  path: string;
  status: string;
  timestamp: string;
}
