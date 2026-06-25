export const environment = {
  production: true,

  apiUrl: 'https://n8ng4pqeob.execute-api.us-east-1.amazonaws.com/v1/',

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
