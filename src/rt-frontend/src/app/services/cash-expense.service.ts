import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { CashExpense } from '../models/cash-expense.model';

export interface CashExpensesQuery {
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  Items: T[];
  Total: number;
}

@Injectable({ providedIn: 'root' })
export class CashExpenseService {
  constructor(private http: HttpClient) {}

  list(query?: CashExpensesQuery): Observable<PagedResult<CashExpense>> {
    let params = new HttpParams();
    const q = query || {};
    params = params.set('page', String(q.page ?? 1));
    params = params.set('pageSize', String(q.pageSize ?? 10));
    return this.http.get<PagedResult<CashExpense>>(`${environment.apiUrl}/cash/expenses`, { params });
  }

  get(id: string): Observable<CashExpense> {
    return this.http.get<CashExpense>(`${environment.apiUrl}/cash/expenses/${id}`);
  }

  create(expense: Partial<CashExpense>): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/cash/expenses`, expense);
  }

  update(id: string, expense: Partial<CashExpense>): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/cash/expenses/${id}`, expense);
  }

  private toDateInputValue(dateIso: string | null | undefined): string {
    if (!dateIso) return '';
    const d = new Date(dateIso);
    // yyyy-mm-dd manual
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0'); // getMonth() 0-based
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
