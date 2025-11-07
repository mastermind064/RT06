import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { AuthUser, UserRole } from '../models/user.model';
import { tap, map } from 'rxjs/operators';
import { Observable } from 'rxjs';

interface LoginResponse {
  Token: string;
  UserId: string;
  Username: string;
  Role: UserRole;
  RtId: string;
  RtRw: string;
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
      const user: AuthUser = JSON.parse(stored);

      // ✅ cek apakah token masih valid
      if (user.token && !this.isTokenExpired(user.token)) {
        this.currentUserSignal.set(user);
      } else {
        this.logout(); // token expired → bersihkan session
      }
    }
  }

  /** 🔐 Login dan simpan token */
  login(request: LoginRequest): Observable<void> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap(res => {
        const user: AuthUser = {
          token: res.Token,
          userId: res.UserId,
          username: res.Username,
          role: res.Role,
          rtId: res.RtId,
          rtRw: res.RtRw
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

  /** 🚪 Logout user & hapus data */
  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.currentUserSignal.set(null);
    this.router.navigate(['/login']);
  }

  /** ✅ Apakah user sudah login dan token masih valid */
  isAuthenticated(): boolean {
    const user = this.currentUserSignal();
    if (!user?.token) return false;

    // cek apakah token expired
    if (this.isTokenExpired(user.token)) {
      this.logout();
      return false;
    }

    return true;
  }

  /** ✅ Cek role */
  hasRole(role: UserRole): boolean {
    return this.currentUserSignal()?.role === role;
  }

  /** ✅ Ambil user aktif */
  currentUser(): AuthUser | null {
    return this.currentUserSignal();
  }

  /** ✅ Ambil token aktif */
  token(): string | null {
    const token = this.currentUserSignal()?.token ?? null;
    if (token && this.isTokenExpired(token)) {
      this.logout();
      return null;
    }
    return token;
  }

  /** 🧠 Cek apakah JWT sudah kedaluwarsa */
  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp;
      if (!exp) return true;

      const now = Math.floor(Date.now() / 1000);
      return exp < now; // true jika sudah lewat waktu
    } catch {
      // token tidak valid / tidak bisa didecode
      return true;
    }
  }

  validateToken(): Observable<boolean> {
    const token = this.token();
    if (!token) {
      return new Observable<boolean>(observer => {
        observer.next(false);
        observer.complete();
      });
    } else {
      return this.isTokenExpired(token)
        ? new Observable<boolean>(observer => {
            observer.next(false);
            observer.complete();
          })
        : new Observable<boolean>(observer => {
            observer.next(true);
            observer.complete();
          });
    }
  }
}
