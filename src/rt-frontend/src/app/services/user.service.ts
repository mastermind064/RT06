import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { AuthUser, UserRole } from '../models/user.model';

export interface UserSummary {
  UserId: string;
  Username: string;
  Role: UserRole;
  IsActive: boolean;
}

export interface CreateUserRequest {
  Username: string;
  Password: string;
  Role: UserRole;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private http: HttpClient) {}

  list(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${environment.apiUrl}/users`);
  }

  create(payload: CreateUserRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${environment.apiUrl}/auth/register`, payload);
  }

  setActive(userId: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/users/${userId}/status/${isActive}`, { isActive });
  }
}
