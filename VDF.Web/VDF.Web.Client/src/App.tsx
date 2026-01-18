import React, { useState } from 'react';
import { Layout, Menu, theme, Typography } from 'antd';
import {
  SettingOutlined,
  DashboardOutlined,
  FolderOpenOutlined,
} from '@ant-design/icons';
import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import Settings from './pages/Settings';
import Results from './pages/Results';

const { Header, Sider, Content } = Layout;
const { Title } = Typography;

const MainLayout: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();
  const { token: { colorBgContainer } } = theme.useToken();

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider collapsible collapsed={collapsed} onCollapse={(value) => setCollapsed(value)} theme="light" style={{ borderRight: '1px solid #f0f0f0' }}>
        <div style={{ height: 64, margin: 16, background: 'rgba(22, 119, 255, 0.1)', borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#1677ff', fontWeight: 'bold', fontSize: collapsed ? 12 : 16, overflow: 'hidden', whiteSpace: 'nowrap' }}>
          {collapsed ? 'VDF' : 'Video Dup Finder'}
        </div>
        <Menu
          theme="light"
          selectedKeys={[location.pathname]}
          mode="inline"
          items={[
            { key: '/', icon: <DashboardOutlined />, label: <Link to="/">Dashboard</Link> },
            { key: '/results', icon: <FolderOpenOutlined />, label: <Link to="/results">Results</Link> },
            { key: '/settings', icon: <SettingOutlined />, label: <Link to="/settings">Settings</Link> },
          ]}
        />
      </Sider>
      <Layout>
        <Header style={{ padding: '0 24px', background: colorBgContainer, borderBottom: '1px solid #f0f0f0' }}>
          <Title level={4} style={{ margin: '16px 0' }}>
            {location.pathname === '/settings' ? 'Settings' : location.pathname === '/results' ? 'Results' : 'Dashboard'}
          </Title>
        </Header>
        <Content style={{ margin: '24px 16px', padding: 24, minHeight: 280, background: 'transparent' }}>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/results" element={<Results />} />
            <Route path="/settings" element={<Settings />} />
          </Routes>
        </Content>
      </Layout>
    </Layout>
  );
};

const App: React.FC = () => (
  <Router>
    <MainLayout />
  </Router>
);

export default App;