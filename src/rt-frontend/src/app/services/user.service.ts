import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { AuthUser, UserRole } from '../models/user.model';

export interface UserSummary {
  userId: string;
  username: string;
  role: UserRole;
  isActive: boolean;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  role: UserRole;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private http: HttpClient) {}

  list(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${environment.apiUrl}/users`);
  }

  create(payload: CreateUserRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${environment.apiUrl}/users`, payload);
  }

  setActive(userId: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/users/${userId}/status`, { isActive });
  }
}
