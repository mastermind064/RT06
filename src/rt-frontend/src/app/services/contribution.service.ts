import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { Contribution } from '../models/contribution.model';

export interface ContributionsQuery {
  page?: number;
  pageSize?: number;
  status?: string;
  blok?: string;
  adminNote?: string;
  paymentDateFrom?: string; // ISO yyyy-MM-dd
  paymentDateTo?: string;
  amountMin?: number;
  amountMax?: number;
  periodStartFrom?: string; // ISO yyyy-MM-dd
  periodStartTo?: string;
  periodEndFrom?: string;
  periodEndTo?: string;
}

export interface PagedResult<T> {
  Items: T[];
  Total: number;
}

@Injectable({ providedIn: 'root' })
export class ContributionService {
  constructor(private http: HttpClient) {}

  listMe(query?: ContributionsQuery): Observable<PagedResult<Contribution>> {
    return this.http.get<PagedResult<Contribution>>(
      `${environment.apiUrl}/contributions/me`,
      { params: this.buildParams(query) }
    );
  }

  listAll(query?: ContributionsQuery): Observable<PagedResult<Contribution>> {
    return this.http.get<PagedResult<Contribution>>(
      `${environment.apiUrl}/contributions`,
      { params: this.buildParams(query) }
    );
  }

  private buildParams(query?: ContributionsQuery): HttpParams {
    let params = new HttpParams();
    const q = query || {};

    const add = (key: string, value: any) => {
      if (value === undefined || value === null) return;
      if (typeof value === 'string' && value.trim() === '') return;
      params = params.set(key, String(value));
    };

    // Always include paging (with defaults)
    add('page', q.page ?? 1);
    add('pageSize', q.pageSize ?? 10);

    add('status', q.status);
    add('blok', q.blok);
    add('adminNote', q.adminNote);
    add('paymentDateFrom', q.paymentDateFrom);
    add('paymentDateTo', q.paymentDateTo);
    add('amountMin', q.amountMin);
    add('amountMax', q.amountMax);
    add('periodStartFrom', q.periodStartFrom);
    add('periodStartTo', q.periodStartTo);
    add('periodEndFrom', q.periodEndFrom);
    add('periodEndTo', q.periodEndTo);

    return params;
  }

  get(id: string): Observable<Contribution> {
    return this.http.get<Contribution>(`${environment.apiUrl}/contributions/${id}/edit`);
  }

  create(payload: FormData): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/contributions`, payload);
  }

  update(id: string, payload: FormData): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/contributions/${id}`, payload);
  }

  approve(id: string, payload: any): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/contributions/${id}/review`, payload);
  }

  reject(id: string, note: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/contributions/${id}/reject`, { note });
  }
}
