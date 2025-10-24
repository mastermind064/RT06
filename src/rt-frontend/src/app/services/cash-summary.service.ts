import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { CashSummary } from '../models/cash-summary.model';

@Injectable({ providedIn: 'root' })
export class CashSummaryService {
  constructor(private http: HttpClient) {}

  getMonthlySummary(year: number, month: number): Observable<CashSummary> {
    const params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());
    return this.http.get<CashSummary>(`${environment.apiUrl}/cash/summary`, { params });
  }
}
