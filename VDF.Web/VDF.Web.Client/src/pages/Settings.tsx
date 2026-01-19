import React, { useState } from 'react';
import { Form, InputNumber, Switch, Button, Card, message, Select, List, Typography, Space, Input, Row, Col, Tooltip } from 'antd';
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
  SaveOutlined
} from '@ant-design/icons';
import { useRequest } from 'ahooks';
import { useTranslation } from 'react-i18next';
import { settings } from '../api';
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

  const { data, loading } = useRequest(settings.get, {
    onSuccess: (res) => {
      form.setFieldsValue(res);
      setIncludes(res.Includes || []);
      setBlacklists(res.Blacklists || []);
    }
  });

  const onFinish = async (values: any) => {
    try {
      const toSave = {
        ...data,
        ...values,
        Includes: includes,
        Blacklists: blacklists
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

      <Form
        
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
