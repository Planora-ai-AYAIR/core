export const ROUTES = {
  home: '/',
  signIn: '/auth/sign-in',
  signUp: '/auth/sign-up',
  confirmEmail: '/auth/confirm-email',
  forgotPassword: '/auth/forgot-password',
  verifyOtp: '/auth/verify-otp',
  resetPassword: '/auth/reset-password',

  dashboard: '/app/dashboard',

  analysis: '/app/analyses',
  newAnalysis: '/app/analyses/new',
  parcel: '/app/parcels',
  newParcel: '/app/parcels/new',

  terms: '/terms',
  privacy: '/privacy',
};

export const PUBLIC_AUTH_ENDPOINTS = [
  '/auth/login',
  '/auth/register',
  '/auth/verify-otp',
  '/auth/resend-otp',
  '/auth/forgot-password',
  '/auth/reset-password',
  '/auth/refresh-token',
];

export const REG_EXP = {
  EMAIL: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  PASSWORD: /^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$/,
  OTP: /^\d{6}$/,
  OTP_DIGIT: /^[0-9]$/,
} as const;

export const STORAGE_KEYS = {
  AUTH_TOKEN: 'auth_token',
  PARCELS_POINTS: 'parcel_points',
} as const;

export const OTP_CONFIG = {
  LENGTH: 6,
  RESEND_TIMER_SECONDS: 60,
  EXPIRY_MINUTES: 5,
} as const;

export const VALIDATION_CONFIG = {
  MIN_NAME_LENGTH: 3,
  MIN_PASSWORD_LENGTH: 8,
  MIN_AGE: 18,
} as const;

export const AUTH_MESSAGES = {
  REGISTRATION_SUCCESS: 'Registration successful! Please verify your email.',
  REGISTRATION_FAILED: 'Registration failed.',
  UNEXPECTED_ERROR: 'An unexpected error occurred. Please try again.',
  LOGIN_SUCCESS: 'Logged in successfully!',
  LOGIN_FAILED: 'Login failed.',
} as const;

export const VALIDATION_ERROR_MESSAGES = {
  REQUIRED: 'This field is required.',
  EMAIL: 'Please enter a valid email address.',
  PASSWORD_REQUIREMENTS:
    'Password must contain uppercase, lowercase, number, and special character.',
  PHONE_FORMAT: 'Enter a valid Egyptian mobile number (e.g. 01XXXXXXXXX).',
  LICENSE_FORMAT: 'License number must be 5-10 digits.',
  INVALID_FORMAT: 'Invalid format.',
  MIN_LENGTH: (length: number) => `Must be at least ${length} characters.`,
  MIN_AGE: (age: number) => `You must be at least ${age} years old.`,
  MAX_AGE: (age: number) => `You cannot be older than ${age} years.`,
  INVALID_DATE: 'Please enter a valid date.',
} as const;
