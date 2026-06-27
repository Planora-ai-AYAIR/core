export const environment = {
  production: false,

  apiUrl: 'https://n8ng4pqeob.execute-api.us-east-1.amazonaws.com/v1/api/',
  // apiUrl: 'https://planora-ai.runasp.net/api/',

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

  Parcels: {
    create: 'parcels',
    list: 'parcels',
    details: (id: string) => `parcels/${id}`,
    delete: (id: string) => `parcels/${id}`,
  },

  Analysis: {
    start: (parcelId: string) => `parcels/${parcelId}/analysis`,
    status: (parcelId: string) => `parcels/${parcelId}/analysis-status`,
    topography: (parcelId: string) => `topography/${parcelId}`,
    soil: (parcelId: string) => `soil/${parcelId}`,
    risk: (parcelId: string) => `risk/${parcelId}`,
    borehole: (parcelId: string) => `borehole/${parcelId}`,
    dashboard: `analysis/jobs`,
  },

  Notifications: {
    list: 'notifications',
    markAsRead: (id: string) => `notifications/${id}/read`,
  },
};
