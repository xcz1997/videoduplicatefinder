import React, { useState, useMemo } from 'react';
import { Card, Table, Image, Button, Space, Empty, Tag, Tooltip, message, Modal } from 'antd';
import { useRequest } from 'ahooks';
import { useTranslation } from 'react-i18next';
import { scan } from '../api';
import { FileImageOutlined, VideoCameraOutlined, DeleteOutlined, CheckCircleOutlined, StarFilled, ExclamationCircleOutlined, SaveOutlined, EyeOutlined } from '@ant-design/icons';
import ThumbnailPreview from '../components/ThumbnailPreview';

interface DuplicateItem {
  path: string;
  sizeLong: number;
  duration: string;
  frameSize: string;
  similarity: number;
  hasThumbnail: boolean;
  thumbnailCount: number;
  isImage: boolean;
  // Best flags
  isBestSize?: boolean;
  isBestDuration?: boolean;
  isBestFrameSize?: boolean;
  isBestFps?: boolean;
  isBestBitRateKbs?: boolean;
  isBestAudioSampleRate?: boolean;
  fps?: number;
  bitRateKbs?: number;
  audioSampleRate?: number;
}

interface DuplicateGroup {
  groupId: string;
  items: DuplicateItem[];
}

const PAGE_SIZE = 10;

const Results: React.FC = () => {
  const { data, refresh } = useRequest(scan.getResults);
  const { t } = useTranslation();
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());
  const [currentPage, setCurrentPage] = useState(1);
  const [deleting, setDeleting] = useState(false);
  const [saving, setSaving] = useState(false);
  const [previewItem, setPreviewItem] = useState<DuplicateItem | null>(null);

  // 计算所有 groupId 用于默认展开
  const allGroupIds = useMemo(() => {
    if (!data || data.length === 0) return [];
    return data.map((g: DuplicateGroup) => g.groupId);
  }, [data]);

  // Calculate best item in a group based on multiple criteria
  const getBestItemInGroup = (items: DuplicateItem[]): string | null => {
    if (items.length === 0) return null;

    // Score each item: higher is better
    const scored = items.map(item => {
      let score = 0;
      if (item.isBestFrameSize) score += 3; // Resolution is most important
      if (item.isBestDuration) score += 2;  // Duration second
      if (item.isBestBitRateKbs) score += 2;
      if (item.isBestFps) score += 1;
      if (item.isBestAudioSampleRate) score += 1;
      // Smaller file size is better for keeping (others will be deleted)
      if (item.isBestSize) score += 1;
      return { path: item.path, score };
    });

    // Return the item with highest score
    scored.sort((a, b) => b.score - a.score);
    return scored[0].path;
  };

  const handleSelectBest = (group: DuplicateGroup) => {
    const bestPath = getBestItemInGroup(group.items);
    if (!bestPath) return;

    // Select all items except the best one (those will be candidates for deletion)
    const newSelected = new Set(selectedItems);
    group.items.forEach(item => {
      if (item.path !== bestPath) {
        newSelected.add(item.path);
      } else {
        newSelected.delete(item.path);
      }
    });
    setSelectedItems(newSelected);
    message.success(t('Results.BestSelected'));
  };

  // Smart select for current page
  const handleSelectCurrentPageBest = () => {
    if (!data || data.length === 0) return;

    const startIdx = (currentPage - 1) * PAGE_SIZE;
    const endIdx = Math.min(startIdx + PAGE_SIZE, data.length);
    const currentPageGroups = data.slice(startIdx, endIdx);

    const newSelected = new Set(selectedItems);
    currentPageGroups.forEach((group: DuplicateGroup) => {
      const bestPath = getBestItemInGroup(group.items);
      if (!bestPath) return;
      group.items.forEach(item => {
        if (item.path !== bestPath) {
          newSelected.add(item.path);
        } else {
          newSelected.delete(item.path);
        }
      });
    });
    setSelectedItems(newSelected);
    message.success(t('Results.CurrentPageBestSelected', { count: currentPageGroups.length }));
  };

  // Smart select for all pages
  const handleSelectAllBest = () => {
    if (!data || data.length === 0) return;

    const newSelected = new Set(selectedItems);
    data.forEach((group: DuplicateGroup) => {
      const bestPath = getBestItemInGroup(group.items);
      if (!bestPath) return;
      group.items.forEach(item => {
        if (item.path !== bestPath) {
          newSelected.add(item.path);
        } else {
          newSelected.delete(item.path);
        }
      });
    });
    setSelectedItems(newSelected);
    message.success(t('Results.AllBestSelected', { count: data.length }));
  };

  // Delete selected files
  const handleDeleteSelected = () => {
    if (selectedItems.size === 0) {
      message.warning(t('Results.NoSelection'));
      return;
    }

    Modal.confirm({
      title: t('Results.DeleteConfirmTitle'),
      icon: <ExclamationCircleOutlined />,
      content: t('Results.DeleteConfirmContent', { count: selectedItems.size }),
      okText: t('Dialog.Yes'),
      okType: 'danger',
      cancelText: t('Dialog.Cancel'),
      onOk: async () => {
        setDeleting(true);
        try {
          const result = await scan.deleteFiles(Array.from(selectedItems));
          message.success(t('Results.DeleteSuccess', {
            success: result.successCount,
            fail: result.failCount
          }));
          // Clear selection and refresh
          setSelectedItems(new Set());
          refresh();
        } catch {
          message.error(t('Results.DeleteFailed'));
        } finally {
          setDeleting(false);
        }
      }
    });
  };

  // Save results
  const handleSaveResults = async () => {
    setSaving(true);
    try {
      const result = await scan.saveResults();
      if (result.success) {
        message.success(t('Results.SaveSuccess', { count: result.count }));
      } else {
        message.error(result.message || t('Results.SaveFailed'));
      }
    } catch {
      message.error(t('Results.SaveFailed'));
    } finally {
      setSaving(false);
    }
  };

  const columns = [
    {
      title: t('DuplicateList.Header.GroupItem'),
      dataIndex: 'groupId',
      key: 'groupId',
      render: (_: string, record: DuplicateGroup, index: number) =>
        t('Results.GroupLabel', { index: index + 1, count: record.items.length }),
    },
    {
      title: t('DuplicateList.Header.Size'),
      key: 'totalSize',
      render: (_: string, record: DuplicateGroup) => {
        const size = record.items.reduce((acc, item) => acc + item.sizeLong, 0);
        return (size / 1024 / 1024).toFixed(2) + ' MB';
      }
    },
    {
      title: t('Toolbar.Selection'),
      key: 'actions',
      render: (_: any, record: DuplicateGroup) => (
        <Space>
           <Button size="small" onClick={() => handleSelectBest(record)}>
             {t('Results.SelectBest')}
           </Button>
        </Space>
      )
    }
  ];

  const renderBestTags = (item: DuplicateItem) => {
    const tags = [];
    if (item.isBestFrameSize) tags.push(<Tag key="res" color="blue">{t('Results.BestResolution')}</Tag>);
    if (item.isBestDuration) tags.push(<Tag key="dur" color="green">{t('Results.BestDuration')}</Tag>);
    if (item.isBestBitRateKbs) tags.push(<Tag key="br" color="purple">{t('Results.BestBitrate')}</Tag>);
    if (item.isBestFps) tags.push(<Tag key="fps" color="orange">{t('Results.BestFps')}</Tag>);
    if (item.isBestSize) tags.push(<Tag key="size" color="cyan">{t('Results.SmallestSize')}</Tag>);
    return tags.length > 0 ? <Space size={2} wrap>{tags}</Space> : null;
  };

  const isBestInGroup = (item: DuplicateItem, group: DuplicateGroup): boolean => {
    const bestPath = getBestItemInGroup(group.items);
    return item.path === bestPath;
  };

  const expandedRowRender = (record: DuplicateGroup) => {
    return (
      <>
        <Table
          columns={[
            {
              title: t('Results.Preview'),
              key: 'thumbnail',
              width: 120,
              render: (_: any, item: DuplicateItem) => (
                item.hasThumbnail ? (
                  <div
                    style={{ position: 'relative', display: 'inline-block', cursor: 'pointer' }}
                    onClick={() => setPreviewItem(item)}
                  >
                    <Image
                      width={100}
                      style={{ borderRadius: 4 }}
                      src={`/api/scan/thumbnail?path=${encodeURIComponent(item.path)}`}
                      preview={false}
                      fallback="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='100' height='60'%3E%3Crect fill='%23f0f0f0' width='100' height='60'/%3E%3Ctext x='50%25' y='50%25' dominant-baseline='middle' text-anchor='middle' fill='%23999'%3EError%3C/text%3E%3C/svg%3E"
                    />
                    <div style={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      right: 0,
                      bottom: 0,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      background: 'rgba(0,0,0,0.4)',
                      opacity: 0,
                      transition: 'opacity 0.2s',
                      borderRadius: 4,
                    }}
                    className="thumbnail-overlay"
                    >
                      <EyeOutlined style={{ color: '#fff', fontSize: 20 }} />
                    </div>
                  </div>
                ) : (
                  <div style={{ width: 100, height: 60, background: '#f0f0f0', display: 'flex', alignItems: 'center', justifyContent: 'center', borderRadius: 4 }}>
                    {item.isImage ? <FileImageOutlined /> : <VideoCameraOutlined />}
                  </div>
                )
              )
            },
          { title: t('DuplicateList.Header.Path'), dataIndex: 'path', key: 'path', ellipsis: true },
          {
            title: t('DuplicateList.Header.Size'),
            dataIndex: 'sizeLong',
            key: 'size',
            width: 100,
            render: (v: number) => (v / 1024 / 1024).toFixed(2) + ' MB',
            sorter: (a: DuplicateItem, b: DuplicateItem) => a.sizeLong - b.sizeLong
          },
          { title: t('DuplicateList.Header.Resolution'), dataIndex: 'frameSize', key: 'res', width: 100 },
          { title: t('DuplicateList.Header.Similarity'), dataIndex: 'similarity', key: 'sim', width: 80, render: (v: number) => Math.round(v) + '%' },
          {
            title: t('Results.Quality'),
            key: 'quality',
            width: 200,
            render: (_: any, item: DuplicateItem) => (
              <Space direction="vertical" size={2}>
                {isBestInGroup(item, record) && (
                  <Tag color="gold" icon={<StarFilled />}>{t('Results.Recommended')}</Tag>
                )}
                {renderBestTags(item)}
              </Space>
            )
          },
          {
            title: t('Results.Action'),
            key: 'action',
            width: 100,
            render: (_: any, item: DuplicateItem) => (
              <Space>
                <Tooltip title={selectedItems.has(item.path) ? t('Results.Deselect') : t('Results.Select')}>
                  <Button
                    type={selectedItems.has(item.path) ? "primary" : "default"}
                    size="small"
                    icon={<CheckCircleOutlined />}
                    onClick={() => {
                      const newSelected = new Set(selectedItems);
                      if (newSelected.has(item.path)) {
                        newSelected.delete(item.path);
                      } else {
                        newSelected.add(item.path);
                      }
                      setSelectedItems(newSelected);
                    }}
                  />
                </Tooltip>
                <Button type="text" danger icon={<DeleteOutlined />} />
              </Space>
            )
          }
        ]}
        dataSource={record.items}
        pagination={false}
        rowKey="path"
        size="small"
        rowClassName={(item: DuplicateItem) => selectedItems.has(item.path) ? 'selected-row' : ''}
        />
      </>
    );
  };

  return (
    <Card
      title={t('Results.Title')}
      variant="borderless"
      extra={
        <Space wrap>
          {selectedItems.size > 0 && (
            <Tag color="blue">{t('Results.SelectedCount', { count: selectedItems.size })}</Tag>
          )}
          <Button onClick={handleSelectCurrentPageBest} disabled={!data || data.length === 0}>
            {t('Results.SelectCurrentPageBest')}
          </Button>
          <Button onClick={handleSelectAllBest} disabled={!data || data.length === 0}>
            {t('Results.SelectAllBest')}
          </Button>
          <Button
            danger
            type="primary"
            icon={<DeleteOutlined />}
            onClick={handleDeleteSelected}
            disabled={selectedItems.size === 0}
            loading={deleting}
          >
            {t('Results.DeleteSelected')}
          </Button>
          <Button
            icon={<SaveOutlined />}
            onClick={handleSaveResults}
            disabled={!data || data.length === 0}
            loading={saving}
          >
            {t('Results.SaveResults')}
          </Button>
          <Button onClick={refresh}>{t('Results.Refresh')}</Button>
        </Space>
      }
    >
      {(!data || data.length === 0) ? (
        <Empty description={t('Results.NoDuplicatesFound')} />
      ) : (
        <Table
          columns={columns}
          dataSource={data}
          rowKey="groupId"
          expandable={{ expandedRowRender, expandedRowKeys: allGroupIds }}
          pagination={{
            pageSize: PAGE_SIZE,
            current: currentPage,
            onChange: (page) => setCurrentPage(page)
          }}
        />
      )}
      <style>{`
        .selected-row {
          background-color: #e6f7ff !important;
        }
        .selected-row:hover > td {
          background-color: #bae7ff !important;
        }
        .thumbnail-overlay {
          opacity: 0 !important;
        }
        div:hover > .thumbnail-overlay {
          opacity: 1 !important;
        }
      `}</style>

      {/* Thumbnail Preview Modal */}
      <ThumbnailPreview
        visible={previewItem !== null}
        onClose={() => setPreviewItem(null)}
        path={previewItem?.path || ''}
        thumbnailCount={previewItem?.thumbnailCount || 1}
        isImage={previewItem?.isImage || false}
      />
    </Card>
  );
};

export default Results;
