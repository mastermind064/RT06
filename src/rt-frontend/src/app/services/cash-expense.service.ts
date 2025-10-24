import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { CashExpense } from '../models/cash-expense.model';

@Injectable({ providedIn: 'root' })
export class CashExpenseService {
  constructor(private http: HttpClient) {}

  list(): Observable<CashExpense[]> {
    return this.http.get<CashExpense[]>(`${environment.apiUrl}/cash-expenses`);
  }

  get(id: string): Observable<CashExpense> {
    return this.http.get<CashExpense>(`${environment.apiUrl}/cash-expenses/${id}`);
  }

  create(expense: Partial<CashExpense>): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/cash-expenses`, expense);
  }

  update(id: string, expense: Partial<CashExpense>): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/cash-expenses/${id}`, expense);
  }
}
