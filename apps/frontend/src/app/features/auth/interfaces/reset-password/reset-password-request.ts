export interface ResetPasswordRequest {
  userId: string;
  otp: string;
  newPassword: string;
  confirmPassword: string;
}
