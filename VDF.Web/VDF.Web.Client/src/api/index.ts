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
  deleteFiles: (paths: string[], permanently?: boolean) =>
    api.post('/scan/delete', { paths, permanently: permanently ?? false }).then(res => res.data),
  saveResults: () => api.post('/scan/save-results').then(res => res.data),
};

export const trash = {
  getItems: () => api.get('/trash').then(res => res.data),
  getItem: (id: string) => api.get(`/trash/${id}`).then(res => res.data),
  restore: (id: string, overwriteExisting?: boolean) =>
    api.post(`/trash/${id}/restore`, { overwriteExisting: overwriteExisting ?? false }).then(res => res.data),
  restoreBatch: (ids: string[], overwriteExisting?: boolean) =>
    api.post('/trash/restore', { ids, overwriteExisting: overwriteExisting ?? false }).then(res => res.data),
  delete: (id: string) => api.delete(`/trash/${id}`).then(res => res.data),
  empty: () => api.delete('/trash').then(res => res.data),
  cleanup: (days?: number) => api.post('/trash/cleanup', { days }).then(res => res.data),
};

export const history = {
  getList: () => api.get('/history').then(res => res.data),
  getDetails: (scanId: string) => api.get(`/history/${scanId}`).then(res => res.data),
  getDeletions: (scanId: string) => api.get(`/history/${scanId}/deletions`).then(res => res.data),
  delete: (scanId: string) => api.delete(`/history/${scanId}`).then(res => res.data),
  cleanup: (days?: number) => api.post('/history/cleanup', { days }).then(res => res.data),
  getByDateRange: (from: string, to: string) =>
    api.get('/history/bydate', { params: { from, to } }).then(res => res.data),
  search: (folder: string) => api.get('/history/search', { params: { folder } }).then(res => res.data),
};

export interface TrashItem {
  id: string;
  fileName: string;
  originalPath: string;
  fileSize: number;
  sizeDisplay: string;
  deletedAt: string;
  deletedAgo: string;
  scanId?: string;
  groupId?: string;
  fileExists: boolean;
  canRestore: boolean;
}

export interface TrashListResponse {
  items: TrashItem[];
  totalSize: number;
  itemCount: number;
}

export interface HistorySummary {
  scanId: string;
  timestamp: string;
  folders: string[];
  duplicateGroups: number;
  duplicateItems: number;
  totalDuplicateSize: number;
  deletedCount: number;
  deletedSize: number;
}

export interface HistoryListResponse {
  entries: HistorySummary[];
  totalCount: number;
}

export const settings = {
  get: () => api.get('/settings').then(res => res.data),
  save: (data: any) => api.post('/settings', data),
  getCacheInfo: () => api.get('/settings/cache-info').then(res => res.data),
  clearCache: () => api.post('/settings/clear-cache').then(res => res.data),
  updateIncludes: (paths: string[]) => api.patch('/settings/includes', paths).then(res => res.data),
  getDeletePolicy: () => api.get('/settings/delete-policy').then(res => res.data),
  updateDeletePolicy: (data: DeletePolicySettings) => api.put('/settings/delete-policy', data).then(res => res.data),
  getDeleteActions: () => api.get('/settings/delete-actions').then(res => res.data),
  getPathsInfo: () => api.get('/settings/paths-info').then(res => res.data),
};

export interface DeletePolicySettings {
  defaultDeleteAction: number;
  trashFolderPath: string;
  trashFolderRelativeToScan: boolean;
  autoExcludeTrashFolder: boolean;
  trashRetentionDays: number;
  enableScanHistory: boolean;
  maxHistoryDays: number;
  saveThumbnailsInHistory: boolean;
  historyFolderPath: string;
}

export interface DeleteActionOption {
  value: number;
  name: string;
  description: string;
}

export interface CacheInfo {
  cacheSize: number;
  cacheSizeFormatted: string;
  cacheFolder: string;
  defaultCacheFolder: string;
}

export interface PathInfo {
  configuredPath: string;
  resolvedPath: string;
  size: number;
  sizeFormatted: string;
}

export interface PathsInfoResponse {
  dataFolder: PathInfo;
  databaseFolder: PathInfo;
  cacheFolder: PathInfo;
  historyFolder: PathInfo;
  trashFolder: PathInfo;
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
