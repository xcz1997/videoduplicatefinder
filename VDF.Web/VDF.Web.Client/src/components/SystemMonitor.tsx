import React, { useState, useEffect, useRef } from 'react';
import { Row, Col, Select, Alert } from 'antd';
import { DisconnectOutlined } from '@ant-design/icons';
import { Area } from '@ant-design/charts';
import { monitor, MetricPoint, GpuInfo } from '../api';

interface ChartDataPoint {
  time: string;
  value: number;
  type: string;
}

// Windows 11 Task Manager style colors
const COLORS = {
  cpu: {
    line: '#60cdff',
    fill: 'l(270) 0:rgba(96, 205, 255, 0) 1:rgba(96, 205, 255, 0.3)',
    glow: 'rgba(96, 205, 255, 0.5)',
  },
  gpu: {
    line: '#6ccb5f',
    fill: 'l(270) 0:rgba(108, 203, 95, 0) 1:rgba(108, 203, 95, 0.3)',
    glow: 'rgba(108, 203, 95, 0.5)',
  },
  background: '#1e1e1e',
  cardBg: '#2d2d2d',
  border: '#3d3d3d',
  text: '#ffffff',
  textSecondary: '#999999',
  grid: 'rgba(255, 255, 255, 0.08)',
};

export const SystemMonitor: React.FC = () => {
  const [cpuData, setCpuData] = useState<ChartDataPoint[]>([]);
  const [gpuData, setGpuData] = useState<ChartDataPoint[]>([]);
  const [currentCpu, setCurrentCpu] = useState<number>(0);
  const [currentGpu, setCurrentGpu] = useState<number | null>(null);
  const [gpuAvailable, setGpuAvailable] = useState<boolean>(false);
  const [gpuList, setGpuList] = useState<GpuInfo[]>([]);
  const [selectedGpuId, setSelectedGpuId] = useState<string>('');
  const [selectedGpuName, setSelectedGpuName] = useState<string>('GPU');
  const [connectionError, setConnectionError] = useState<boolean>(false);
  const intervalRef = useRef<number | null>(null);
  const errorCountRef = useRef<number>(0);

  // Load GPU list on mount
  useEffect(() => {
    const loadGpus = async () => {
      try {
        const data = await monitor.getGpus();
        setGpuList(data.gpus || []);
        if (data.selectedGpuId) {
          setSelectedGpuId(data.selectedGpuId);
        }
      } catch (error) {
        console.error('Failed to load GPUs:', error);
      }
    };
    loadGpus();
  }, []);

  const fetchMetrics = async () => {
    try {
      const data = await monitor.getMetrics();
      errorCountRef.current = 0;
      setConnectionError(false);
      setGpuAvailable(data.gpuAvailable);
      if (data.selectedGpuName) {
        setSelectedGpuName(data.selectedGpuName);
      }
      if (data.selectedGpuId && !selectedGpuId) {
        setSelectedGpuId(data.selectedGpuId);
      }

      const cpuPoints: ChartDataPoint[] = data.history.map((p: MetricPoint, idx: number) => ({
        time: idx.toString(),
        value: Math.round(p.cpu * 10) / 10,
        type: 'CPU',
      }));
      setCpuData(cpuPoints);

      if (data.gpuAvailable) {
        const gpuPoints: ChartDataPoint[] = data.history
          .filter((p: MetricPoint) => p.gpu !== null)
          .map((p: MetricPoint, idx: number) => ({
            time: idx.toString(),
            value: Math.round((p.gpu ?? 0) * 10) / 10,
            type: 'GPU',
          }));
        setGpuData(gpuPoints);
      }

      // Update current values
      if (data.history.length > 0) {
        const last = data.history[data.history.length - 1];
        setCurrentCpu(Math.round(last.cpu * 10) / 10);
        setCurrentGpu(last.gpu !== null ? Math.round(last.gpu * 10) / 10 : null);
      }
    } catch {
      errorCountRef.current += 1;
      // Stop polling after 3 consecutive errors
      if (errorCountRef.current >= 3) {
        setConnectionError(true);
        if (intervalRef.current) {
          clearInterval(intervalRef.current);
          intervalRef.current = null;
        }
      }
    }
  };

  useEffect(() => {
    fetchMetrics();
    intervalRef.current = window.setInterval(fetchMetrics, 1000);
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, []);

  const handleGpuChange = async (gpuId: string) => {
    try {
      await monitor.selectGpu(gpuId);
      setSelectedGpuId(gpuId);
      const gpu = gpuList.find(g => g.id === gpuId);
      if (gpu) {
        setSelectedGpuName(gpu.name);
      }
    } catch (error) {
      console.error('Failed to select GPU:', error);
    }
  };

  // Windows 11 Task Manager style chart config
  const createChartConfig = (data: ChartDataPoint[], colorScheme: typeof COLORS.cpu) => ({
    data,
    height: 100,
    autoFit: true,
    xField: 'time',
    yField: 'value',
    xAxis: false,
    yAxis: {
      min: 0,
      max: 100,
      tickCount: 5,
      grid: {
        line: {
          style: {
            stroke: COLORS.grid,
            lineWidth: 1,
            lineDash: [4, 4],
          },
        },
      },
      label: {
        style: {
          fill: COLORS.textSecondary,
          fontSize: 10,
        },
        formatter: (v: string) => `${v}%`,
      },
    },
    smooth: true,
    animation: false,
    tooltip: false,
    color: colorScheme.line,
    areaStyle: {
      fill: colorScheme.fill,
    },
    line: {
      style: {
        stroke: colorScheme.line,
        lineWidth: 2,
        shadowColor: colorScheme.glow,
        shadowBlur: 8,
      },
    },
  });

  // Windows 11 style card
  const cardStyle: React.CSSProperties = {
    background: COLORS.cardBg,
    borderRadius: 8,
    border: `1px solid ${COLORS.border}`,
    padding: '12px 16px',
    height: '100%',
  };

  const headerStyle: React.CSSProperties = {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  };

  const percentageStyle: React.CSSProperties = {
    fontSize: 28,
    fontWeight: 300,
    lineHeight: 1,
    fontFamily: 'Segoe UI, -apple-system, BlinkMacSystemFont, sans-serif',
  };

  const labelStyle: React.CSSProperties = {
    fontSize: 12,
    color: COLORS.textSecondary,
    fontWeight: 400,
  };

  // Don't render if connection error
  if (connectionError) {
    return (
      <Alert
        type="warning"
        showIcon
        icon={<DisconnectOutlined />}
        message="System Monitor Unavailable"
        description="Unable to connect to backend server"
        style={{ marginBottom: 24 }}
      />
    );
  }

  return (
    <div style={{ marginBottom: 24 }}>
      <Row gutter={12}>
        <Col span={gpuAvailable ? 12 : 24}>
          <div style={cardStyle}>
            <div style={headerStyle}>
              <div>
                <div style={labelStyle}>CPU</div>
                <div style={{ ...percentageStyle, color: COLORS.cpu.line }}>
                  {currentCpu}%
                </div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ ...labelStyle, marginBottom: 2 }}>60 seconds</div>
                <div style={{ fontSize: 10, color: COLORS.textSecondary }}>100%</div>
              </div>
            </div>
            <div style={{
              background: COLORS.background,
              borderRadius: 4,
              padding: '8px 4px 4px 4px',
              marginTop: 8,
            }}>
              <Area {...createChartConfig(cpuData, COLORS.cpu)} />
            </div>
            <div style={{
              display: 'flex',
              justifyContent: 'space-between',
              marginTop: 4,
              fontSize: 10,
              color: COLORS.textSecondary,
            }}>
              <span>60s</span>
              <span>0</span>
            </div>
          </div>
        </Col>
        {gpuAvailable && (
          <Col span={12}>
            <div style={cardStyle}>
              <div style={headerStyle}>
                <div>
                  {gpuList.length > 1 ? (
                    <Select
                      size="small"
                      variant="borderless"
                      value={selectedGpuId}
                      onChange={handleGpuChange}
                      style={{
                        minWidth: 120,
                        marginLeft: -8,
                        color: COLORS.text,
                      }}
                      popupClassName="monitor-gpu-dropdown"
                      options={gpuList.map(g => ({ label: g.name, value: g.id }))}
                    />
                  ) : (
                    <div style={labelStyle}>{selectedGpuName}</div>
                  )}
                  <div style={{ ...percentageStyle, color: COLORS.gpu.line }}>
                    {currentGpu !== null ? `${currentGpu}%` : 'N/A'}
                  </div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div style={{ ...labelStyle, marginBottom: 2 }}>60 seconds</div>
                  <div style={{ fontSize: 10, color: COLORS.textSecondary }}>100%</div>
                </div>
              </div>
              <div style={{
                background: COLORS.background,
                borderRadius: 4,
                padding: '8px 4px 4px 4px',
                marginTop: 8,
              }}>
                <Area {...createChartConfig(gpuData, COLORS.gpu)} />
              </div>
              <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                marginTop: 4,
                fontSize: 10,
                color: COLORS.textSecondary,
              }}>
                <span>60s</span>
                <span>0</span>
              </div>
            </div>
          </Col>
        )}
      </Row>
    </div>
  );
};
