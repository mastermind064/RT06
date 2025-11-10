export type UserRole = 'ADMIN' | 'WARGA';

export interface AuthUser {
  userId: string;
  username: string;
  rtId: string;
  role: UserRole;
  token: string;
  rtRw: string;
  PicPath?: string;
}
