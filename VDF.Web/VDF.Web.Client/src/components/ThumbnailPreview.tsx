import React, { useState, useEffect, useCallback } from 'react';
import { Modal, Image, Space } from 'antd';
import { LeftOutlined, RightOutlined } from '@ant-design/icons';

interface ThumbnailPreviewProps {
  visible: boolean;
  onClose: () => void;
  path: string;
  thumbnailCount: number;
  isImage: boolean;
}

const ThumbnailPreview: React.FC<ThumbnailPreviewProps> = ({
  visible,
  onClose,
  path,
  thumbnailCount,
  isImage,
}) => {
  const [currentIndex, setCurrentIndex] = useState(0);

  // Get thumbnail URL - fullsize for main preview, small for selector
  const getThumbnailUrl = (index: number, fullsize: boolean = false) => {
    const base = `/api/scan/thumbnail-single?path=${encodeURIComponent(path)}&index=${index}`;
    return fullsize ? `${base}&fullsize=true` : base;
  };

  const handlePrev = useCallback(() => {
    setCurrentIndex((prev) => (prev > 0 ? prev - 1 : thumbnailCount - 1));
  }, [thumbnailCount]);

  const handleNext = useCallback(() => {
    setCurrentIndex((prev) => (prev < thumbnailCount - 1 ? prev + 1 : 0));
  }, [thumbnailCount]);

  // Reset index when modal opens
  useEffect(() => {
    if (visible) {
      setCurrentIndex(0);
    }
  }, [visible, path]);

  // Keyboard navigation
  useEffect(() => {
    if (!visible) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'ArrowLeft') {
        handlePrev();
      } else if (e.key === 'ArrowRight') {
        handleNext();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [visible, handlePrev, handleNext]);

  // For images, just show the original
  if (isImage) {
    return (
      <Modal
        open={visible}
        onCancel={onClose}
        footer={null}
        width="80%"
        centered
        styles={{ body: { padding: 0, textAlign: 'center', background: '#000' } }}
      >
        <Image
          src={`/api/scan/original?path=${encodeURIComponent(path)}`}
          style={{ maxHeight: '80vh', objectFit: 'contain' }}
          preview={false}
        />
      </Modal>
    );
  }

  // For videos with multiple thumbnails
  return (
    <Modal
      open={visible}
      onCancel={onClose}
      footer={null}
      width="80%"
      centered
      styles={{ body: { padding: 16, background: '#1a1a1a' } }}
    >
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16 }}>
        {/* Main preview image */}
        <div style={{
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          width: '100%',
          minHeight: 400,
        }}>
          {thumbnailCount > 1 && (
            <div
              onClick={handlePrev}
              style={{
                position: 'absolute',
                left: 0,
                top: '50%',
                transform: 'translateY(-50%)',
                cursor: 'pointer',
                padding: '20px 10px',
                color: '#fff',
                fontSize: 24,
                background: 'rgba(0,0,0,0.3)',
                borderRadius: '0 4px 4px 0',
                zIndex: 10,
              }}
            >
              <LeftOutlined />
            </div>
          )}

          <Image
            src={getThumbnailUrl(currentIndex, true)}
            style={{ maxHeight: '60vh', maxWidth: '100%', objectFit: 'contain' }}
            preview={false}
          />

          {thumbnailCount > 1 && (
            <div
              onClick={handleNext}
              style={{
                position: 'absolute',
                right: 0,
                top: '50%',
                transform: 'translateY(-50%)',
                cursor: 'pointer',
                padding: '20px 10px',
                color: '#fff',
                fontSize: 24,
                background: 'rgba(0,0,0,0.3)',
                borderRadius: '4px 0 0 4px',
                zIndex: 10,
              }}
            >
              <RightOutlined />
            </div>
          )}
        </div>

        {/* Thumbnail selector */}
        {thumbnailCount > 1 && (
          <div style={{
            display: 'flex',
            justifyContent: 'center',
            gap: 8,
            flexWrap: 'wrap',
            padding: '8px 0',
          }}>
            <Space size={8} wrap>
              {Array.from({ length: thumbnailCount }, (_, i) => (
                <div
                  key={i}
                  onClick={() => setCurrentIndex(i)}
                  style={{
                    cursor: 'pointer',
                    border: currentIndex === i ? '2px solid #1890ff' : '2px solid transparent',
                    borderRadius: 4,
                    overflow: 'hidden',
                    opacity: currentIndex === i ? 1 : 0.6,
                    transition: 'all 0.2s',
                  }}
                >
                  <img
                    src={getThumbnailUrl(i)}
                    alt={`Thumbnail ${i + 1}`}
                    style={{
                      height: 60,
                      width: 'auto',
                      display: 'block',
                    }}
                  />
                </div>
              ))}
            </Space>
          </div>
        )}

        {/* Index indicator */}
        {thumbnailCount > 1 && (
          <div style={{ color: '#fff', fontSize: 14 }}>
            {currentIndex + 1} / {thumbnailCount}
          </div>
        )}
      </div>
    </Modal>
  );
};

export default ThumbnailPreview;
