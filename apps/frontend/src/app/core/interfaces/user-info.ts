export interface UserInfo {
  id: string;
  fullName: string;
  email: string;
  role: 'patient' | 'doctor';
}
