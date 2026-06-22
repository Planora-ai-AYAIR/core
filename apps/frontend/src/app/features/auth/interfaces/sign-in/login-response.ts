export interface LoginResponse {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: string;
  isEmailConfirmed: boolean;
  accessToken: string;
  refreshToken: string;
}
