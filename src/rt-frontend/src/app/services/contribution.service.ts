import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { Contribution } from '../models/contribution.model';

@Injectable({ providedIn: 'root' })
export class ContributionService {
  constructor(private http: HttpClient) {}

  list(): Observable<Contribution[]> {
    return this.http.get<Contribution[]>(`${environment.apiUrl}/contributions`);
  }

  get(id: string): Observable<Contribution> {
    return this.http.get<Contribution>(`${environment.apiUrl}/contributions/${id}`);
  }

  create(payload: FormData): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/contributions`, payload);
  }

  update(id: string, payload: FormData): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/contributions/${id}`, payload);
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/contributions/${id}/approve`, {});
  }

  reject(id: string, note: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/contributions/${id}/reject`, { note });
  }
}
