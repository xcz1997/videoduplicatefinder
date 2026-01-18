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
};

export const settings = {
  get: () => api.get('/settings').then(res => res.data),
  save: (data: any) => api.post('/settings', data),
};

export interface FileSystemNode {
  name: string;
  path: string;
  isDirectory: boolean;
  hasChildren: boolean;
}

export interface ScanStatus {
  isScanning: boolean;
  currentActivity: string;
  progress: number;
  processedFiles: number;
  totalFiles: number;
  duplicatesFound: number;
}
