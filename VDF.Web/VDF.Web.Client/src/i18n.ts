import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import Backend from 'i18next-http-backend';

i18n
  .use(Backend)
  .use(initReactI18next)
  .init({
    lng: 'zh', // Default language
    fallbackLng: 'en',
    backend: {
      loadPath: '/api/localization/{{lng}}',
    },
    keySeparator: false,
    interpolation: {
      escapeValue: false, // React handles escaping
    },
  });

export default i18n;
