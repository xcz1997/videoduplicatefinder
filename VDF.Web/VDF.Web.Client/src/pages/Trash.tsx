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
  Input,
  Select,
  Image,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  DeleteOutlined,
  ReloadOutlined,
  UndoOutlined,
  ClearOutlined,
  FolderOutlined,
  ClockCircleOutlined,
  FileOutlined,
  EyeOutlined,
  SearchOutlined,
  FilterOutlined,
  PlayCircleOutlined,
} from '@ant-design/icons';
import { useRequest } from 'ahooks';
import { trash, TrashItem, TrashListResponse } from '../api';

const { Title, Text } = Typography;
const { Search } = Input;
const { Option } = Select;

// Check if file is a video based on extension
const isVideoFile = (fileName: string): boolean => {
  const videoExtensions = ['.mp4', '.mkv', '.avi', '.mov', '.wmv', '.flv', '.webm', '.m4v', '.mpeg', '.mpg', '.3gp'];
  const ext = fileName.toLowerCase().substring(fileName.lastIndexOf('.'));
  return videoExtensions.includes(ext);
};

// Check if file is an image based on extension
const isImageFile = (fileName: string): boolean => {
  const imageExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.tiff', '.ico'];
  const ext = fileName.toLowerCase().substring(fileName.lastIndexOf('.'));
  return imageExtensions.includes(ext);
};

const Trash: React.FC = () => {
  const { t } = useTranslation();
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [searchText, setSearchText] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [previewItem, setPreviewItem] = useState<TrashItem | null>(null);
  const [previewVisible, setPreviewVisible] = useState(false);

  const {
    data,
    loading,
    refresh,
  } = useRequest<TrashListResponse, []>(trash.getItems);

  const handleRestore = async (id: string) => {
    try {
      await trash.restore(id);
      message.success(t('Trash.RestoreSuccess'));
      refresh();
    } catch (error) {
      message.error(t('Trash.RestoreFailed'));
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await trash.delete(id);
      message.success(t('Trash.DeleteSuccess'));
      refresh();
    } catch (error) {
      message.error(t('Trash.DeleteFailed'));
    }
  };

  const handleBatchRestore = async () => {
    if (selectedRowKeys.length === 0) {
      message.warning(t('Trash.SelectItemsWarning'));
      return;
    }
    try {
      const result = await trash.restoreBatch(selectedRowKeys as string[]);
      message.success(t('Trash.BatchRestoreSuccess', { success: result.successCount, fail: result.failCount }));
      setSelectedRowKeys([]);
      refresh();
    } catch (error) {
      message.error(t('Trash.BatchRestoreFailed'));
    }
  };

  const handleEmptyTrash = async () => {
    try {
      const result = await trash.empty();
      message.success(t('Trash.EmptySuccess', { count: result.deletedCount }));
      setSelectedRowKeys([]);
      refresh();
    } catch (error) {
      message.error(t('Trash.EmptyFailed'));
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  // Filter items based on search text and status
  const filteredItems = useMemo(() => {
    let items = data?.items || [];

    // Apply search filter
    if (searchText) {
      const lowerSearch = searchText.toLowerCase();
      items = items.filter(item =>
        item.fileName.toLowerCase().includes(lowerSearch) ||
        item.originalPath.toLowerCase().includes(lowerSearch)
      );
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      items = items.filter(item => {
        if (statusFilter === 'available') return item.fileExists;
        if (statusFilter === 'missing') return !item.fileExists;
        if (statusFilter === 'restorable') return item.canRestore;
        return true;
      });
    }

    return items;
  }, [data?.items, searchText, statusFilter]);

  // Preview handlers
  const handlePreview = (item: TrashItem) => {
    setPreviewItem(item);
    setPreviewVisible(true);
  };

  const closePreview = () => {
    setPreviewVisible(false);
    setPreviewItem(null);
  };

  const columns: ColumnsType<TrashItem> = [
    {
      title: t('Trash.Column.FileName'),
      dataIndex: 'fileName',
      key: 'fileName',
      sorter: (a, b) => a.fileName.localeCompare(b.fileName),
      render: (text: string, record: TrashItem) => (
        <Space>
          {isVideoFile(text) ? <PlayCircleOutlined style={{ color: '#1890ff' }} /> : <FileOutlined />}
          <Text strong>{text}</Text>
          {(isVideoFile(text) || isImageFile(text)) && record.fileExists && (
            <Tooltip title={t('Trash.Preview')}>
              <Button
                type="link"
                size="small"
                icon={<EyeOutlined />}
                onClick={() => handlePreview(record)}
              />
            </Tooltip>
          )}
        </Space>
      ),
    },
    {
      title: t('Trash.Column.OriginalPath'),
      dataIndex: 'originalPath',
      key: 'originalPath',
      ellipsis: true,
      sorter: (a, b) => a.originalPath.localeCompare(b.originalPath),
      render: (text: string) => (
        <Tooltip title={text}>
          <Space>
            <FolderOutlined />
            <Text type="secondary">{text}</Text>
          </Space>
        </Tooltip>
      ),
    },
    {
      title: t('Trash.Column.Size'),
      dataIndex: 'fileSize',
      key: 'fileSize',
      width: 100,
      sorter: (a, b) => a.fileSize - b.fileSize,
      render: (_: number, record: TrashItem) => record.sizeDisplay,
    },
    {
      title: t('Trash.Column.Deleted'),
      dataIndex: 'deletedAt',
      key: 'deletedAt',
      width: 150,
      sorter: (a, b) => new Date(a.deletedAt).getTime() - new Date(b.deletedAt).getTime(),
      defaultSortOrder: 'descend',
      render: (_: string, record: TrashItem) => (
        <Tooltip title={new Date(record.deletedAt).toLocaleString()}>
          <Space>
            <ClockCircleOutlined />
            <Text type="secondary">{record.deletedAgo}</Text>
          </Space>
        </Tooltip>
      ),
    },
    {
      title: t('Trash.Column.Status'),
      key: 'status',
      width: 150,
      filters: [
        { text: t('Trash.Status.Available'), value: 'available' },
        { text: t('Trash.Status.Missing'), value: 'missing' },
        { text: t('Trash.Status.Restorable'), value: 'restorable' },
      ],
      onFilter: (value, record) => {
        if (value === 'available') return record.fileExists;
        if (value === 'missing') return !record.fileExists;
        if (value === 'restorable') return record.canRestore;
        return true;
      },
      render: (_: any, record: TrashItem) => (
        <Space wrap>
          {record.fileExists ? (
            <Tag color="green">{t('Trash.Status.Available')}</Tag>
          ) : (
            <Tag color="red">{t('Trash.Status.Missing')}</Tag>
          )}
          {!record.canRestore && <Tag color="orange">{t('Trash.Status.PathUnavailable')}</Tag>}
        </Space>
      ),
    },
    {
      title: t('Trash.Column.Actions'),
      key: 'actions',
      width: 200,
      render: (_: any, record: TrashItem) => (
        <Space>
          <Tooltip title={t('Trash.RestoreTooltip')}>
            <Button
              type="primary"
              icon={<UndoOutlined />}
              size="small"
              onClick={() => handleRestore(record.id)}
              disabled={!record.fileExists}
            >
              {t('Trash.Restore')}
            </Button>
          </Tooltip>
          <Popconfirm
            title={t('Trash.DeleteConfirmTitle')}
            description={t('Trash.DeleteConfirmDesc')}
            onConfirm={() => handleDelete(record.id)}
            okText={t('Dialog.Yes')}
            cancelText={t('Dialog.No')}
          >
            <Button danger icon={<DeleteOutlined />} size="small">
              {t('Trash.Delete')}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const rowSelection = {
    selectedRowKeys,
    onChange: (keys: React.Key[]) => setSelectedRowKeys(keys),
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: 50 }}>
        <Spin size="large" />
      </div>
    );
  }

  const items = data?.items || [];
  const isEmpty = items.length === 0;

  return (
    <div>
      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col span={8}>
          <Card>
            <Statistic
              title={t('Trash.ItemsInTrash')}
              value={data?.itemCount || 0}
              prefix={<DeleteOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title={t('Trash.TotalSize')}
              value={formatBytes(data?.totalSize || 0)}
              prefix={<FileOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Space style={{ width: '100%', justifyContent: 'center' }}>
              <Button icon={<ReloadOutlined />} onClick={refresh}>
                {t('Trash.Refresh')}
              </Button>
              <Popconfirm
                title={t('Trash.EmptyConfirmTitle')}
                description={t('Trash.EmptyConfirmDesc')}
                onConfirm={handleEmptyTrash}
                okText={t('Dialog.Yes')}
                cancelText={t('Dialog.No')}
                disabled={isEmpty}
              >
                <Button danger icon={<ClearOutlined />} disabled={isEmpty}>
                  {t('Trash.EmptyTrash')}
                </Button>
              </Popconfirm>
            </Space>
          </Card>
        </Col>
      </Row>

      <Card
        title={<Title level={4}>{t('Trash.Title')}</Title>}
        extra={
          <Space>
            {selectedRowKeys.length > 0 && (
              <>
                <Text type="secondary">{t('Trash.SelectedCount', { count: selectedRowKeys.length })}</Text>
                <Button
                  type="primary"
                  icon={<UndoOutlined />}
                  onClick={handleBatchRestore}
                >
                  {t('Trash.RestoreSelected')}
                </Button>
              </>
            )}
          </Space>
        }
      >
        {/* Search and Filter Controls */}
        <Space style={{ marginBottom: 16 }} wrap>
          <Search
            placeholder={t('Trash.SearchPlaceholder')}
            allowClear
            style={{ width: 300 }}
            prefix={<SearchOutlined />}
            onSearch={setSearchText}
            onChange={(e) => !e.target.value && setSearchText('')}
          />
          <Select
            style={{ width: 150 }}
            value={statusFilter}
            onChange={setStatusFilter}
            placeholder={t('Trash.FilterPlaceholder')}
            suffixIcon={<FilterOutlined />}
          >
            <Option value="all">{t('Trash.Filter.All')}</Option>
            <Option value="available">{t('Trash.Status.Available')}</Option>
            <Option value="missing">{t('Trash.Status.Missing')}</Option>
            <Option value="restorable">{t('Trash.Status.Restorable')}</Option>
          </Select>
          <Text type="secondary">
            {t('Trash.ShowingCount', { showing: filteredItems.length, total: items.length })}
          </Text>
        </Space>

        {isEmpty ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={t('Trash.Empty')}
          />
        ) : filteredItems.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={t('Trash.NoMatch')}
          />
        ) : (
          <Table
            rowKey="id"
            rowSelection={rowSelection}
            columns={columns}
            dataSource={filteredItems}
            pagination={{
              pageSize: 20,
              showSizeChanger: true,
              showTotal: (total) => t('Trash.TotalItems', { total }),
            }}
          />
        )}
      </Card>

      {/* Preview Modal */}
      <Modal
        title={previewItem?.fileName || 'Preview'}
        open={previewVisible}
        onCancel={closePreview}
        footer={null}
        width={800}
        centered
      >
        {previewItem && (
          <div style={{ textAlign: 'center' }}>
            {isImageFile(previewItem.fileName) ? (
              <Image
                src={`/api/trash/${previewItem.id}/preview`}
                alt={previewItem.fileName}
                style={{ maxHeight: '60vh' }}
                fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMIAAADDCAYAAADQvc6UAAABRWlDQ1BJQ0MgUHJvZmlsZQAAKJFjYGASSSwoyGFhYGDIzSspCnJ3UoiIjFJgf8LAwSDCIMogwMCcmFxc4BgQ4ANUwgCjUcG3awyMIPqyLsis7PPOq3QdDFcvjV3jOD1boQVTPQrgSkktTgbSf4A4LbmgqISBgTEFyFYuLykAsTuAbJEioKOA7DkgdjqEvQHEToKwj4DVhAQ5A9k3gGyB5IxEoBmML4BsnSQk8XQkNtReEOBxcfXxUQg1Mjc0dyHgXNJBSWpFCYh2zi+oLMpMzyhRcASGUqqCZ16yno6CkYGRAQMDKMwhqj/fAIcloxgHQqxAjIHBEugw5sUIsSQpBobtQPdLciLEVJYzMPBHMDBsayhILEqEO4DxG0txmrERhM29nYGBddr//5/DGRjYNRkY/l7////39v///y4Dmn+LgesATQKgqPiHRgAAAAlwSFlzAAALEwAACxMBAJqcGAAAAVlpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IlhNUCBDb3JlIDUuNC4wIj4KICAgPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4KICAgICAgPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIKICAgICAgICAgICAgeG1sbnM6dGlmZj0iaHR0cDovL25zLmFkb2JlLmNvbS90aWZmLzEuMC8iPgogICAgICAgICA8dGlmZjpPcmllbnRhdGlvbj4xPC90aWZmOk9yaWVudGF0aW9uPgogICAgICA8L3JkZjpEZXNjcmlwdGlvbj4KICAgPC9yZGY6UkRGPgo8L3g6eG1wbWV0YT4KTMInWQAABPhJREFUeAHt3UFqG0EQBdDc/04B5wCBA85p7MYrLYwJxMZ0qar5r4EhhJl/+gue0KT59/b29u3r6+vL5y8/v3z/+f0fHz9//PLz9++fn7+8fP/2+vLl6/fvL6/f//v+48fnL1//9/3fvn/98u3l+7/93/fv9x9//fbl+8u3z3+ev/86+n+/fff51y//d73/+O/zn19+/vX6++efLy8///3z/Pf76/evv//+9evn5y9fvvz69/uv12/fvvz++P7969fXry+fPr+8/Pj46evL11+/fvn66/uXf97+6+fr9y+/f/zz4+Pp+Pz15/ev3/7z98+f/3z5+evPr98/vy7+/uX7l+8/Pn9/+fHz9y8/f//9+9cvn3/8+Pnl++e/f375/vXnz+vHl+8//vz+9fv7z58/f/78+evPH//8+fPrz9++ff74+PH5+5/f//nr98/fv379+t/f//r19fvnH5++f/7y+euPj4+f/3z/5+cvn7/f//M6+Of/59+/f/n5x5+v3/7z/f/68/evn19e/n74++f3L5++/PPn98/fv/z88/f3n9++/fjz9M+fv/zz55+f//z59/e//vXz5+cvv3z/8fX71++/fvn+8/Xr18/ff7y8fPvx8evn37/8+PH5/7+//PP7t5cvP/746/vL9x8/P/768/379+8/f375+eXnr9+/fH79+evL949/v37/8vP71++fv3/+8eP7y68/f/72/en7z5+//vz49dvnl28/vn/++vPrz5//vP38/vXr168vv73+/vrzj6+/vv7z/dPXry+/fHv5+fX76/efP39++fXl19+/vvz8/evP/33+/Pen/w+fZ71+e/X1+5dffv3+8/Xb169fv/z++/ef3/768uvnH99/+fr19fvn75+//n79+vPr98+/fv/z+/P3r9+fz6/fvv/48cvnz7++fv3++/Xr5y+vvz4+f/3y++vLr99+vP7+8c8vn/56+/7l1y+/f/v27dvnz99+/Pjl55fvP7/8+u37tx8/Xr/++v3zr58/fv3248f7t5efv3z79vX7509/f/727cvP77/8+vn5y8+f33/+/PlH/v8O/vmfT2e/f/2S++efvz6/5v3nX99+ff376/efv/58/fn1x/f3b99/fnn59uXLrz+++/7z5//+/Z+Px+vH7z+/fX35+uXHl++f/v7+2++f/v79+P71/7+f/x9P//z56/Vf59ff/3v99e3zl6/ff/78+/fvr9++/vz1x8uvP799/vnn95dfX37/+fn9++u3n79+f/32+fn16/fP3z/9+cfrry+//xr+fHr5+nL++vnHy/cf37/9+v1b/vx8ev78+v3nt7+/f3v5+v37tx8//nz58euPP37+8u3n9y8/P/3y7cf3H1+/fvp6+/r7t18/f/z6+OOv77+8Pv398u3rl5e/vr38+nL9+ufXrz+//vj67fNr/vr1+9uP1/0yPX/59fPlH8Dnl9+//Pv16+fnl28/vr18+3H9/PLt548/P339+ufXH59//fz28+fn198/Pn/7+uXP1++/vn/9/v3rz99+fv/2+vr9+8v317/+/tffPv/95evPl+/fv3999/3nt+/f79/e99fnny8/vnz9+eW3P758/bH5+/bx68+/fvn6/eXn119+fv36+/vn77++fvv57cevvz+e//rx68+fv/368eOXb1++ff/88uXrl5+vP37++OXP95/fvn77+fPb1y8v315+ff3288evX7/9/PPt47cv337++Pn79df/fP78/fvrl28//vz9+5ff3/7+9fvXH6+vr79+vP79z8+/fv7y58u/vr18/fHy/dvX39++fft8+/rl648fP179+O+fz9+//P7x45fvr7//+ufnl68v/+/z/8fn/+vz/8vn/x8cG19fX/30/fcv/+j1/5//9P99fv7/8/+bPz4+f/v+4+XV5z+/P38++/3P799fv335+frzty/f3n79+u3t49evn37+/Pz5++u3b9++f//25euX7z9evv+8//H59evX7z++/fj1z+svP3/8/Pn918uvf//y7evP378+Xz9/+/nj5evPn99ff/7x8vPl58/v376+fv/6+v3n99dvX/+7/+Tr+5fvv/z48fLzy89vn15+fv388/X739++fvv+9dvPH1++fP/25ee3lx+//v39u5+/fv7+/ctnny+v/30+nz+/fv9x/ffnr1+v379+/fH5+8/f//ryj5fX718+f/ny7dvL9+8/fvn5++fn9y+//vz28/cv379++X59/fbb5+evP/98/f1v//3+8fX15+e/b96/fn199/nHy9efP7/+/PLz+49fXp++3l9f35///vv/Pv/n8/P/n//f/n/5/P/r/2f1/8fn9z+fvv38/v3Ll+vn5x9fv/345ffX1+vX7z++fX39/uvr68/v379+/fLz29evP3+/fv/16+3H75++vH77+ev3b69fX768fv/6+v3n99+//3y9/vz28+v3H59+vP76+v3Hy8/r99fX1x8//vj18+/fv/7+/fuvb/9++PD1y5evv77++OPlx5cvP7+9fvv2+vrz+/fvP369/vz99duvn99+/PH19evl5+vzq38+f/765ef3H7/9/PXLt8+3H9++fvn2/fvnl5+/Pn/5+fXl2+vL59cf33+8fP/67efXb7/+/P7z269fPz6/Prx+/frj548fP//98/e/b9++fvnx59evX7/9/eXb19frx4/PP77++vL1y4/vn398+/nlx+vPL99+fn49vL79+OPXr69fv/+6fv/58+v377++vF5fvv28fnz7+vXL9y8/v7++/Pz69df3l1+/f/l5/fL58+fXn99/vvz49vXr5+/fv/169/rt+8/r58/ffv7x8vXn15+/fvn+/OP769fX15+/fvn2/fvLy48fL58/v/64/vj29fXn09evP79+/fbrx4+vn79+v/7+/fvP19+//X79+OXb15evL68/fv39+/v315+v3758e315+frl+8/rr19+fvn5+vr5+8+fr79+fvn+++/fv3z98v3zt28/f/76+vrzy4+vnz+//Pj+9fPXl+vn79+/fP79y8uvX79+efvz+/Xz6+u/f/v+/fvPn79+/fXt55efXz9/+/X7t68/fv325efL1+v318+fX35+//rr18/Pn19ffv768fXb15/fvn379fP18+ff3/5+/vHl+5fX19d/v/74+uPn6+vr58/X66/f/v76+vr188/ffv/149en399/fPvy5duvn69fv/zz9fvnPz++ff/x+vr19evX16/fv/z6/fv3169ff/36+v3H59eXn19eX19+ff/68/vP779/+/75+8unl6/ffnx9vX79ePvx7evvL1+/f3/5+vXLz+/fv37+/OXH99+/+/zr5+u3b79++fXr55fv37/9+OPP3/7+89fnX9++/f7y5cvPz19+fP38+fvXH79+f3395+dff/38/c+/fvz8+evzr2/ffnz7/P374+eXX15fX798+/Xy8vX79+uf318+/7j+/e8/X/9++/Hr1y/fv/z89evX5z+/f/759fvv17+/fvn+4/O31+/fv3z9+e3b9x8/f/349eXT1y8vn398//b79+vb9++ff/x8fXn5+ePrl6/X1+/ff3z79fPL5+/fv33/8vXrt+/ffv/4++/ffr/+/v7t5++/fnn5+vWXl1++vbxer7/8+Pzz148vr99ef/389cvP75++vXx5fX37+O3b5x/fvn15+f7j+7cvn79/+/b64/vL65eXX99+ff19/frl+/fvv379+uXnl5/ffnz7+uXl9fu3r9+/f/v98/e/v3//+vXb69ev318+P/3+8/v36+vr5+/ffv76/PLz5/fvv3379e3bz++fv/z689fvt68/3p+/f/v665ef//r5+98/f/z88/fn1y+fXr79+uPbl1/efv7z9cvX759+X78/v779/Pnb0+fXb99+fPny+rL//4fz5/Xz19/fXn/++v7z5++vX79++/b79dvvL5/+/PXlx9fP339++/rl9evL169fv7+8vHy9fv/+/dvvH7+8/v7x8/u3V09fX398//b98/fnrz9+fv3y8+evXz5/+frl9fX75+9fv3x9+frry9cvX3///vXbl+/ff3z9cX39/fPbl68/fvzz9+/Xbz+//v7z6+/Xz5+/fPv67de3H1+/ff/18/P35++ff7/+/e37j9ef/8vH9du379+/fn398vX1xy8/P3/78uvHz++/fvn5y4+vX7//+PHjx9eXHz+//7j+Dd8fX/++/v7l5+fP37/8/O3ry4+XH1+//3z58vn16/fP3z5//X79+uvPn99evr7++Pb165eX16/fn/769u3H1y9fX3/8/P7jy9efv/3y58/P/339+v319evXz99+/f7ry8v3bz++/PjlL18+f739+Pnz1+sfn69/37/e/n199+3H99+//fh6/f7t+/ePl6/ffv/79fv3b59+//rl148fn198+/7jl2+v17+/v/72+/v16+/Xr5+//f71yy8/f7/+/vXtt58/fn79+vvX17++/vr19f+fn79+e/nx48fP77//+u3rt+/Pv379+uX79y/fv7x++fb9y9fvv/z6/fu316/fv3/58u3Xn79/+/Hy7euPl+8/f/z88vrty+frr79//fX15+dvP378+vXl9fX7998/fv/27du3H9+//v79y6+fv3z9/fPnl1/evn97+/bjy5cvX379+uPHl19+/fX1x8+vX3/8+Pbz54+fP/++f7/++nP9+evb19cfXz5/u/39/fu37z++fP3y7fv3L7/8+vz108f37z++/Xz98/7z88/fnj5/+/rz+/evX3/++v3757ef336//v7y49vn7z9+fvvx9fvn269fv/76+eeP79++/Pjx48fXz5+/ff3z99+/f/ny9evn15+/f/n1+7dvX397/fP5+/cvPz//+OXL98/fv7/8+v3bt59fvn/7+vXnt58/fv788/fr9+/fP/349euf1x/fv/32+euX75++fPn88+evv//89u3rl++vL9+//3z5+vnn59fP379/f3n5+v3r95cvX39evr3++vnr18+/v3379uvb16/fvv3484+fP75+ef3+9fvnb5+/fXv5/u31x48f3759+fLz14+vPz7//PXz5++/fvn+7fX755+/fvnx7cvPbz+//fz2+8+vX37/+u3Pnz+/fPny49fXn99/fPv19+cvX398vn5+/fLz58+fX398/fHz+48fP7/8+Hb9+/PPn99ef/368/fv3758+fbt27dvP78//+3l2/efP39+/fL18/ev379evr38+u37t2/ff/32+9u/37/++v3Hz++fv//69u/X1x9fv/z+4/vXl2+/Pn77/uvP75+/f/v84/uXn99+/frz688/fvn2y89f/7r+/u3195+/f/v1+9fvn/7++9fXPz//+vHj5+e/3l5/fP3y8/fv37/+/u3b9+/Xrz9ev3/99fXz97/+/uvHt6/Xrz++/vz58/fvP77++OXH148f33/+/PP3P3/99+//ff7/lfr7x+/ff3z75du3bz++v3z99vbz2/eX78+//vp2/fLt5ev1948vr6/fv1y//vj57fOX69fbt+8/fvx4efnx45evn/96+/b55ec/f319/e3/A/bwx7ef3159/fn65dvvX3/8+vu3X3/9+u319ed/v/36+tvP19f/97/ff3//9eflr9+/ffu6/vr16/PL1y9fXn59++3bt5+/f/36+u3lx8v14/v7159fXl9/+fX78+fnz68v37589/r7t9efP3+8vr78/Pnz9dvr9y+v17+//v7+5cuXr79+vn77+tuPb99evr/87fu3z9+/fv/6/c8/f//1++fvH59+/fbt+7eXb1+vn3/8+Pr5vy/ff/z84/fn52+/fv6+vv/+9cuXb99efr78+vbb68/v37998/rly/dv/+/zl+/fXn9+efn949dv3779+PP3/w/s168/f/r54+fnX//+9u3bl59fr19+/fj+5fvL168/vn758vPHl5+//vz+4+vP7y9fv/78+fX19fX7jy9fXr9+f/l+fX39/u3115cvPz9fv798uf795evv699fv3378vn79+/fvn17/f3r19+/ffvx48fn769fXn779u/X56/fPv/28uXz95/ffv74+vL99u37++/fv3z//uvX65cvX39++/n57dePr9++v3z9+u3rl6+/f/787fP378+/fv3y5euPry9fv/3x+/ev39/+z/8D5Pd/fn75+u3nj5cvPz/f/v7x/cvX1x9f/v7y5fuPr99+/frry/cv336+fH15fX35fv3y/fv157evL59ff/766+u3l1+vv39+/v769evz17/++vX769fvL7+//vi6fn/98+f3H9++/fj68u3H929fXr9ev3//9u3l67efX3/98vr1+8+f339++/nzty9fv/78+uX1y/fX19cfv76+/vj27fXnzy9fv/74+vr58+frx5efn79++/n7t19+/vz69cvnL1++ffn19euXXz+/fXv98/Prt5fvv/78+vn6+fr19cfnt59ff379+vXn169ff/z8+vrz5+uvn99evn65fv78+u37z5+/f7//+vX7559fvr/88e3b12+/vl6//fr149uv/x/c929fv/346+X1y/f3z99//vzt27e3P398+fXz27fv365fX79evv/89fv3799fv73++v79149fvv/86+vn728/fv7y9efrx6+/f339+u3rt1+//vzy9eff/z/gfL7+/u3L959fvv3+9vvry5cvL99+vvz4+uXnz5+fX3/89vXl17fX199efvt8/fzz59evP3/9+v37128/v3379ePbz6/fv/788/uvH99eXl4+f/n65dv3r99+/vz99u3rr+vL79/evr5++/r99evnbz+//Pz9+69fX39+/fr55/fv33+8fP/67fvvv779/PVv375++/n7x8+vn1++/Pzr148ff/749fXl+/df3/7+5efXz/9+/v79z59///r16+u3n1+/f/n1++37ty8/vn378u33z5+/fv/y4+flz48/Xr//+Pz5+4/v37+9vH778u3Xty+/f//+4/u3X3/98tvrt29fXl9/+/z9+8/P3358/f7z2+cfX19+fv/68/f3H59fvv3+68u3X99ffn77+u3l+8/r569fvn37+evH11+/fvvl248fv/7+/u3b19+/fvv9z8+/f/n15+/Xb6+vv39+efn5++ePby+/r19ev//+/dvPz19+vv7y6+evX75///7rl++/vt4+f/v6+vP7t28/vv/48u3H79/Xr99+v379/v377+/fv3359uPLr9frt6/ffv389ev3b19/fPvy8/v3l5e33z9/+/7j2/Xbz9+/f/r17fXXzy8v/+/r969fff7+9fX319+/ffn56/efn79+/fr1+/dv337++vXX1y8/f3z/+vPb15evv/78+d/v/23/7ff/z/u/fvr5+8fLz2+/fn37+e33P5/ffv/68vU/r9/f/v/+6e+f33//8+v3r9++/fz77e+Xl68/f//y+du3X6+v/7z89e+frz9/+/b165fv33/88vX7/3z+57df/3379u37tx+/fP/x67evX7/8+u3by7f/fnz9+uuXn99+f339+ufny8uXP378+Hb99uvX52/fvr28vr5ev37++u3ny/d/37/++vbt15+vn3/7+v3Hz1++f/36+8/P319//f35969fX758f/35++vn7z+/ff39++fP33/99evv/x9fvnz/+ufXr6//fP326/v3319ff/z8+e3bt5+/v3579c/vv37+/uvX169fvr7++Pn9209//fb1//f58+e/f/vy4+eXnz9/fH/5/uPP77++fH/58u36+fXb99fP33+++/J9//3715ev37/+/uu/P7/+/vXrz8+fn/7+/uvr59f378+//v7+89vn1+vr799+fv78/ev3r1+/ffv68+3H95cvP798+fr5+48fP759+f7158/fXl5/+/Xz9cvL128/vn39+uvX759fXl++f/n68+3nz5+/vv3++8+vv39+/vr13/d/Xr5++fbz948vP178/O31j+/X1++v3779+vnzx6+/X39++/795cvPt+vPn79+/fHr27ev3759+/by+v3nl9cv3//+9eu3t7+/fP389fvLz+/fv/z69fv776/fvl5fvnz9+v37y8u376/ff/38+v3H78+/vn77/v3Xl69ff//+9u3H9+vPHz9ef/z68+e3X79+/Pjj49u3ly/ffvz+9+XH67dvr1+//Pz+7c/vn39+/f71668/f/z88u3v19ef3799/v318++/v/36/c/ffz/9+vn129u3n9///Pzyzy8/f/39+uXr19fv339+f/ny9fvPr69fvn39+/vXn+8vL6/fv/78/vX179evX358+/b68/vL92/f/vj2/fvP7y+/Xr+8vHy7/f79z59vv7/+/PPl+8u3H79+/vrtx9efP397+/b95dePH9+/fP7x48u37z+/f/v17fvvn35++fX9+8vPly+/fvv5+ceXXz++/fr9+9u3X3/9+vvL79++/vr57cuv318/f/7+8/fPry8/f/7x7fvP369f37/9/PXrz9+/f//++fO3X59/fP35/evPn99fX39++fHy+uXX56+/fvzy9fvXn79+/Pj59fvPl1+/f/3++dn/H34+/33779ff/v32+/Pz98+vX199+fXz1y/fv7x+/fHL9+8/vvz48fOnz18/f/3++8/vn19+/vrt29ff33/89vPb199/fv12+/v6+u3H15dvP7/8/Pbty/ef37/9+P71x89v3398++Xl59cv33/8+Pb928/fn39++fXt508/f3z/+fX11+/fv/348eXb919/fPn65eu319df37/++vXz5cvn719+fP/25dfPb79++/n96/fvt98/v3/78fXb9+u3b59ff3z/+fvnly+vr9++fPn+9/fvv7/+9u3H9x9fv397+/rr9+9fv19/fvn56/P3L1+/f/v29cvXl++/vHx9/fH12+/Xb99/fPv29ev11y+/vr/8/Pbjy9fvP7++fv/y45dvP396/fHrp6/fvv74+c/nr59+/Xz7/v3rl++/fn55/fPj5+fvn79+/f79+49fvv/29dv3bz9+fPv248fPH9++/vj67euPX7//+Pr9+4/vt29fX759+/L525cvrz++fPn26/evX79+fv/27fuXX3/8+PLzr69fv/z49tvX11+/fHn9/eeXL1+/v3z5+vOXlx+/fvv164+fX//+/v3zy7eX16+///r6+uXl5/fvX3/9/u3b79++vHy9fv/+9fvX69cvv/74+vr98++fr7++fnv9+efnz9+/fPv5+c/Pn79+/frt2+9v179+fv/y89u3n6+/vH779sfPr59ff/z68vn17+8/f/3+4/uPbz++/3j9+fXz99fvP37++Pb12++fn799/fH9y5cv379++/b65cfPr69fv19//Pz549fX19+/fv/+8/OXny/f/vr5689vn799+/L1y5e3b99/fPv568+vXz//9tnvn79+/vHjy7evn3/++v37959fv/368+cvr18+/fz645evP/768/u3X79+/frty4+3X7/9/Pzy+u/n79/efn37/u3L96/ffvz89vv77evPby/fvv349e3bt5ev39+++/b9+9evP3+8/vzxy7dv3759+/769fvXb98+v379+uv7z5/fv/36+evz1y8/Xr5++fb7xy+/vn7/8/Prl59fXv76/e37y9cvX77++v7r69cvX/7+8/vXt69ff/787ee3L99/fHv9+vrtz58/v/79+9cfL19e/9+++/Ljy8tff/3++8/v317+/Prl19fvry8/fv78+uXzj5cvX77//v3b779++fHj9cvnH99+fnn59fP355ev33/99svXr5+/fPv+9fvn15df3l6+fP/zy/dv337/+uP3169fff7z4+vP3z9/fP/1y/fvv7/8/vXt5/evnz9/+/b168/Pr6/fv/78/O3b95/fv3z7/u3Xz28/v335+uvb568//vr+4/On19fvn39++/Xrt+8/fvz68/OPbz9evnx9+/bt+/cfv77+/vn12+ufP757+f7r27dv3779+Pbz249fPz5//f715+cv/789+/fLl6+/Xr/++ePr19cf3759+fz149e3l++vX759+/H1y68/v/7y8/uXL99fv3z7+u3bt9cfXz/v+8+X69cf/w+E559/vvzz88e3b18+v3z5+fX1168/fn7+9fPHjz+++/z19e3Xz28/f/349evXbz9+fvt+/fb1+9fvv79/+/b167fvr1+/vXz58vr9y5fXb99+fPn69dufn79evv78+ufHL79evn75+vr68vXb9++///b99fvPH99efv38+evrl9fvv798/frz5+cff3///vO3b7++/PX169cfP358//Hj28+vv/7+8+vn19+/fPn1+cvX1x9fvv38/v339+8/f+6379++fv3y+9efX378+OXbl5/fvvz49uXl+/Xb5+9fvn/9+v3H169fv/38+vr1x8/Pn3/+9uXXl9cvL1+/fvn59fvnb9+/3r7+/v3Pt5fvX799+/X128+/f/z48+uvb99+vv389v3rl18/f/z49vXr5+vH62/ffn7/9u3HjxcAAAABAgMEBQYHCAkKCwwNDg8QERITFBUWFxgZGhscHR4fICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj9AQUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVpbXF1eX2BhYmNkZWZnaGlqa2xtbm9wcXJzdHV2d3h5ent8fX5/gA=="
              />
            ) : isVideoFile(previewItem.fileName) ? (
              <div>
                <video
                  controls
                  style={{ maxWidth: '100%', maxHeight: '60vh' }}
                  src={`/api/trash/${previewItem.id}/preview`}
                >
                  Your browser does not support the video tag.
                </video>
              </div>
            ) : (
              <Empty description={t('Trash.PreviewNotAvailable')} />
            )}

            <div style={{ marginTop: 16, textAlign: 'left' }}>
              <Space direction="vertical" style={{ width: '100%' }}>
                <Text><strong>{t('Trash.Column.OriginalPath')}:</strong> {previewItem.originalPath}</Text>
                <Text><strong>{t('Trash.Column.Size')}:</strong> {previewItem.sizeDisplay}</Text>
                <Text><strong>{t('Trash.Column.Deleted')}:</strong> {new Date(previewItem.deletedAt).toLocaleString()}</Text>
                <Space>
                  <Text><strong>{t('Trash.Column.Status')}:</strong></Text>
                  {previewItem.fileExists ? (
                    <Tag color="green">{t('Trash.Status.Available')}</Tag>
                  ) : (
                    <Tag color="red">{t('Trash.Status.Missing')}</Tag>
                  )}
                </Space>
              </Space>
            </div>

            <div style={{ marginTop: 16 }}>
              <Space>
                <Button
                  type="primary"
                  icon={<UndoOutlined />}
                  onClick={() => {
                    handleRestore(previewItem.id);
                    closePreview();
                  }}
                  disabled={!previewItem.fileExists}
                >
                  {t('Trash.Restore')}
                </Button>
                <Popconfirm
                  title={t('Trash.DeleteConfirmTitle')}
                  description={t('Trash.DeleteConfirmDesc')}
                  onConfirm={() => {
                    handleDelete(previewItem.id);
                    closePreview();
                  }}
                  okText={t('Dialog.Yes')}
                  cancelText={t('Dialog.No')}
                >
                  <Button danger icon={<DeleteOutlined />}>
                    {t('Trash.DeletePermanently')}
                  </Button>
                </Popconfirm>
              </Space>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default Trash;
