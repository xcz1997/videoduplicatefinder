import React, { useState, useEffect } from 'react';
import { Form, InputNumber, Switch, Button, Card, message, Select, List, Typography, Space, Input, Row, Col, Tooltip, Statistic, Checkbox, Alert, Tag } from 'antd';
import {
  DeleteOutlined,
  PlusOutlined,
  QuestionCircleOutlined,
  FolderOutlined,
  SettingOutlined,
  ThunderboltOutlined,
  ControlOutlined,
  ExperimentOutlined,
  ToolOutlined,
  SaveOutlined,
  ClearOutlined,
  DatabaseOutlined,
  AppstoreOutlined,
  BulbOutlined
} from '@ant-design/icons';

// Media server template definitions
const MEDIA_TEMPLATES = {
  jellyfin: {
    id: 'jellyfin',
    patterns: [
      '*.nfo', 'poster.*', '*-poster.*', 'cover.*', 'folder.*', 'default.*', 'movie.*',
      'backdrop.*', 'backdrop?.*', 'backdrop-?.*', 'fanart.*', 'background.*', 'art.*',
      'banner.*', '*-banner.*', 'logo.*', 'clearlogo.*', '*-logo.*', '*-clearlogo.*',
      'thumb.*', '*-thumb.*', 'landscape.*', '*-landscape.*', 'clearart.*', '*-clearart.*',
      'disc.*', 'cdart.*', '*-disc.*', '*-cdart.*', 'season??.*', 'season??-*.*',
      'season-specials*.*', 'theme.*', 'theme-music/*', 'extrafanart/*'
    ]
  },
  emby: {
    id: 'emby',
    patterns: [
      '*.nfo', 'poster.*', '*-poster.*', '*-cover.*', 'folder.*',
      'backdrop.*', 'backdrop?.*', 'fanart.*', 'background.*', 'art.*',
      'clearart.*', '*-clearart.*', 'banner.*', '*-banner.*',
      'logo.*', 'clearlogo.*', '*-logo.*', '*-clearlogo.*',
      'disc.*', 'cdart.*', '*-disc.*', '*-cdart.*',
      'thumb.*', '*-thumb.*', 'landscape.*', '*-landscape.*',
      'season??-poster.*', 'season??-fanart.*', 'season??-banner.*', 'season??-landscape.*',
      'season-specials-poster.*', 'season-specials-fanart.*', 'season-specials-banner.*'
    ]
  },
  plex: {
    id: 'plex',
    patterns: [
      'poster.*', 'poster-?.*', 'cover.*', 'default.*', 'folder.*', 'movie.*',
      '*-fanart.*', 'art.*', 'backdrop.*', 'background.*', 'fanart.*',
      'logo.*', 'clearlogo.*', 'logo-?.*', 'clearlogo-?.*',
      'show.*', 'show-?.*', 'season??.*', 'season???.*',
      'square.*', 'squareArt.*', 'backgroundSquare.*'
    ]
  },
  kodi: {
    id: 'kodi',
    patterns: [
      '*.nfo', 'poster.*', '*-poster.*', 'folder.*',
      'fanart.*', '*-fanart.*', 'banner.*', '*-banner.*',
      'clearart.*', '*-clearart.*', 'clearlogo.*', '*-clearlogo.*',
      'landscape.*', '*-landscape.*', 'disc.*', 'cdart.*',
      'thumb.*', '*-thumb.*', 'extrafanart/*', 'extrathumbs/*'
    ]
  },
  subtitles: {
    id: 'subtitles',
    patterns: ['*.srt', '*.sub', '*.ass', '*.ssa', '*.idx', '*.vtt', '*.sup', '*.lrc']
  },
  trailers: {
    id: 'trailers',
    patterns: [
      '*-trailer.*', '*-trailer?.*', '*-Trailer.*', '*-Trailer?.*',
      'trailer.*', 'trailer?.*', 'trailers/*',
      '*-featurette.*', '*-featurette?.*', '*-behindthescenes.*',
      '*-deleted.*', '*-interview.*', '*-scene.*', '*-short.*', '*-other.*'
    ]
  }
} as const;
import { useRequest } from 'ahooks';
import { useTranslation } from 'react-i18next';
import { settings, scan, CacheInfo, monitor, HwAccelRecommendation, MonitorMethodInfo } from '../api';
import { FolderPicker } from '../components/FolderPicker';

const { Option } = Select;
const { Text, Title } = Typography;

// Helper component for label with tooltip
const LabelWithTooltip: React.FC<{ label: string; tooltip?: string }> = ({ label, tooltip }) => {
  if (!tooltip) return <span className="label-text">{label}</span>;
  return (
    <Space size={6}>
      <span className="label-text">{label}</span>
      <Tooltip title={tooltip}>
        <QuestionCircleOutlined style={{ color: '#6750A4', cursor: 'help', fontSize: 14 }} />
      </Tooltip>
    </Space>
  );
};

// Card Title with Icon
const CardTitle: React.FC<{ icon: React.ReactNode; iconClass: string; title: string }> = ({ icon, iconClass, title }) => (
  <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
    <div className={`card-icon ${iconClass}`}>{icon}</div>
    <span>{title}</span>
  </div>
);

// Switch Item Component
const SwitchItem: React.FC<{
  name: string;
  label: string;
  tooltip?: string;
}> = ({ name, label, tooltip }) => (
  <div className="switch-item">
    <LabelWithTooltip label={label} tooltip={tooltip} />
    <Form.Item name={name} valuePropName="checked" noStyle>
      <Switch />
    </Form.Item>
  </div>
);

const Settings: React.FC = () => {
  const [form] = Form.useForm();
  const { t } = useTranslation();

  // State for folder lists
  const [includes, setIncludes] = useState<string[]>([]);
  const [blacklists, setBlacklists] = useState<string[]>([]);
  const [showIncludePicker, setShowIncludePicker] = useState(false);
  const [showExcludePicker, setShowExcludePicker] = useState(false);

  // State for media server templates
  const [selectedTemplates, setSelectedTemplates] = useState<string[]>([]);
  const [filePathNotContainsTexts, setFilePathNotContainsTexts] = useState<string[]>([]);

  // Cache info state
  const [cacheInfo, setCacheInfo] = useState<CacheInfo | null>(null);
  const [clearingCache, setClearingCache] = useState(false);

  // Database operations state
  const [clearingDatabase, setClearingDatabase] = useState(false);
  const [cleaningDatabase, setCleaningDatabase] = useState(false);

  // Hardware acceleration recommendation
  const [hwRecommendation, setHwRecommendation] = useState<HwAccelRecommendation | null>(null);

  // GPU monitoring methods
  const [monitorMethods, setMonitorMethods] = useState<MonitorMethodInfo[]>([]);
  const [currentMonitorMethod, setCurrentMonitorMethod] = useState<string>('auto');

  // Load cache info
  const loadCacheInfo = async () => {
    try {
      const info = await settings.getCacheInfo();
      setCacheInfo(info);
    } catch (e) {
      console.error('Failed to load cache info', e);
    }
  };

  // Load hardware acceleration recommendation
  const loadHwRecommendation = async () => {
    try {
      const rec = await monitor.getRecommendation();
      setHwRecommendation(rec);
    } catch (e) {
      console.error('Failed to load hw recommendation', e);
    }
  };

  // Load GPU monitor methods
  const loadMonitorMethods = async () => {
    try {
      const res = await monitor.getMonitorMethods();
      setMonitorMethods(res.methods || []);
      setCurrentMonitorMethod(res.currentMethod || 'auto');
    } catch (e) {
      console.error('Failed to load monitor methods', e);
    }
  };

  // Handle monitor method change
  const handleMonitorMethodChange = async (method: string) => {
    try {
      await monitor.setMonitorMethod(method);
      setCurrentMonitorMethod(method);
      message.success(t('Settings.MonitorMethodChanged') || 'GPU monitor method updated');
    } catch (e) {
      message.error(t('Settings.MonitorMethodChangeFailed') || 'Failed to change monitor method');
    }
  };

  useEffect(() => {
    loadCacheInfo();
    loadHwRecommendation();
    loadMonitorMethods();
  }, []);

  const handleClearCache = async () => {
    setClearingCache(true);
    try {
      await settings.clearCache();
      message.success(t('Settings.CacheClearedSuccess') || 'Thumbnail cache cleared successfully');
      await loadCacheInfo();
    } catch (e) {
      message.error(t('Settings.CacheClearedFailed') || 'Failed to clear cache');
    } finally {
      setClearingCache(false);
    }
  };

  const handleClearDatabase = async () => {
    setClearingDatabase(true);
    try {
      await scan.clearDatabase();
      message.success(t('Settings.DatabaseClearedSuccess') || 'Database cleared successfully');
    } catch (e: any) {
      message.error(e?.response?.data || t('Settings.DatabaseClearedFailed') || 'Failed to clear database');
    } finally {
      setClearingDatabase(false);
    }
  };

  const handleCleanupDatabase = async () => {
    setCleaningDatabase(true);
    try {
      await scan.cleanupDatabase();
      message.success(t('Settings.DatabaseCleanupSuccess') || 'Database cleanup started');
    } catch (e: any) {
      message.error(e?.response?.data || t('Settings.DatabaseCleanupFailed') || 'Failed to cleanup database');
    } finally {
      setCleaningDatabase(false);
    }
  };

  const { data, loading, error } = useRequest(settings.get, {
    onSuccess: (res) => {
      form.setFieldsValue(res);
      setIncludes(res.includes || res.Includes || []);
      setBlacklists(res.blacklists || res.Blacklists || []);
      setSelectedTemplates(res.selectedMediaTemplates || res.SelectedMediaTemplates || []);
      setFilePathNotContainsTexts(res.filePathNotContainsTexts || res.FilePathNotContainsTexts || []);
    },
    onError: (e) => {
      console.error('Failed to load settings:', e);
      message.error(t('Settings.LoadFailed') || 'Failed to load settings. Is the backend running?');
    }
  });

  // Handle template toggle
  const handleTemplateToggle = (templateId: string, checked: boolean) => {
    const template = MEDIA_TEMPLATES[templateId as keyof typeof MEDIA_TEMPLATES];
    if (!template) return;

    if (checked) {
      // Add template and its patterns
      setSelectedTemplates(prev => prev.includes(templateId) ? prev : [...prev, templateId]);
      setFilePathNotContainsTexts(prev => {
        const newPatterns = (template.patterns as readonly string[]).filter(p => !prev.includes(p));
        return [...prev, ...newPatterns];
      });
      // Enable the filter
      form.setFieldValue('FilterByFilePathNotContains', true);
    } else {
      // Remove template and its patterns
      setSelectedTemplates(prev => prev.filter(id => id !== templateId));
      setFilePathNotContainsTexts(prev => prev.filter(p => !(template.patterns as readonly string[]).includes(p)));
    }
  };

  const onFinish = async (values: any) => {
    try {
      const toSave = {
        ...data,
        ...values,
        Includes: includes,
        Blacklists: blacklists,
        SelectedMediaTemplates: selectedTemplates,
        FilePathNotContainsTexts: filePathNotContainsTexts
      };
      await settings.save(toSave);
      message.success(t('Settings.SaveSuccess') || 'Settings saved successfully');
    } catch (e) {
      message.error(t('Settings.SaveFailed') || 'Failed to save settings');
    }
  };

  const removeItem = (list: string[], setList: (v: string[]) => void, item: string) => {
    setList(list.filter(i => i !== item));
  };

  return (
    <div className="settings-container">
      {/* Page Header */}
      <div className="settings-header">
        <Title level={2} style={{ margin: 0, marginBottom: 8 }}>{t('Settings.Tab.Settings')}</Title>
        <Text type="secondary">{t('Settings.Hint')}</Text>
      </div>

      {error && (
        <Alert
          type="error"
          message={t('Settings.ConnectionError') || 'Connection Error'}
          description={t('Settings.BackendNotRunning') || 'Unable to connect to backend. Please make sure the server is running.'}
          showIcon
          style={{ marginBottom: 24 }}
        />
      )}

      <Form
        form={form}
        layout="vertical"
        onFinish={onFinish}
        initialValues={{
          Percent: 95,
          Thumbnails: 2,
          MaxDegreeOfParallelism: -1,
          HardwareAccelerationMode: 1
        }}
      >
        <div className="settings-grid">
          {/* Scanner Card - Folders */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<FolderOutlined />} iconClass="scanner" title={t('Settings.Tab.Scanner')} />}
            loading={loading}
          >
            {/* Include Folders */}
            <div style={{ marginBottom: 24 }}>
              <div className="section-header">
                <span className="section-title">{t('Settings.SearchDirs')}</span>
                <Button
                  type="primary"
                  ghost
                  size="small"
                  icon={<PlusOutlined />}
                  onClick={() => setShowIncludePicker(true)}
                  style={{ borderRadius: 20 }}
                >
                  {t('Settings.Add')}
                </Button>
              </div>
              <List
                className="folder-list"
                size="small"
                locale={{ emptyText: t('Settings.NoFolders') || 'No folders added' }}
                dataSource={includes}
                renderItem={item => (
                  <List.Item
                    actions={[
                      <Button
                        type="text"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => removeItem(includes, setIncludes, item)}
                      />
                    ]}
                  >
                    <Text ellipsis style={{ maxWidth: '100%' }}>{item}</Text>
                  </List.Item>
                )}
              />
            </div>

            {/* Exclude Folders */}
            <div>
              <div className="section-header">
                <span className="section-title">{t('Settings.ExcludeDirs')}</span>
                <Button
                  type="primary"
                  ghost
                  size="small"
                  icon={<PlusOutlined />}
                  onClick={() => setShowExcludePicker(true)}
                  style={{ borderRadius: 20 }}
                >
                  {t('Settings.Add')}
                </Button>
              </div>
              <List
                className="folder-list"
                size="small"
                locale={{ emptyText: t('Settings.NoFolders') || 'No folders added' }}
                dataSource={blacklists}
                renderItem={item => (
                  <List.Item
                    actions={[
                      <Button
                        type="text"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => removeItem(blacklists, setBlacklists, item)}
                      />
                    ]}
                  >
                    <Text ellipsis style={{ maxWidth: '100%' }}>{item}</Text>
                  </List.Item>
                )}
              />
            </div>
          </Card>

          {/* Media Server Templates Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<AppstoreOutlined />} iconClass="templates" title={t('Settings.Templates') || 'Templates'} />}
            loading={loading}
          >
            <div style={{ marginBottom: 16 }}>
              <Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
                {t('Settings.TemplatesDesc') || 'Select media server templates to exclude metadata files from scanning'}
              </Text>
              <Row gutter={[16, 12]}>
                <Col xs={12} sm={8}>
                  <Checkbox
                    checked={selectedTemplates.includes('jellyfin')}
                    onChange={e => handleTemplateToggle('jellyfin', e.target.checked)}
                  >
                    <Tooltip title={t('Settings.Template.Jellyfin.Desc')}>
                      {t('Settings.Template.Jellyfin') || 'Jellyfin'}
                    </Tooltip>
                  </Checkbox>
                </Col>
                <Col xs={12} sm={8}>
                  <Checkbox
                    checked={selectedTemplates.includes('emby')}
                    onChange={e => handleTemplateToggle('emby', e.target.checked)}
                  >
                    <Tooltip title={t('Settings.Template.Emby.Desc')}>
                      {t('Settings.Template.Emby') || 'Emby'}
                    </Tooltip>
                  </Checkbox>
                </Col>
                <Col xs={12} sm={8}>
                  <Checkbox
                    checked={selectedTemplates.includes('plex')}
                    onChange={e => handleTemplateToggle('plex', e.target.checked)}
                  >
                    <Tooltip title={t('Settings.Template.Plex.Desc')}>
                      {t('Settings.Template.Plex') || 'Plex'}
                    </Tooltip>
                  </Checkbox>
                </Col>
                <Col xs={12} sm={8}>
                  <Checkbox
                    checked={selectedTemplates.includes('kodi')}
                    onChange={e => handleTemplateToggle('kodi', e.target.checked)}
                  >
                    <Tooltip title={t('Settings.Template.Kodi.Desc')}>
                      {t('Settings.Template.Kodi') || 'Kodi'}
                    </Tooltip>
                  </Checkbox>
                </Col>
                <Col xs={12} sm={8}>
                  <Checkbox
                    checked={selectedTemplates.includes('subtitles')}
                    onChange={e => handleTemplateToggle('subtitles', e.target.checked)}
                  >
                    <Tooltip title={t('Settings.Template.Subtitles.Desc')}>
                      {t('Settings.Template.Subtitles') || 'Subtitles'}
                    </Tooltip>
                  </Checkbox>
                </Col>
                <Col xs={12} sm={8}>
                  <Checkbox
                    checked={selectedTemplates.includes('trailers')}
                    onChange={e => handleTemplateToggle('trailers', e.target.checked)}
                  >
                    <Tooltip title={t('Settings.Template.Trailers.Desc')}>
                      {t('Settings.Template.Trailers') || 'Trailers'}
                    </Tooltip>
                  </Checkbox>
                </Col>
              </Row>
            </div>
            {filePathNotContainsTexts.length > 0 && (
              <div>
                <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                  {t('Settings.ActiveExclusions') || 'Active exclusion patterns'}: {filePathNotContainsTexts.length}
                </Text>
                <div style={{ maxHeight: 100, overflow: 'auto', padding: 8, background: 'rgba(0,0,0,0.05)', borderRadius: 4, fontSize: 12 }}>
                  {filePathNotContainsTexts.join(', ')}
                </div>
              </div>
            )}
          </Card>

          {/* Misc Settings Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<SettingOutlined />} iconClass="misc" title={t('Settings.Tab.Misc')} />}
            loading={loading}
          >
            <div className="switch-group">
              <SwitchItem name="IncludeSubDirectories" label={t('Settings.IncludeSubDirs')} />
              <SwitchItem name="IncludeImages" label={t('Settings.IncludeImages')} />
              <SwitchItem name="IgnoreReadOnlyFolders" label={t('Settings.IgnoreReadOnly')} />
              <SwitchItem
                
                name="GeneratePreviewThumbnails"
                label={t('Settings.GeneratePreviews')}
                tooltip={t('ToolTip.Settings.GeneratePreviews')}
              />
              <SwitchItem
                
                name="IgnoreReparsePoints"
                label={t('Settings.ExcludeReparse')}
                tooltip={t('ToolTip.Settings.ExcludeReparse')}
              />
              <SwitchItem
                
                name="ExcludeHardLinks"
                label={t('Settings.ExcludeHardLinks')}
                tooltip={t('ToolTip.Settings.ExcludeHardLinks')}
              />
              <SwitchItem
                
                name="IncludeNonExistingFiles"
                label={t('Settings.IncludeNonExisting')}
                tooltip={t('ToolTip.Settings.IncludeNonExisting')}
              />
              <SwitchItem
                
                name="ScanAgainstEntireDatabase"
                label={t('Settings.ScanAgainstDb')}
                tooltip={t('ToolTip.Settings.ScanAgainstDb')}
              />
            </div>
          </Card>

          {/* Matching Settings Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<ControlOutlined />} iconClass="matching" title={t('Settings.FilesNote')} />}
            loading={loading}
          >
            <div className="input-group">
              <Form.Item
                label={<LabelWithTooltip label={t('Settings.Percent')} tooltip={t('ToolTip.Settings.Percent')} />}
                name="Percent"
              >
                <InputNumber min={1} max={100} style={{ width: '100%' }} addonAfter="%" />
              </Form.Item>
              <Form.Item
                label={<LabelWithTooltip label={t('Settings.DurationDiff')} tooltip={t('ToolTip.Settings.DurationDiff')} />}
                name="PercentDurationDifference"
              >
                <InputNumber min={0} max={100} style={{ width: '100%' }} addonAfter="%" />
              </Form.Item>
              <Form.Item
                label={<LabelWithTooltip label={t('Settings.Thumbnails')} tooltip={t('ToolTip.Settings.Thumbnails')} />}
                name="Thumbnails"
              >
                <InputNumber min={1} max={100} style={{ width: '100%' }} />
              </Form.Item>
            </div>
          </Card>

          {/* Performance Settings Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<ThunderboltOutlined />} iconClass="performance" title={t('Settings.Performance') || 'Performance'} />}
            loading={loading}
          >
            <Row gutter={[20, 20]}>
              <Col xs={24} sm={12}>
                <Form.Item
                  label={<LabelWithTooltip label={t('Settings.Parallelism')} tooltip={t('ToolTip.Settings.Parallelism')} />}
                  name="MaxDegreeOfParallelism"
                >
                  <InputNumber min={-1} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12}>
                <Form.Item
                  label={<LabelWithTooltip label={t('Settings.HWAccel')} tooltip={t('ToolTip.Settings.HWAccel')} />}
                  name="HardwareAccelerationMode"
                >
                  <Select>
                    <Option value={0}>None</Option>
                    <Option value={1}>Auto</Option>
                    <Option value={2}>VDPAU (Linux)</Option>
                    <Option value={3}>DXVA2 (Windows)</Option>
                    <Option value={4}>VAAPI (Linux)</Option>
                    <Option value={5}>QSV (Intel)</Option>
                    <Option value={6}>CUDA (NVIDIA)</Option>
                    <Option value={7}>VideoToolbox (macOS)</Option>
                    <Option value={8}>D3D11VA (Windows)</Option>
                    <Option value={9}>DRM (Linux)</Option>
                    <Option value={10}>MediaCodec (Android)</Option>
                    <Option value={11}>Vulkan</Option>
                  </Select>
                </Form.Item>
              </Col>
            </Row>
            {hwRecommendation && hwRecommendation.mode !== 1 && (
              <Alert
                type="info"
                showIcon
                icon={<BulbOutlined />}
                message={
                  <Space>
                    <Text>{t('Settings.RecommendedHWAccel') || 'Recommended'}:</Text>
                    <Text strong>{hwRecommendation.modeName}</Text>
                    <Text type="secondary">({hwRecommendation.gpuName})</Text>
                  </Space>
                }
                description={hwRecommendation.reason}
                style={{ marginTop: 16 }}
                action={
                  <Button
                    size="small"
                    type="primary"
                    onClick={() => form.setFieldValue('HardwareAccelerationMode', hwRecommendation.mode)}
                  >
                    {t('Settings.Apply') || 'Apply'}
                  </Button>
                }
              />
            )}

            {/* GPU Monitor Method */}
            {monitorMethods.length > 0 && (
              <div style={{ marginTop: 20 }}>
                <Text strong style={{ display: 'block', marginBottom: 8 }}>
                  {t('Settings.GpuMonitorMethod') || 'GPU Monitor Method'}
                </Text>
                <Select
                  style={{ width: '100%' }}
                  value={currentMonitorMethod}
                  onChange={handleMonitorMethodChange}
                >
                  {monitorMethods.map(method => (
                    <Option
                      key={method.id}
                      value={method.id}
                      disabled={!method.isAvailable}
                    >
                      <Space>
                        <span>{method.name}</span>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          ({method.description})
                        </Text>
                        {!method.isAvailable && method.dependency && (
                          <Tag color="orange" style={{ fontSize: 10 }}>
                            {method.dependency}
                          </Tag>
                        )}
                      </Space>
                    </Option>
                  ))}
                </Select>
              </div>
            )}
          </Card>

          {/* Advanced Settings Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<ExperimentOutlined />} iconClass="advanced" title={t('Settings.Advanced') || 'Advanced'} />}
            loading={loading}
          >
            <div className="switch-group">
              <SwitchItem
                
                name="UsePHash"
                label={t('Settings.UsePHash')}
                tooltip={t('ToolTip.Settings.UsePHash')}
              />
              <SwitchItem
                
                name="UseExifCreationDate"
                label={t('Settings.UseExif')}
                tooltip={t('ToolTip.Settings.UseExif')}
              />
              <SwitchItem
                
                name="IgnoreBlackPixels"
                label={t('Settings.IgnoreBlack')}
                tooltip={t('ToolTip.Settings.IgnoreBlack')}
              />
              <SwitchItem
                
                name="IgnoreWhitePixels"
                label={t('Settings.IgnoreWhite')}
                tooltip={t('ToolTip.Settings.IgnoreWhite')}
              />
              <SwitchItem
                
                name="UseNativeFfmpegBinding"
                label={t('Settings.NativeFFmpeg')}
                tooltip={t('ToolTip.Settings.NativeFFmpeg')}
              />
              <SwitchItem
                
                name="CompareHorizontallyFlipped"
                label={t('Settings.CompareFlipped')}
                tooltip={t('ToolTip.Settings.CompareFlipped')}
              />
              <SwitchItem
                
                name="ExtendedFFToolsLogging"
                label={t('Settings.ExtendedLogging')}
                tooltip={t('ToolTip.Settings.ExtendedLogging')}
              />
              <SwitchItem
                
                name="AlwaysRetryFailedSampling"
                label={t('Settings.RetrySampling')}
                tooltip={t('ToolTip.Settings.RetrySampling')}
              />
              <SwitchItem
                
                name="BackupAfterListChanged"
                label={t('Settings.AutoBackup')}
                tooltip={t('ToolTip.Settings.AutoBackup')}
              />
              <SwitchItem
                
                name="AskToSaveResultsOnExit"
                label={t('Settings.SaveOnExit')}
                tooltip={t('ToolTip.Settings.SaveOnExit')}
              />
            </div>
          </Card>

          {/* Custom Settings Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<ToolOutlined />} iconClass="custom" title={t('Settings.Custom') || 'Custom'} />}
            loading={loading}
          >
            <Form.Item
              label={<LabelWithTooltip label={t('Settings.CustomFFArgs')} tooltip={t('ToolTip.Settings.CustomFFArgs')} />}
              name="CustomFFArguments"
            >
              <Input placeholder="e.g. -hwaccel cuda" />
            </Form.Item>

            <Form.Item
              label={<LabelWithTooltip label={t('Settings.CustomDbFolder')} tooltip={t('ToolTip.Settings.CustomDbFolder')} />}
              name="CustomDatabaseFolder"
            >
              <Input placeholder={t('Settings.DefaultLocation') || 'Leave empty for default location'} />
            </Form.Item>
          </Card>

          {/* Thumbnail Cache Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<DatabaseOutlined />} iconClass="cache" title={t('Settings.ThumbnailCache') || 'Thumbnail Cache'} />}
            loading={loading}
          >
            <Form.Item
              label={<LabelWithTooltip label={t('Settings.ThumbnailCacheFolder')} tooltip={t('ToolTip.Settings.ThumbnailCacheFolder')} />}
              name="ThumbnailCacheFolder"
            >
              <Input placeholder={cacheInfo?.defaultCacheFolder || t('Settings.DefaultLocation') || 'Leave empty for default location'} />
            </Form.Item>

            <div style={{ marginBottom: 16 }}>
              <Space size="large">
                <Statistic
                  title={t('Settings.CacheSize') || 'Cache Size'}
                  value={cacheInfo?.cacheSizeFormatted || '0 B'}
                  prefix={<DatabaseOutlined />}
                />
                <Button
                  type="primary"
                  danger
                  icon={<ClearOutlined />}
                  onClick={handleClearCache}
                  loading={clearingCache}
                >
                  {t('Settings.ClearThumbnailCache') || 'Clear Cache'}
                </Button>
              </Space>
            </div>

            {cacheInfo && (
              <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                {t('Settings.CacheLocation') || 'Cache Location'}: {cacheInfo.cacheFolder}
              </Text>
            )}
          </Card>

          {/* Database Management Card */}
          <Card
            className="settings-card"
            title={<CardTitle icon={<DatabaseOutlined />} iconClass="advanced" title={t('Settings.DatabaseManagement') || 'Database Management'} />}
            loading={loading}
          >
            <Alert
              message={t('Settings.DatabaseWarning') || 'Warning: These operations cannot be undone'}
              description={t('Settings.DatabaseWarningDesc') || 'Clear Database will remove all scanned file records. Cleanup Database will remove records for files that no longer exist.'}
              type="warning"
              showIcon
              style={{ marginBottom: 16 }}
            />
            <Space size="middle" wrap>
              <Button
                danger
                icon={<DeleteOutlined />}
                onClick={handleClearDatabase}
                loading={clearingDatabase}
              >
                {t('Settings.ClearDatabase') || 'Clear Database'}
              </Button>
              <Button
                icon={<ClearOutlined />}
                onClick={handleCleanupDatabase}
                loading={cleaningDatabase}
              >
                {t('Settings.CleanupDatabase') || 'Cleanup Database'}
              </Button>
            </Space>
            <div style={{ marginTop: 12 }}>
              <Text type="secondary" style={{ fontSize: 12 }}>
                {t('Settings.DatabaseHint') || 'Use "Clear Database" to resolve issues with duplicate mount points (e.g., SMB vs NFS paths pointing to the same files).'}
              </Text>
            </div>
          </Card>
        </div>

        {/* Save Button */}
        <div style={{ marginTop: 32, textAlign: 'center' }}>
          <Button
            type="primary"
            htmlType="submit"
            className="save-button"
            icon={<SaveOutlined />}
            size="large"
          >
            {t('Settings.Save')}
          </Button>
        </div>
      </Form>

      {/* Folder Pickers */}
      <FolderPicker
        open={showIncludePicker}
        value={includes}
        onCancel={() => setShowIncludePicker(false)}
        onChange={(vals) => {
          setIncludes(prev => Array.from(new Set([...prev, ...vals])));
          setShowIncludePicker(false);
        }}
      />

      <FolderPicker
        open={showExcludePicker}
        value={blacklists}
        onCancel={() => setShowExcludePicker(false)}
        onChange={(vals) => {
          setBlacklists(prev => Array.from(new Set([...prev, ...vals])));
          setShowExcludePicker(false);
        }}
      />
    </div>
  );
};

export default Settings;
