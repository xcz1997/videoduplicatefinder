import React from 'react';
import { Form, InputNumber, Switch, Button, Card, message, Divider } from 'antd';
import { useRequest } from 'ahooks';
import { settings } from '../api';

const Settings: React.FC = () => {
  const [form] = Form.useForm();
  const { data, loading } = useRequest(settings.get, {
    onSuccess: (res) => {
      form.setFieldsValue(res);
    }
  });

  const onFinish = async (values: any) => {
    try {
      // Merge with existing data to keep lists intact (Includes/Blacklists handled separately ideally)
      const toSave = { ...data, ...values };
      await settings.save(toSave);
      message.success('Settings saved successfully');
    } catch (e) {
      message.error('Failed to save settings');
    }
  };

  return (
    <Card title="Scan Settings" loading={loading} bordered={false}>
      <Form
        form={form}
        layout="vertical"
        onFinish={onFinish}
        initialValues={{
          Percent: 95,
          Thumbnails: 2,
          MaxDegreeOfParallelism: -1
        }}
      >
        <Divider orientation="left">General</Divider>
        <Form.Item label="Scan Include Subdirectories" name="IncludeSubDirectories" valuePropName="checked">
          <Switch />
        </Form.Item>
        <Form.Item label="Include Images" name="IncludeImages" valuePropName="checked">
          <Switch />
        </Form.Item>
        <Form.Item label="Ignore Read-Only Folders" name="IgnoreReadOnlyFolders" valuePropName="checked">
          <Switch />
        </Form.Item>

        <Divider orientation="left">Performance & Accuracy</Divider>
        <Form.Item label="Similarity Percent (%)" name="Percent" tooltip="Minimum similarity percentage to consider duplicates">
          <InputNumber min={1} max={100} />
        </Form.Item>
        <Form.Item label="Duration Difference (%)" name="PercentDurationDifference">
          <InputNumber min={0} max={100} />
        </Form.Item>
        <Form.Item label="Thumbnail Count" name="Thumbnails">
          <InputNumber min={1} max={100} />
        </Form.Item>
        <Form.Item label="Thread Count (Max Parallelism)" name="MaxDegreeOfParallelism" tooltip="-1 for auto">
          <InputNumber min={-1} />
        </Form.Item>
        <Form.Item label="Use pHash (More robust but slower)" name="UsePHash" valuePropName="checked">
          <Switch />
        </Form.Item>

        <Form.Item>
          <Button type="primary" htmlType="submit">
            Save Settings
          </Button>
        </Form.Item>
      </Form>
    </Card>
  );
};

export default Settings;
