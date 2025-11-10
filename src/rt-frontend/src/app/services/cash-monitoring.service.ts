import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface CashMonitoringQuery {
  year: number;
  name?: string;
  blok?: string;
  page?: number;
  pageSize?: number;
}

export interface CashMonitoringRow {
  ResidentId: string;
  FullName: string;
  Blok: string;
  M1: number; M2: number; M3: number; M4: number; M5: number; M6: number;
  M7: number; M8: number; M9: number; M10: number; M11: number; M12: number;
  Total: number;
}

export interface CashMonitoringResult {
  Items: CashMonitoringRow[];
  Total: number;
  Footer: { M1: number; M2: number; M3: number; M4: number; M5: number; M6: number; M7: number; M8: number; M9: number; M10: number; M11: number; M12: number; Total: number };
}

@Injectable({ providedIn: 'root' })
export class CashMonitoringService {
  constructor(private http: HttpClient) {}

  list(query: CashMonitoringQuery) {
    let params = new HttpParams()
      .set('year', String(query.year))
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 10));
    if (query.name) params = params.set('name', query.name);
    if (query.blok) params = params.set('blok', query.blok);
    return this.http.get<CashMonitoringResult>(`${environment.apiUrl}/cash/summary/monitoring`, { params });
  }
}

