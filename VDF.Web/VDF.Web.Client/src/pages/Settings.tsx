import React, { useState } from 'react';
import { Form, InputNumber, Switch, Button, Card, message, Divider, Select, List, Typography, Space, Input, Row, Col, Tooltip } from 'antd';
import { DeleteOutlined, PlusOutlined, QuestionCircleOutlined } from '@ant-design/icons';
import { useRequest } from 'ahooks';
import { useTranslation } from 'react-i18next';
import { settings } from '../api';
import { FolderPicker } from '../components/FolderPicker';

const { Option } = Select;
const { Text } = Typography;

// Helper component for label with tooltip
const LabelWithTooltip: React.FC<{ label: string; tooltip?: string }> = ({ label, tooltip }) => {
  if (!tooltip) return <>{label}</>;
  return (
    <Space>
      {label}
      <Tooltip title={tooltip}>
        <QuestionCircleOutlined style={{ color: '#1890ff', cursor: 'help' }} />
      </Tooltip>
    </Space>
  );
};

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
      message.success('Settings saved successfully');
    } catch (e) {
      message.error('Failed to save settings');
    }
  };

  const removeItem = (list: string[], setList: (v: string[]) => void, item: string) => {
    setList(list.filter(i => i !== item));
  };

  return (
    <Card title={t('Settings.Tab.Settings')} loading={loading} variant="borderless">
      <Form
        form={form}
        layout="vertical"
        onFinish={onFinish}
        initialValues={{
          Percent: 95,
          Thumbnails: 2,
          MaxDegreeOfParallelism: -1,
          HardwareAccelerationMode: 1 // auto
        }}
      >
        {/* Hint about hovering for more info */}
        <Text type="warning" style={{ display: 'block', marginBottom: 16 }}>
          {t('Settings.Hint')}
        </Text>

        <Divider orientation="left">{t('Settings.Tab.Scanner')}</Divider>
        
        {/* Includes */}
        <div style={{ marginBottom: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                <Text strong>{t('Settings.SearchDirs')}</Text>
                <Button type="dashed" size="small" icon={<PlusOutlined />} onClick={() => setShowIncludePicker(true)}>
                    {t('Settings.Add')}
                </Button>
            </div>
            <List
                size="small"
                bordered
                dataSource={includes}
                renderItem={item => (
                    <List.Item actions={[<Button type="text" danger icon={<DeleteOutlined />} onClick={() => removeItem(includes, setIncludes, item)} />]}>
                        {item}
                    </List.Item>
                )}
            />
            <Text type="warning" style={{ fontSize: 12 }}>{t('Settings.DragDropHint')}</Text>
        </div>

        {/* Blacklists */}
        <div style={{ marginBottom: 24 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                <Text strong>{t('Settings.ExcludeDirs')}</Text>
                <Button type="dashed" size="small" icon={<PlusOutlined />} onClick={() => setShowExcludePicker(true)}>
                    {t('Settings.Add')}
                </Button>
            </div>
            <List
                size="small"
                bordered
                dataSource={blacklists}
                renderItem={item => (
                    <List.Item actions={[<Button type="text" danger icon={<DeleteOutlined />} onClick={() => removeItem(blacklists, setBlacklists, item)} />]}>
                        {item}
                    </List.Item>
                )}
            />
            <Text type="warning" style={{ fontSize: 12 }}>{t('Settings.DragDropHint')}</Text>
        </div>
        
        <Divider orientation="left">{t('Settings.Tab.Misc')}</Divider>
        <Space wrap>
            <Form.Item label={t('Settings.IncludeSubDirs')} name="IncludeSubDirectories" valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item label={t('Settings.IncludeImages')} name="IncludeImages" valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item label={t('Settings.IgnoreReadOnly')} name="IgnoreReadOnlyFolders" valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.GeneratePreviews')} tooltip={t('ToolTip.Settings.GeneratePreviews')} />} 
              name="GeneratePreviewThumbnails" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.ExcludeReparse')} tooltip={t('ToolTip.Settings.ExcludeReparse')} />} 
              name="IgnoreReparsePoints" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.ExcludeHardLinks')} tooltip={t('ToolTip.Settings.ExcludeHardLinks')} />} 
              name="ExcludeHardLinks" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.IncludeNonExisting')} tooltip={t('ToolTip.Settings.IncludeNonExisting')} />} 
              name="IncludeNonExistingFiles" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.ScanAgainstDb')} tooltip={t('ToolTip.Settings.ScanAgainstDb')} />} 
              name="ScanAgainstEntireDatabase" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
        </Space>

        <Divider orientation="left">{t('Settings.FilesNote')}</Divider>
        <Row gutter={16} >
            <Col span={8}>
                <Form.Item 
                  label={<LabelWithTooltip label={t('Settings.Percent')} tooltip={t('ToolTip.Settings.Percent')} />} 
                  name="Percent"
                >
                  <InputNumber min={1} max={100} style={{ width: '100%' }} addonAfter="%" />
                </Form.Item>
            </Col>
            <Col span={8}>
                <Form.Item 
                  label={<LabelWithTooltip label={t('Settings.DurationDiff')} tooltip={t('ToolTip.Settings.DurationDiff')} />} 
                  name="PercentDurationDifference"
                >
                  <InputNumber min={0} max={100} style={{ width: '100%' }} addonAfter="%" />
                </Form.Item>
            </Col>
            <Col span={8}>
                <Form.Item 
                  label={<LabelWithTooltip label={t('Settings.Thumbnails')} tooltip={t('ToolTip.Settings.Thumbnails')} />} 
                  name="Thumbnails"
                >
                  <InputNumber min={1} max={100} style={{ width: '100%' }} />
                </Form.Item>
            </Col>
        </Row>

        <Row gutter={16}>
             <Col span={12}>
                <Form.Item 
                  label={<LabelWithTooltip label={t('Settings.Parallelism')} tooltip={t('ToolTip.Settings.Parallelism')} />} 
                  name="MaxDegreeOfParallelism"
                >
                  <InputNumber min={-1} style={{ width: '100%' }} />
                </Form.Item>
             </Col>
             <Col span={12}>
                <Form.Item 
                  label={<LabelWithTooltip label={t('Settings.HWAccel')} tooltip={t('ToolTip.Settings.HWAccel')} />} 
                  name="HardwareAccelerationMode"
                >
                    <Select>
                        <Option value={0}>None</Option>
                        <Option value={1}>Auto</Option>
                        <Option value={2}>VDPAU</Option>
                        <Option value={3}>DXVA2</Option>
                        <Option value={4}>VAAPI</Option>
                        <Option value={5}>QSV</Option>
                        <Option value={6}>CUDA</Option>
                        <Option value={7}>VideoToolbox</Option>
                        <Option value={8}>D3D11VA</Option>
                        <Option value={9}>DRM</Option>
                        <Option value={10}>MediaCodec</Option>
                        <Option value={11}>Vulkan</Option>
                    </Select>
                </Form.Item>
             </Col>
        </Row>
        
        <Divider orientation="left">Advanced</Divider>
        <Space wrap>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.UsePHash')} tooltip={t('ToolTip.Settings.UsePHash')} />} 
              name="UsePHash" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.UseExif')} tooltip={t('ToolTip.Settings.UseExif')} />} 
              name="UseExifCreationDate" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.IgnoreBlack')} tooltip={t('ToolTip.Settings.IgnoreBlack')} />} 
              name="IgnoreBlackPixels" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.IgnoreWhite')} tooltip={t('ToolTip.Settings.IgnoreWhite')} />} 
              name="IgnoreWhitePixels" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.NativeFFmpeg')} tooltip={t('ToolTip.Settings.NativeFFmpeg')} />} 
              name="UseNativeFfmpegBinding" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.CompareFlipped')} tooltip={t('ToolTip.Settings.CompareFlipped')} />} 
              name="CompareHorizontallyFlipped" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.ExtendedLogging')} tooltip={t('ToolTip.Settings.ExtendedLogging')} />} 
              name="ExtendedFFToolsLogging" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.RetrySampling')} tooltip={t('ToolTip.Settings.RetrySampling')} />} 
              name="AlwaysRetryFailedSampling" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.AutoBackup')} tooltip={t('ToolTip.Settings.AutoBackup')} />} 
              name="BackupAfterListChanged" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item 
              label={<LabelWithTooltip label={t('Settings.SaveOnExit')} tooltip={t('ToolTip.Settings.SaveOnExit')} />} 
              name="AskToSaveResultsOnExit" 
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
        </Space>

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
            <Input placeholder="Leave empty for default location" />
        </Form.Item>

        <Form.Item>
          <Button type="primary" htmlType="submit">
            {t('Settings.Save')}
          </Button>
        </Form.Item>
      </Form>

      <FolderPicker 
        open={showIncludePicker} 
        value={includes}
        onCancel={() => setShowIncludePicker(false)}
        onChange={(vals) => {
            // Merge unique
            setIncludes(prev => Array.from(new Set([...prev, ...vals])));
            setShowIncludePicker(false);
        }}
      />
      
      <FolderPicker 
        open={showExcludePicker} 
        value={blacklists}
        onCancel={() => setShowExcludePicker(false)}
        onChange={(vals) => {
            // Merge unique
            setBlacklists(prev => Array.from(new Set([...prev, ...vals])));
            setShowExcludePicker(false);
        }}
      />
    </Card>
  );
};

export default Settings;
