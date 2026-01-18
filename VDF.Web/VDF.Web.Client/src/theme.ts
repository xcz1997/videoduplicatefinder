import { ThemeConfig } from 'antd';

export const appTheme: ThemeConfig = {
  token: {
    colorPrimary: '#1677ff', // 经典蓝，专业
    borderRadius: 6,
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
    fontSize: 14,
    colorBgLayout: '#f0f2f5', // 浅灰背景
  },
  components: {
    Layout: {
      headerBg: '#ffffff',
      bodyBg: '#f0f2f5',
    },
    Card: {
      borderRadiusLG: 12, // 更圆润的卡片
      boxShadowTertiary: '0 1px 2px 0 rgba(0, 0, 0, 0.03), 0 1px 6px -1px rgba(0, 0, 0, 0.02), 0 2px 4px 0 rgba(0, 0, 0, 0.02)', // 细腻阴影
    },
    Button: {
      fontWeight: 500,
    }
  },
};
