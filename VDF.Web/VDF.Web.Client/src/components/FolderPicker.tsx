import React, { useState, useEffect, useRef } from 'react';
import { Modal, Tree } from 'antd';
import { FolderOpenOutlined, HddOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { fileSystem, FileSystemNode } from '../api';
import { DataNode } from 'antd/es/tree';

interface FolderPickerProps {
  value?: string[];
  onChange?: (value: string[]) => void;
  open: boolean;
  onCancel: () => void;
}

const updateTreeData = (list: DataNode[], key: React.Key, children: DataNode[]): DataNode[] => {
  return list.map((node) => {
    if (node.key === key) {
      return { ...node, children };
    }
    if (node.children) {
      return { ...node, children: updateTreeData(node.children, key, children) };
    }
    return node;
  });
};

export const FolderPicker: React.FC<FolderPickerProps> = ({ value, onChange, open, onCancel }) => {
  const { t } = useTranslation();
  const [treeData, setTreeData] = useState<DataNode[]>([]);
  const [selectedPaths, setSelectedPaths] = useState<string[]>(value || []);
  // Use ref for immediate synchronous check to prevent duplicate loads
  const loadedKeysRef = useRef<Set<string>>(new Set());
  const loadingKeysRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    if (open) {
      loadDrives();
    }
  }, [open]);

  const loadDrives = async () => {
    const drives = await fileSystem.getDrives();
    const nodes = drives.map((d: FileSystemNode) => ({
      title: d.name,
      key: d.path,
      icon: <HddOutlined />,
      isLeaf: false,
    }));
    setTreeData(nodes);
    // Reset loaded/loading keys when reloading drives
    loadedKeysRef.current = new Set();
    loadingKeysRef.current = new Set();
  };

  const onLoadData = async ({ key }: any) => {
    const path = key as string;

    // Skip if already loaded or currently loading
    if (loadedKeysRef.current.has(path) || loadingKeysRef.current.has(path)) {
      return;
    }

    // Mark as loading immediately (synchronous)
    loadingKeysRef.current.add(path);

    try {
      const items = await fileSystem.getList(path);
      const children = items.map((d: FileSystemNode) => ({
        title: d.name,
        key: d.path,
        icon: <FolderOpenOutlined />,
        isLeaf: !d.isDirectory,
      }));

      setTreeData((origin) => updateTreeData(origin, key, children));
      loadedKeysRef.current.add(path);
    } finally {
      loadingKeysRef.current.delete(path);
    }
  };

  const handleOk = () => {
    if (onChange) onChange(selectedPaths);
    onCancel();
  };

  return (
    <Modal title={t('FolderPicker.Title')} open={open} onOk={handleOk} onCancel={onCancel} width={600} okText={t('Dialog.OK')} cancelText={t('Dialog.Cancel')}>
      <Tree
        checkable
        showIcon
        loadData={onLoadData}
        treeData={treeData}
        onCheck={(checkedKeys) => {
            // Antd Tree onCheck returns { checked: [], halfChecked: [] } or just [] depending on config
            // Since we treat folders as independent roots, we just take checked strings
            if (Array.isArray(checkedKeys)) {
                setSelectedPaths(checkedKeys as string[]);
            }
        }}
      />
    </Modal>
  );
};
