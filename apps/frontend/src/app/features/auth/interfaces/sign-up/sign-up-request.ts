export interface SignUpRequest {
  email: string;
  password: string;
  phoneNumber?: string | null;
  firstName: string;
  lastName: string;
}
