import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { AuthUser, UserRole } from '../models/user.model';
import { tap, map } from 'rxjs/operators';
import { Observable } from 'rxjs';

interface LoginResponse {
  token: string;
  userId: string;
  username: string;
  role: UserRole;
  rtId: string;
}

interface LoginRequest {
  username: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'rt-auth';
  private readonly currentUserSignal = signal<AuthUser | null>(null);

  constructor(private http: HttpClient, private router: Router) {
    const stored = localStorage.getItem(this.storageKey);
    if (stored) {
      this.currentUserSignal.set(JSON.parse(stored));
    }
  }

  login(request: LoginRequest): Observable<void> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap(res => {
        const user: AuthUser = {
          token: res.token,
          userId: res.userId,
          username: res.username,
          role: res.role,
          rtId: res.rtId
        };
        localStorage.setItem(this.storageKey, JSON.stringify(user));
        this.currentUserSignal.set(user);
      }),
      tap(() => {
        this.router.navigate(['/dashboard']);
      }),
      map(() => void 0)
    );
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.currentUserSignal.set(null);
  }

  isAuthenticated(): boolean {
    return this.currentUserSignal() !== null;
  }

  hasRole(role: UserRole): boolean {
    return this.currentUserSignal()?.role === role;
  }

  currentUser(): AuthUser | null {
    return this.currentUserSignal();
  }

  token(): string | null {
    return this.currentUserSignal()?.token ?? null;
  }
}
