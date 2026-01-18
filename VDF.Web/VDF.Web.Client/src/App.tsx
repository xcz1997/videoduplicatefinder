import React, { useState } from 'react';
import { Layout, Menu, theme, Typography, Select, Space } from 'antd';
import {
  SettingOutlined,
  DashboardOutlined,
  FolderOpenOutlined,
  GlobalOutlined,
} from '@ant-design/icons';
import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useRequest } from 'ahooks';
import { localization } from './api';
import Dashboard from './pages/Dashboard';
import Settings from './pages/Settings';
import Results from './pages/Results';

const { Header, Sider, Content } = Layout;
const { Title } = Typography;

const MainLayout: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();
  const { token: { colorBgContainer } } = theme.useToken();
  const { t, i18n } = useTranslation();
  
  const { data: languages } = useRequest(localization.getList);

  const getPageTitle = (path: string) => {
    switch(path) {
        case '/settings': return t('Settings.Tab.Settings');
        case '/results': return 'Results'; // Key missing in current json, need fallback
        default: return t('Settings.Tab.Scanner'); // Reuse existing keys
    }
  }

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
            { key: '/', icon: <DashboardOutlined />, label: <Link to="/">{t('Settings.Tab.Scanner')}</Link> },
            { key: '/results', icon: <FolderOpenOutlined />, label: <Link to="/results">Results</Link> },
            { key: '/settings', icon: <SettingOutlined />, label: <Link to="/settings">{t('Settings.Tab.Settings')}</Link> },
          ]}
        />
      </Sider>
      <Layout>
        <Header style={{ padding: '0 24px', background: colorBgContainer, borderBottom: '1px solid #f0f0f0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Title level={4} style={{ margin: '16px 0' }}>
            {getPageTitle(location.pathname)}
          </Title>
          <Space>
             <GlobalOutlined />
             <Select 
                value={i18n.language} 
                onChange={(val) => i18n.changeLanguage(val)}
                options={(languages || ['en']).map((lang: string) => ({ value: lang, label: lang.toUpperCase() }))}
                style={{ width: 100 }}
             />
          </Space>
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