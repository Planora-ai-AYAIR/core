export const environment = {
  production: false,

  apiUrl: 'http://planora-ai.runasp.net/api/',

  // APIs
  Auth: {
    login: 'auth/login',
    register: 'auth/register',
    'forgot-password': 'auth/forgot-password',
    'verify-otp': 'auth/verify-otp',
    'resend-otp': 'auth/resend-otp',
    'reset-password': 'auth/reset-password',
    'change-password': 'auth/change-password',
    'refresh-token': 'auth/refresh-token',
    logout: 'auth/logout',
  },
};
