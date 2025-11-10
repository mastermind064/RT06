import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { ResidentProfile } from '../models/resident.model';

export interface ResidentsQuery {
  page?: number;
  pageSize?: number;
  name?: string;
  blok?: string;
  status?: string;
}

export interface ResidentListItem {
  ResidentId: string;
  FullName: string;
  Blok: string;
  PhoneNumber?: string;
  ApprovalStatus: string;
}

export interface PagedResult<T> {
  Items: T[];
  Total: number;
}

export interface ResidentDetail {
  ResidentId: string;
  NationalIdNumber: string;
  FullName: string;
  BirthDate: string;
  Gender: 'L' | 'P';
  Blok: string;
  PhoneNumber: string;
  KkDocumentPath?: string;
  PicPath?: string;
  ApprovalStatus: 'DRAFT' | 'PENDING' | 'APPROVED' | 'REJECTED';
  ApprovalNote?: string;
  FamilyMembers: Array<{
    FamilyMemberId: string;
    FullName: string;
    BirthDate: string;
    Gender: 'L' | 'P';
    Relationship: 'ISTRI' | 'SUAMI' | 'ANAK' | 'LAINNYA';
  }>;
}

@Injectable({ providedIn: 'root' })
export class ResidentService {
  constructor(private http: HttpClient) {}

  getProfile(): Observable<ResidentProfile> {
    return this.http.get<ResidentProfile>(`${environment.apiUrl}/residents/me`);
  }

  updateProfile(profile: FormData): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/residents/me`, profile);
  }

  list(query?: ResidentsQuery) {
    let params = new HttpParams();
    const q = query || {};
    params = params.set('page', String(q.page ?? 1));
    params = params.set('pageSize', String(q.pageSize ?? 10));
    if (q.name) params = params.set('name', q.name);
    if (q.blok) params = params.set('blok', q.blok);
    if (q.status) params = params.set('status', q.status);
    return this.http.get<PagedResult<ResidentListItem>>(`${environment.apiUrl}/residents`, { params });
  }

  getById(residentId: string) {
    return this.http.get<ResidentDetail>(`${environment.apiUrl}/residents/${residentId}`);
  }

  review(residentId: string, approve: boolean, note?: string) {
    return this.http.post(`${environment.apiUrl}/residents/${residentId}/approval`, {
      approve,
      note
    });
  }
}
