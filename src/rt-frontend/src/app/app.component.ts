import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIf, UpperCasePipe } from '@angular/common';
import { AuthService } from './services/auth.service';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgIf, UpperCasePipe],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  private readonly router = inject(Router);
  readonly authService = inject(AuthService);

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  avatarUrl(): string | undefined {
    const user: any = this.authService.currentUser();//console.log(user);
    const pic = user?.PicPath;
    if (!pic) return undefined;
    if (typeof pic === 'string') return this.fullUrl(pic);
    try {
      if (pic instanceof File) return URL.createObjectURL(pic);
    } catch {}
    const asString = pic?.toString?.();//console.log('asString ', asString);
    return asString ? this.fullUrl(asString) : undefined;
  }

  get initials(): string {
    const name = this.authService.currentUser()?.username ?? '';
    const trimmed = name.trim();
    if (!trimmed) return '';
    const parts = trimmed.split(/\s+/);
    const first = parts[0]?.[0] ?? '';
    const second = parts.length > 1 ? parts[1]?.[0] ?? '' : (trimmed[1] ?? '');
    return (first + second).toUpperCase();
  }

  private fullUrl(rel: string): string {
    if (!rel) return rel;
    if (rel.startsWith('http://') || rel.startsWith('https://')) return rel;
    return `${environment.fileBaseUrl}${rel}`;
  }
}
