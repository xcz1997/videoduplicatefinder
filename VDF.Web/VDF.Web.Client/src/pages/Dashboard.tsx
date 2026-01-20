import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Card, Statistic, Progress, List, Tag, Row, Col, Space, Typography, Modal, Tooltip, Badge } from 'antd';
import {
  FolderOpenOutlined,
  PlayCircleOutlined,
  StopOutlined,
  PlusOutlined,
  DeleteOutlined,
  FileSearchOutlined,
  ThunderboltOutlined,
  SwapOutlined,
  PictureOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined
} from '@ant-design/icons';
import { useRequest } from 'ahooks';
import { useTranslation } from 'react-i18next';
import { scan, settings, ScanPhase, RecentFileEntry } from '../api';
import { FolderPicker } from '../components/FolderPicker';
import { SystemMonitor } from '../components/SystemMonitor';

const { Text } = Typography;

// Phase display config
const phaseConfig: Record<ScanPhase, { icon: React.ReactNode; color: string; labelKey: string }> = {
  [ScanPhase.Idle]: { icon: <ClockCircleOutlined />, color: '#8c8c8c', labelKey: 'Dashboard.Idle' },
  [ScanPhase.EnumeratingFiles]: { icon: <FileSearchOutlined />, color: '#1677ff', labelKey: 'Dashboard.Phase.Enumerating' },
  [ScanPhase.BuildingHashes]: { icon: <ThunderboltOutlined />, color: '#faad14', labelKey: 'Dashboard.Phase.Hashing' },
  [ScanPhase.Comparing]: { icon: <SwapOutlined />, color: '#eb2f96', labelKey: 'Dashboard.Phase.Comparing' },
  [ScanPhase.RetrievingThumbnails]: { icon: <PictureOutlined />, color: '#722ed1', labelKey: 'Dashboard.Phase.Thumbnails' },
  [ScanPhase.Finished]: { icon: <CheckCircleOutlined />, color: '#52c41a', labelKey: 'Dashboard.Phase.Finished' },
};

const Dashboard: React.FC = () => {
  const [selectedPaths, setSelectedPaths] = useState<string[]>([]);
  const [showFolderPicker, setShowFolderPicker] = useState(false);
  const [showRecentFiles, setShowRecentFiles] = useState(false);
  const [recentFiles, setRecentFiles] = useState<RecentFileEntry[]>([]);
  const { t } = useTranslation();
  const debounceRef = useRef<number | null>(null);

  // Load saved paths on mount
  useEffect(() => {
    settings.get().then((data) => {
      // Handle both camelCase and PascalCase from backend
      const includes = data.Includes || data.includes || [];
      if (includes.length > 0) {
        setSelectedPaths(includes);
      }
    }).catch(() => {});
  }, []);

  // Persist paths when changed (debounced)
  const persistPaths = useCallback((paths: string[]) => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }
    debounceRef.current = window.setTimeout(() => {
      settings.updateIncludes(paths).catch(() => {});
    }, 500);
  }, []);

  const handlePathsChange = useCallback((paths: string[]) => {
    setSelectedPaths(paths);
    persistPaths(paths);
  }, [persistPaths]);

  const handleRemovePath = useCallback((pathToRemove: string) => {
    const newPaths = selectedPaths.filter(p => p !== pathToRemove);
    handlePathsChange(newPaths);
  }, [selectedPaths, handlePathsChange]);

  // Poll status every 1s
  const { data: status, run: refreshStatus } = useRequest(scan.getStatus, {
    pollingInterval: 1000,
    pollingWhenHidden: false,
  });

  // Check if scanning - disable path modification during scan
  const isScanning = status?.isScanning || false;
  const currentPhase: ScanPhase = (status?.phase as ScanPhase) ?? ScanPhase.Idle;
  const phaseInfo = phaseConfig[currentPhase] ?? phaseConfig[ScanPhase.Idle];

  // Fetch recent files when modal is open
  useEffect(() => {
    let interval: number | undefined;
    if (showRecentFiles && isScanning) {
      const fetchRecentFiles = () => {
        scan.getRecentFiles().then(setRecentFiles).catch(() => {});
      };
      fetchRecentFiles();
      interval = window.setInterval(fetchRecentFiles, 1000);
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [showRecentFiles, isScanning]);

  const handleStart = async () => {
    if (selectedPaths.length === 0) return;
    await scan.start(selectedPaths);
    refreshStatus();
  };

  const handleStop = async () => {
    await scan.stop();
    refreshStatus();
  };

  const handleProcessedClick = () => {
    if (isScanning) {
      scan.getRecentFiles().then(setRecentFiles).catch(() => {});
      setShowRecentFiles(true);
    }
  };

  return (
    <>
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={8}>
          <Card variant="borderless">
            <Statistic
              title={t('Dashboard.Status')}
              value={status?.phaseDescription || t('Dashboard.Idle')}
              valueStyle={{ color: phaseInfo.color }}
              prefix={phaseInfo.icon}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card variant="borderless">
            <Statistic title={t('Dashboard.DuplicatesFound')} value={status?.duplicatesFound || 0} prefix={<FolderOpenOutlined />} />
          </Card>
        </Col>
        <Col span={8}>
          <Tooltip title={isScanning ? t('Dashboard.ClickToViewFiles') : undefined}>
            <Card
              variant="borderless"
              style={{ cursor: isScanning ? 'pointer' : 'default' }}
              onClick={handleProcessedClick}
              hoverable={isScanning}
            >
              <Badge dot={isScanning} color={phaseInfo.color}>
                <Statistic
                  title={t('Dashboard.ProcessedFiles')}
                  value={status?.processedFiles || 0}
                  suffix={`/ ${status?.totalFiles || 0}`}
                />
              </Badge>
            </Card>
          </Tooltip>
        </Col>
      </Row>

      <SystemMonitor />

      <Card title={t('Settings.Tab.Scanner')} variant="borderless" style={{ marginBottom: 24 }}>
        <div style={{ marginBottom: 20 }}>
           <Text strong>{t('Settings.SearchDirs')}: </Text>
           <Button
             type="link"
             icon={<PlusOutlined />}
             onClick={() => setShowFolderPicker(true)}
             disabled={isScanning}
           >
             {t('Settings.Add')}
           </Button>
           <List
             size="small"
             dataSource={selectedPaths}
             locale={{ emptyText: t('Settings.NoFolders') || 'No folders added' }}
             renderItem={(item) => (
               <List.Item
                 actions={!isScanning ? [
                   <Button
                     type="text"
                     danger
                     size="small"
                     icon={<DeleteOutlined />}
                     onClick={() => handleRemovePath(item)}
                   />
                 ] : undefined}
               >
                 <Tag color="blue">{item}</Tag>
               </List.Item>
             )}
           />
        </div>

        <Space size="large" style={{ width: '100%', marginBottom: 24 }}>
           <Button type="primary" size="large" icon={<PlayCircleOutlined />} onClick={handleStart} loading={status?.isScanning}>
             {t('Toolbar.Scan')}
           </Button>
           <Button danger size="large" icon={<StopOutlined />} onClick={handleStop} disabled={!status?.isScanning}>
             {t('Toolbar.Stop')}
           </Button>
        </Space>

        {status?.isScanning && (
            <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, alignItems: 'flex-start' }}>
                    <Space direction="vertical" size={4} style={{ flex: 1, minWidth: 0 }}>
                      <Space>
                        <Tag color={phaseInfo.color} icon={phaseInfo.icon}>
                          {status.phaseDescription}
                        </Tag>
                        <Text type="secondary">{Math.round(status.progress)}%</Text>
                      </Space>
                      <Text
                        type="secondary"
                        style={{
                          wordBreak: 'break-all',
                          fontSize: 12,
                          lineHeight: 1.4,
                          display: 'block'
                        }}
                        title={status.currentActivity}
                      >
                        {status.currentActivity}
                      </Text>
                    </Space>
                </div>
                <Progress percent={status.progress} status="active" showInfo={false} strokeColor={phaseInfo.color} />
            </div>
        )}
      </Card>

      <FolderPicker
        open={showFolderPicker}
        value={selectedPaths}
        onCancel={() => setShowFolderPicker(false)}
        onChange={(vals) => {
            handlePathsChange(vals);
            setShowFolderPicker(false);
        }}
      />

      <Modal
        title={t('Dashboard.RecentFiles')}
        open={showRecentFiles}
        onCancel={() => setShowRecentFiles(false)}
        footer={null}
        width="80%"
        style={{ maxWidth: 1200 }}
      >
        <List
          size="small"
          dataSource={recentFiles.slice().reverse()}
          style={{ maxHeight: 500, overflow: 'auto' }}
          renderItem={(item) => (
            <List.Item style={{ padding: '8px 0' }}>
              <div style={{ width: '100%', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12 }}>
                <Text
                  style={{
                    flex: 1,
                    wordBreak: 'break-all',
                    fontSize: 13,
                    lineHeight: 1.4,
                    fontFamily: 'monospace'
                  }}
                  title={item.path}
                >
                  {item.path}
                </Text>
                <Tag
                  color={item.status === 'Hashing' ? 'orange' : item.status === 'Comparing' ? 'pink' : 'blue'}
                  style={{ flexShrink: 0 }}
                >
                  {item.status}
                </Tag>
              </div>
            </List.Item>
          )}
        />
      </Modal>
    </>
  );
};

export default Dashboard;
