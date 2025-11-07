import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { ResidentProfile } from '../models/resident.model';

@Injectable({ providedIn: 'root' })
export class ResidentService {
  constructor(private http: HttpClient) {}

  getProfile(): Observable<ResidentProfile> {
    return this.http.get<ResidentProfile>(`${environment.apiUrl}/residents/me`);
  }

  updateProfile(profile: FormData): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/residents/me`, profile);
  }
}
