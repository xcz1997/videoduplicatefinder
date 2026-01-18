import React, { useState, useEffect } from 'react';
import { Modal, Tree } from 'antd';
import { FolderOpenOutlined, HddOutlined } from '@ant-design/icons';
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
  const [treeData, setTreeData] = useState<DataNode[]>([]);
  const [selectedPaths, setSelectedPaths] = useState<string[]>(value || []);

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
  };

  const onLoadData = async ({ key }: any) => {
    const path = key as string;
    const items = await fileSystem.getList(path);
    const children = items.map((d: FileSystemNode) => ({
      title: d.name,
      key: d.path,
      icon: <FolderOpenOutlined />,
      isLeaf: !d.isDirectory, // simplified
    }));
    setTreeData((origin) => updateTreeData(origin, key, children));
  };

  const handleOk = () => {
    if (onChange) onChange(selectedPaths);
    onCancel();
  };

  return (
    <Modal title="Select Search Directories" open={open} onOk={handleOk} onCancel={onCancel} width={600}>
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
