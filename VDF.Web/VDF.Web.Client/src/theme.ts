import { ThemeConfig } from 'antd';

// Material Design 3 inspired theme configuration
export const appTheme: ThemeConfig = {
  token: {
    // Primary color - Material Design 3 primary tone
    colorPrimary: '#6750A4',
    colorPrimaryHover: '#7965AF',
    colorPrimaryActive: '#553B9F',

    // Secondary colors
    colorSuccess: '#4CAF50',
    colorWarning: '#FF9800',
    colorError: '#F44336',
    colorInfo: '#2196F3',

    // Surface colors - Material Design 3 surface tones
    colorBgBase: '#FFFBFE',
    colorBgLayout: '#F7F2FA',
    colorBgContainer: '#FFFFFF',
    colorBgElevated: '#FFFFFF',

    // Border and shadow
    borderRadius: 12,
    borderRadiusSM: 8,
    borderRadiusLG: 16,
    borderRadiusXS: 4,

    // Typography
    fontFamily: "'Inter', 'Google Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
    fontSize: 14,
    fontSizeSM: 12,
    fontSizeLG: 16,
    fontSizeHeading1: 32,
    fontSizeHeading2: 24,
    fontSizeHeading3: 20,
    fontSizeHeading4: 16,

    // Line heights
    lineHeight: 1.5714285714285714,
    lineHeightLG: 1.5,
    lineHeightSM: 1.6666666666666667,

    // Spacing
    marginXS: 8,
    marginSM: 12,
    margin: 16,
    marginMD: 20,
    marginLG: 24,
    marginXL: 32,

    paddingXS: 8,
    paddingSM: 12,
    padding: 16,
    paddingMD: 20,
    paddingLG: 24,
    paddingXL: 32,

    // Motion
    motionDurationFast: '0.1s',
    motionDurationMid: '0.2s',
    motionDurationSlow: '0.3s',
    motionEaseInOut: 'cubic-bezier(0.4, 0, 0.2, 1)',
    motionEaseOut: 'cubic-bezier(0, 0, 0.2, 1)',

    // Box shadow - Material Design 3 elevation
    boxShadow: '0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24)',
    boxShadowSecondary: '0 3px 6px rgba(0,0,0,0.15), 0 2px 4px rgba(0,0,0,0.12)',
  },
  components: {
    Layout: {
      headerBg: '#FFFFFF',
      headerPadding: '0 24px',
      bodyBg: '#F7F2FA',
      siderBg: '#FFFFFF',
      triggerBg: '#6750A4',
      triggerColor: '#FFFFFF',
    },
    Card: {
      borderRadiusLG: 16,
      paddingLG: 24,
      boxShadowTertiary: '0 1px 3px rgba(103, 80, 164, 0.08), 0 4px 12px rgba(103, 80, 164, 0.05)',
      headerBg: 'transparent',
      colorBorderSecondary: 'rgba(103, 80, 164, 0.08)',
    },
    Button: {
      fontWeight: 500,
      borderRadius: 20,
      controlHeight: 40,
      controlHeightSM: 32,
      controlHeightLG: 48,
      primaryShadow: '0 2px 4px rgba(103, 80, 164, 0.3)',
    },
    Input: {
      borderRadius: 12,
      controlHeight: 44,
      paddingInline: 16,
    },
    InputNumber: {
      borderRadius: 12,
      controlHeight: 44,
    },
    Select: {
      borderRadius: 12,
      controlHeight: 44,
    },
    Switch: {
      trackHeight: 24,
      trackMinWidth: 44,
      handleSize: 20,
      colorPrimary: '#6750A4',
      colorPrimaryHover: '#7965AF',
    },
    Divider: {
      colorSplit: 'rgba(103, 80, 164, 0.12)',
      marginLG: 32,
    },
    Form: {
      labelColor: 'rgba(0, 0, 0, 0.87)',
      labelFontSize: 14,
      verticalLabelPadding: '0 0 8px',
      itemMarginBottom: 20,
    },
    List: {
      borderRadiusLG: 12,
      colorBorder: 'rgba(103, 80, 164, 0.12)',
    },
    Menu: {
      borderRadius: 12,
      itemBorderRadius: 8,
      itemMarginInline: 8,
      itemPaddingInline: 16,
      colorItemBgSelected: 'rgba(103, 80, 164, 0.12)',
      colorItemTextSelected: '#6750A4',
    },
    Tooltip: {
      borderRadius: 8,
      colorBgSpotlight: '#322F35',
    },
    Message: {
      borderRadiusLG: 12,
    },
    Typography: {
      titleMarginBottom: '0.5em',
      titleMarginTop: '0',
    },
  },
};

// Dark theme variant (Material Design 3 dark scheme)
export const darkTheme: ThemeConfig = {
  token: {
    ...appTheme.token,
    colorPrimary: '#D0BCFF',
    colorPrimaryHover: '#E8DEFF',
    colorPrimaryActive: '#C4B1F7',

    colorBgBase: '#1C1B1F',
    colorBgLayout: '#141218',
    colorBgContainer: '#1C1B1F',
    colorBgElevated: '#2B2930',

    colorText: '#E6E1E5',
    colorTextSecondary: '#CAC4D0',
    colorTextTertiary: '#938F99',
    colorTextQuaternary: '#79747E',

    colorBorder: 'rgba(208, 188, 255, 0.12)',
    colorBorderSecondary: 'rgba(208, 188, 255, 0.08)',
  },
  components: {
    ...appTheme.components,
    Layout: {
      headerBg: '#1C1B1F',
      bodyBg: '#141218',
      siderBg: '#1C1B1F',
      triggerBg: '#D0BCFF',
      triggerColor: '#381E72',
    },
    Card: {
      ...appTheme.components?.Card,
      boxShadowTertiary: '0 1px 3px rgba(0, 0, 0, 0.3), 0 4px 12px rgba(0, 0, 0, 0.2)',
      colorBorderSecondary: 'rgba(208, 188, 255, 0.12)',
    },
  },
};
