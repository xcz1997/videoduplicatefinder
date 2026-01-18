import React, { useState } from 'react';
import { Button, Card, Statistic, Progress, List, Tag, Row, Col, Space, Typography } from 'antd';
import {
  FolderOpenOutlined,
  PlayCircleOutlined,
  StopOutlined,
  PlusOutlined
} from '@ant-design/icons';
import { useRequest } from 'ahooks';
import { scan } from '../api';
import { FolderPicker } from '../components/FolderPicker';

const { Text } = Typography;

const Dashboard: React.FC = () => {
  const [selectedPaths, setSelectedPaths] = useState<string[]>([]);
  const [showFolderPicker, setShowFolderPicker] = useState(false);

  // Poll status every 1s
  const { data: status, run: refreshStatus } = useRequest(scan.getStatus, {
    pollingInterval: 1000,
    pollingWhenHidden: false,
  });

  const handleStart = async () => {
    if (selectedPaths.length === 0) return;
    await scan.start(selectedPaths);
    refreshStatus();
  };

  const handleStop = async () => {
    await scan.stop();
    refreshStatus();
  };

  return (
    <>
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={8}>
          <Card bordered={false}>
            <Statistic
              title="Status"
              value={status?.isScanning ? 'Scanning' : 'Idle'}
              valueStyle={{ color: status?.isScanning ? '#1677ff' : '#cf1322' }}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card bordered={false}>
            <Statistic title="Duplicates Found" value={status?.duplicatesFound || 0} prefix={<FolderOpenOutlined />} />
          </Card>
        </Col>
        <Col span={8}>
          <Card bordered={false}>
            <Statistic title="Processed Files" value={status?.processedFiles || 0} suffix={`/ ${status?.totalFiles || 0}`} />
          </Card>
        </Col>
      </Row>

      <Card title="Scan Control" bordered={false} style={{ marginBottom: 24 }}>
        <div style={{ marginBottom: 20 }}>
           <Text strong>Search Directories: </Text>
           <Button type="link" icon={<PlusOutlined />} onClick={() => setShowFolderPicker(true)}>Add Folder</Button>
           <List
             size="small"
             dataSource={selectedPaths}
             renderItem={(item) => (
               <List.Item>
                 <Tag color="blue">{item}</Tag>
               </List.Item>
             )}
           />
        </div>

        <Space size="large" style={{ width: '100%', marginBottom: 24 }}>
           <Button type="primary" size="large" icon={<PlayCircleOutlined />} onClick={handleStart} loading={status?.isScanning}>
             Start Scan
           </Button>
           <Button danger size="large" icon={<StopOutlined />} onClick={handleStop} disabled={!status?.isScanning}>
             Stop
           </Button>
        </Space>

        {status?.isScanning && (
            <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                    <Text type="secondary">{status.currentActivity}</Text>
                    <Text type="secondary">{Math.round(status.progress)}%</Text>
                </div>
                <Progress percent={status.progress} status="active" showInfo={false} />
            </div>
        )}
      </Card>

      <FolderPicker 
        open={showFolderPicker} 
        value={selectedPaths}
        onCancel={() => setShowFolderPicker(false)}
        onChange={(vals) => {
            setSelectedPaths(vals);
            setShowFolderPicker(false);
        }}
      />
    </>
  );
};

export default Dashboard;
