import React, { useState } from 'react';
import { Card, Table, Image, Button, Tag, Space, Tooltip, Empty } from 'antd';
import { useRequest } from 'ahooks';
import { scan } from '../api';
import { FileImageOutlined, VideoCameraOutlined, DeleteOutlined } from '@ant-design/icons';

interface DuplicateItem {
  path: string;
  sizeLong: number;
  duration: string;
  frameSize: string;
  similarity: number;
  hasThumbnail: boolean;
  isImage: boolean;
}

interface DuplicateGroup {
  groupId: string;
  items: DuplicateItem[];
}

const Results: React.FC = () => {
  const { data, loading, refresh } = useRequest(scan.getResults);

  const columns = [
    {
      title: 'Group',
      dataIndex: 'groupId',
      key: 'groupId',
      render: (_: string, record: DuplicateGroup, index: number) => `Group #${index + 1} (${record.items.length} items)`,
    },
    {
      title: 'Total Size',
      key: 'totalSize',
      render: (_: string, record: DuplicateGroup) => {
        const size = record.items.reduce((acc, item) => acc + item.sizeLong, 0);
        return (size / 1024 / 1024).toFixed(2) + ' MB';
      }
    },
    {
      title: 'Actions',
      key: 'actions',
      render: () => (
        <Space>
           <Button size="small">Select Best</Button>
        </Space>
      )
    }
  ];

  const expandedRowRender = (record: DuplicateGroup) => {
    return (
      <Table
        columns={[
          {
            title: 'Preview',
            key: 'thumbnail',
            width: 120,
            render: (_: any, item: DuplicateItem) => (
               item.hasThumbnail ? 
               <Image 
                 width={100} 
                 src={`/api/scan/thumbnail?path=${encodeURIComponent(item.path)}`} 
                 fallback="https://via.placeholder.com/100?text=Error"
               /> :
               <div style={{ width: 100, height: 60, background: '#f0f0f0', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  {item.isImage ? <FileImageOutlined /> : <VideoCameraOutlined />}
               </div>
            )
          },
          { title: 'Path', dataIndex: 'path', key: 'path', ellipsis: true },
          { 
            title: 'Size', 
            dataIndex: 'sizeLong', 
            key: 'size', 
            width: 100,
            render: (v: number) => (v / 1024 / 1024).toFixed(2) + ' MB',
            sorter: (a: DuplicateItem, b: DuplicateItem) => a.sizeLong - b.sizeLong
          },
          { title: 'Res', dataIndex: 'frameSize', key: 'res', width: 100 },
          { title: 'Sim', dataIndex: 'similarity', key: 'sim', width: 80, render: (v: number) => Math.round(v) + '%' },
          {
            title: 'Action',
            key: 'action',
            width: 80,
            render: () => <Button type="text" danger icon={<DeleteOutlined />} />
          }
        ]}
        dataSource={record.items}
        pagination={false}
        rowKey="path"
        size="small"
      />
    );
  };

  return (
    <Card title="Scan Results" bordered={false} extra={<Button onClick={refresh}>Refresh</Button>}>
      {(!data || data.length === 0) ? (
        <Empty description="No duplicates found yet" />
      ) : (
        <Table
          columns={columns}
          dataSource={data}
          rowKey="groupId"
          expandable={{ expandedRowRender, defaultExpandAllRows: true }}
          pagination={{ pageSize: 10 }}
        />
      )}
    </Card>
  );
};

export default Results;
