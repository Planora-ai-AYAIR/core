export interface SignUpResponse {
  id: string;
  email: string;
  phoneNumber: string | null;
  role: string;
  isEmailConfirmed: false;
}
