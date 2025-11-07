import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { CashExpense } from '../models/cash-expense.model';

@Injectable({ providedIn: 'root' })
export class CashExpenseService {
  constructor(private http: HttpClient) {}

  list(): Observable<CashExpense[]> {
    return this.http.get<CashExpense[]>(`${environment.apiUrl}/cash/expenses`);
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
