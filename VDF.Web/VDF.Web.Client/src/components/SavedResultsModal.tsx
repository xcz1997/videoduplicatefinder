import React, { useState } from 'react';
import { Modal, Table, Button, Space, Popconfirm, message, Empty } from 'antd';
import { DeleteOutlined, DownloadOutlined } from '@ant-design/icons';
import { useRequest } from 'ahooks';
import { useTranslation } from 'react-i18next';
import { scan, SavedResultsItem } from '../api';
import dayjs from 'dayjs';

interface SavedResultsModalProps {
  visible: boolean;
  onClose: () => void;
  onLoad: (id: string) => Promise<void>;
}

const SavedResultsModal: React.FC<SavedResultsModalProps> = ({ visible, onClose, onLoad }) => {
  const { t } = useTranslation();
  const [loadingId, setLoadingId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const { data, loading, refresh } = useRequest(
    () => scan.getSavedResultsList(),
    {
      refreshDeps: [visible],
      ready: visible,
    }
  );

  const handleLoad = async (id: string) => {
    setLoadingId(id);
    try {
      await onLoad(id);
      onClose();
    } finally {
      setLoadingId(null);
    }
  };

  const handleDelete = async (id: string) => {
    setDeletingId(id);
    try {
      const result = await scan.deleteSavedResults(id);
      if (result.success) {
        message.success(t('SavedResults.DeleteSuccess'));
        refresh();
      } else {
        message.error(result.message || t('SavedResults.DeleteFailed'));
      }
    } catch {
      message.error(t('SavedResults.DeleteFailed'));
    } finally {
      setDeletingId(null);
    }
  };

  const columns = [
    {
      title: t('SavedResults.SaveTime'),
      dataIndex: 'savedAt',
      key: 'savedAt',
      width: 180,
      render: (savedAt: string) => dayjs(savedAt).format('YYYY-MM-DD HH:mm:ss'),
      sorter: (a: SavedResultsItem, b: SavedResultsItem) =>
        new Date(a.savedAt).getTime() - new Date(b.savedAt).getTime(),
      defaultSortOrder: 'descend' as const,
    },
    {
      title: t('SavedResults.Groups'),
      dataIndex: 'groupCount',
      key: 'groupCount',
      width: 100,
      align: 'right' as const,
    },
    {
      title: t('SavedResults.Items'),
      dataIndex: 'itemCount',
      key: 'itemCount',
      width: 100,
      align: 'right' as const,
    },
    {
      title: t('SavedResults.FileSize'),
      dataIndex: 'fileSizeFormatted',
      key: 'fileSize',
      width: 100,
      align: 'right' as const,
    },
    {
      title: t('SavedResults.Actions'),
      key: 'actions',
      width: 150,
      render: (_: any, record: SavedResultsItem) => (
        <Space>
          <Button
            type="primary"
            size="small"
            icon={<DownloadOutlined />}
            loading={loadingId === record.id}
            onClick={() => handleLoad(record.id)}
          >
            {t('SavedResults.Load')}
          </Button>
          <Popconfirm
            title={t('SavedResults.DeleteConfirm')}
            onConfirm={() => handleDelete(record.id)}
            okText={t('Dialog.Yes')}
            cancelText={t('Dialog.Cancel')}
          >
            <Button
              danger
              size="small"
              icon={<DeleteOutlined />}
              loading={deletingId === record.id}
            />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <Modal
      title={t('SavedResults.Title')}
      open={visible}
      onCancel={onClose}
      footer={[
        <Button key="close" onClick={onClose}>
          {t('Dialog.Close')}
        </Button>,
      ]}
      width={700}
    >
      {data?.items?.length === 0 ? (
        <Empty description={t('SavedResults.NoSavedResults')} />
      ) : (
        <Table
          columns={columns}
          dataSource={data?.items || []}
          rowKey="id"
          loading={loading}
          pagination={false}
          size="small"
          scroll={{ y: 400 }}
        />
      )}
    </Modal>
  );
};

export default SavedResultsModal;
