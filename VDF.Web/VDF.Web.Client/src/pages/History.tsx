import React, { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Table,
  Button,
  Space,
  Card,
  Typography,
  Popconfirm,
  message,
  Tooltip,
  Statistic,
  Row,
  Col,
  Empty,
  Spin,
  Tag,
  Modal,
  Descriptions,
  List,
  Input,
  DatePicker,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  DeleteOutlined,
  ReloadOutlined,
  HistoryOutlined,
  FolderOutlined,
  ClockCircleOutlined,
  FileOutlined,
  EyeOutlined,
  SearchOutlined,
  ClearOutlined,
  CalendarOutlined,
} from '@ant-design/icons';
import { useRequest } from 'ahooks';
import { history, HistorySummary, HistoryListResponse } from '../api';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

const History: React.FC = () => {
  const { t } = useTranslation();
  const [selectedScanId, setSelectedScanId] = useState<string | null>(null);
  const [detailsModalOpen, setDetailsModalOpen] = useState(false);
  const [searchFolder, setSearchFolder] = useState('');
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null] | null>(null);

  const {
    data,
    loading,
    refresh,
  } = useRequest<HistoryListResponse, []>(history.getList);

  const {
    data: details,
    loading: loadingDetails,
    run: loadDetails,
  } = useRequest(
    (scanId: string) => history.getDetails(scanId),
    { manual: true }
  );

  const handleViewDetails = async (scanId: string) => {
    setSelectedScanId(scanId);
    await loadDetails(scanId);
    setDetailsModalOpen(true);
  };

  const handleDelete = async (scanId: string) => {
    try {
      await history.delete(scanId);
      message.success(t('History.DeleteSuccess'));
      refresh();
    } catch (error) {
      message.error(t('History.DeleteFailed'));
    }
  };

  const handleCleanup = async () => {
    try {
      const result = await history.cleanup();
      message.success(t('History.CleanupSuccess', { count: result.deletedCount }));
      refresh();
    } catch (error) {
      message.error(t('History.CleanupFailed'));
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatDate = (dateStr: string) => {
    return dayjs(dateStr).format('YYYY-MM-DD HH:mm:ss');
  };

  // Filter entries based on search and date range
  const filteredEntries = useMemo(() => {
    let items = data?.entries || [];

    // Apply search filter
    if (searchFolder) {
      const lowerSearch = searchFolder.toLowerCase();
      items = items.filter((e) =>
        e.folders.some((f) => f.toLowerCase().includes(lowerSearch))
      );
    }

    // Apply date range filter
    if (dateRange && dateRange[0] && dateRange[1]) {
      const startDate = dateRange[0].startOf('day');
      const endDate = dateRange[1].endOf('day');
      items = items.filter((e) => {
        const entryDate = dayjs(e.timestamp);
        return entryDate.isAfter(startDate) && entryDate.isBefore(endDate);
      });
    }

    return items;
  }, [data?.entries, searchFolder, dateRange]);

  const columns: ColumnsType<HistorySummary> = [
    {
      title: t('History.Column.ScanId'),
      dataIndex: 'scanId',
      key: 'scanId',
      width: 180,
      render: (text: string) => (
        <Text code>{text}</Text>
      ),
    },
    {
      title: t('History.Column.Date'),
      dataIndex: 'timestamp',
      key: 'timestamp',
      width: 180,
      render: (text: string) => (
        <Space>
          <ClockCircleOutlined />
          <Text>{formatDate(text)}</Text>
        </Space>
      ),
    },
    {
      title: t('History.Column.ScannedFolders'),
      dataIndex: 'folders',
      key: 'folders',
      ellipsis: true,
      render: (folders: string[]) => (
        <Tooltip title={folders.join('\n')}>
          <Space>
            <FolderOutlined />
            <Text type="secondary">
              {t('History.FolderCount', { count: folders.length })}
            </Text>
          </Space>
        </Tooltip>
      ),
    },
    {
      title: t('History.Column.Duplicates'),
      key: 'duplicates',
      width: 150,
      render: (_: any, record: HistorySummary) => (
        <Space direction="vertical" size={0}>
          <Text>{t('History.ItemCount', { count: record.duplicateItems })}</Text>
          <Text type="secondary">{t('History.GroupCount', { count: record.duplicateGroups })}</Text>
        </Space>
      ),
    },
    {
      title: t('History.Column.Size'),
      dataIndex: 'totalDuplicateSize',
      key: 'totalDuplicateSize',
      width: 100,
      render: (size: number) => formatBytes(size),
    },
    {
      title: t('History.Column.Deleted'),
      key: 'deleted',
      width: 120,
      render: (_: any, record: HistorySummary) => (
        record.deletedCount > 0 ? (
          <Tag color="red">
            {record.deletedCount} ({formatBytes(record.deletedSize)})
          </Tag>
        ) : (
          <Tag color="default">{t('History.None')}</Tag>
        )
      ),
    },
    {
      title: t('History.Column.Actions'),
      key: 'actions',
      width: 150,
      render: (_: any, record: HistorySummary) => (
        <Space>
          <Tooltip title={t('History.ViewDetails')}>
            <Button
              icon={<EyeOutlined />}
              size="small"
              onClick={() => handleViewDetails(record.scanId)}
            />
          </Tooltip>
          <Popconfirm
            title={t('History.DeleteConfirm')}
            onConfirm={() => handleDelete(record.scanId)}
            okText={t('Dialog.Yes')}
            cancelText={t('Dialog.No')}
          >
            <Button danger icon={<DeleteOutlined />} size="small" />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: 50 }}>
        <Spin size="large" />
      </div>
    );
  }

  const entries = data?.entries || [];
  const isEmpty = entries.length === 0;

  return (
    <div>
      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title={t('History.TotalEntries')}
              value={data?.totalCount || 0}
              prefix={<HistoryOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title={t('History.TotalDuplicates')}
              value={entries.reduce((acc, e) => acc + e.duplicateItems, 0)}
              prefix={<FileOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title={t('History.TotalDeletedSize')}
              value={formatBytes(entries.reduce((acc, e) => acc + e.deletedSize, 0))}
              prefix={<DeleteOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Space style={{ width: '100%', justifyContent: 'center' }}>
              <Button icon={<ReloadOutlined />} onClick={refresh}>
                {t('History.Refresh')}
              </Button>
              <Popconfirm
                title={t('History.CleanupConfirmTitle')}
                description={t('History.CleanupConfirmDesc')}
                onConfirm={handleCleanup}
                okText={t('Dialog.Yes')}
                cancelText={t('Dialog.No')}
                disabled={isEmpty}
              >
                <Button icon={<ClearOutlined />} disabled={isEmpty}>
                  {t('History.Cleanup')}
                </Button>
              </Popconfirm>
            </Space>
          </Card>
        </Col>
      </Row>

      <Card
        title={<Title level={4}>{t('History.Title')}</Title>}
      >
        {/* Search and Filter Controls */}
        <Space style={{ marginBottom: 16 }} wrap>
          <Input
            placeholder={t('History.SearchPlaceholder')}
            prefix={<SearchOutlined />}
            value={searchFolder}
            onChange={(e) => setSearchFolder(e.target.value)}
            style={{ width: 250 }}
            allowClear
          />
          <RangePicker
            value={dateRange}
            onChange={(dates) => setDateRange(dates)}
            placeholder={[t('History.StartDate'), t('History.EndDate')]}
            allowClear
            format="YYYY-MM-DD"
            presets={[
              { label: t('History.Last7Days'), value: [dayjs().subtract(7, 'd'), dayjs()] },
              { label: t('History.Last30Days'), value: [dayjs().subtract(30, 'd'), dayjs()] },
              { label: t('History.Last90Days'), value: [dayjs().subtract(90, 'd'), dayjs()] },
              { label: t('History.ThisMonth'), value: [dayjs().startOf('month'), dayjs()] },
              { label: t('History.LastMonth'), value: [dayjs().subtract(1, 'month').startOf('month'), dayjs().subtract(1, 'month').endOf('month')] },
            ]}
          />
          <Text type="secondary">
            <CalendarOutlined /> {t('History.ShowingCount', { showing: filteredEntries.length, total: entries.length })}
          </Text>
        </Space>

        {isEmpty ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={t('History.Empty')}
          />
        ) : filteredEntries.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={t('History.NoMatch')}
          />
        ) : (
          <Table
            rowKey="scanId"
            columns={columns}
            dataSource={filteredEntries}
            pagination={{
              pageSize: 20,
              showSizeChanger: true,
              showTotal: (total) => t('History.TotalItems', { total }),
            }}
          />
        )}
      </Card>

      <Modal
        title={`${t('History.ScanDetails')} - ${selectedScanId}`}
        open={detailsModalOpen}
        onCancel={() => setDetailsModalOpen(false)}
        footer={null}
        width={800}
      >
        {loadingDetails ? (
          <div style={{ textAlign: 'center', padding: 50 }}>
            <Spin />
          </div>
        ) : details ? (
          <div>
            <Descriptions bordered column={2} size="small">
              <Descriptions.Item label={t('History.Column.ScanId')}>{details.scanId}</Descriptions.Item>
              <Descriptions.Item label={t('History.Column.Date')}>{formatDate(details.timestamp)}</Descriptions.Item>
              <Descriptions.Item label={t('History.Detail.TotalFiles')}>{details.totalFiles}</Descriptions.Item>
              <Descriptions.Item label={t('History.Detail.Duration')}>
                {details.duration ? `${Math.round(details.duration / 1000)}s` : 'N/A'}
              </Descriptions.Item>
              <Descriptions.Item label={t('History.Detail.DuplicateGroups')}>{details.duplicateGroups}</Descriptions.Item>
              <Descriptions.Item label={t('History.Detail.DuplicateItems')}>{details.duplicateItems}</Descriptions.Item>
              <Descriptions.Item label={t('History.Detail.TotalDuplicateSize')} span={2}>
                {formatBytes(details.totalDuplicateSize)}
              </Descriptions.Item>
              <Descriptions.Item label={t('History.Detail.DeletedFiles')}>{details.deletedCount}</Descriptions.Item>
              <Descriptions.Item label={t('History.Detail.DeletedSize')}>{formatBytes(details.deletedSize)}</Descriptions.Item>
            </Descriptions>

            <Title level={5} style={{ marginTop: 16 }}>{t('History.Detail.ScannedFolders')}</Title>
            <List
              size="small"
              bordered
              dataSource={details.folders}
              renderItem={(folder: string) => (
                <List.Item>
                  <Space>
                    <FolderOutlined />
                    <Text>{folder}</Text>
                  </Space>
                </List.Item>
              )}
            />

            {details.deletions && details.deletions.length > 0 && (
              <>
                <Title level={5} style={{ marginTop: 16 }}>{t('History.Detail.DeletionRecords')}</Title>
                <Table
                  size="small"
                  dataSource={details.deletions}
                  columns={[
                    {
                      title: t('History.Detail.File'),
                      dataIndex: 'path',
                      key: 'path',
                      ellipsis: true,
                    },
                    {
                      title: t('History.Column.Size'),
                      dataIndex: 'fileSize',
                      key: 'fileSize',
                      width: 100,
                      render: (size: number) => formatBytes(size),
                    },
                    {
                      title: t('History.Detail.Action'),
                      dataIndex: 'action',
                      key: 'action',
                      width: 120,
                      render: (action: string) => (
                        <Tag color={action === 'MoveToTrash' ? 'green' : 'red'}>
                          {action === 'MoveToTrash' ? t('History.Action.MoveToTrash') : t('History.Action.Delete')}
                        </Tag>
                      ),
                    },
                    {
                      title: t('History.Column.Status'),
                      key: 'status',
                      width: 100,
                      render: (_: any, record: any) => (
                        record.restored ? (
                          <Tag color="blue">{t('History.Status.Restored')}</Tag>
                        ) : (
                          <Tag color="default">{t('History.Status.Deleted')}</Tag>
                        )
                      ),
                    },
                  ]}
                  pagination={{ pageSize: 10 }}
                />
              </>
            )}
          </div>
        ) : (
          <Empty description={t('History.LoadDetailsFailed')} />
        )}
      </Modal>
    </div>
  );
};

export default History;
