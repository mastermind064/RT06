import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe, NgIf, NgFor, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ContributionService, ContributionsQuery, PagedResult } from '../../services/contribution.service';
import { Contribution } from '../../models/contribution.model';
import { environment } from 'src/environments/environment';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-contribution-list',
  imports: [CommonModule, FormsModule, RouterLink, DatePipe, CurrencyPipe, NgIf, NgFor, NgClass],
  templateUrl: './contribution-list.component.html'
})
export class ContributionListComponent implements OnInit {
  private readonly contributionService = inject(ContributionService);
  readonly authService = inject(AuthService);

  contributions: Contribution[] = [];
  total = 0;
  loading = false;
  error: string | null = null;

  // Pagination & filters
  query: ContributionsQuery = { page: 1, pageSize: 10 };
  pageSizes = [10, 20];

  // Column-specific filter inputs (UI helpers)
  filterPeriodStartFrom?: string; // yyyy-MM
  filterPeriodStartTo?: string;   // yyyy-MM
  filterPeriodEndFrom?: string;   // yyyy-MM
  filterPeriodEndTo?: string;     // yyyy-MM
  filterPaymentDateFrom?: string; // yyyy-MM-dd
  filterPaymentDateTo?: string;   // yyyy-MM-dd
  filterAmountMin?: number;
  filterAmountMax?: number;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    const q = this.buildQuery();
    const obs = this.authService.hasRole('ADMIN')
      ? this.contributionService.listAll(q)
      : this.contributionService.listMe(q);
    obs.subscribe({
      next: (data: PagedResult<Contribution>) => {
        this.contributions = data.Items;
        this.total = data.Total;
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.error = 'Tidak dapat memuat data iuran.';
        this.loading = false;
      }
    });
    
  }

  private buildQuery(): ContributionsQuery {
    const q: ContributionsQuery = { ...this.query };
    // Map month inputs to yyyy-MM-01 strings
    const toDate = (m?: string) => (m && m.trim().length ? `${m}-01` : undefined);
    q.periodStartFrom = toDate(this.filterPeriodStartFrom);
    q.periodStartTo = toDate(this.filterPeriodStartTo);
    q.periodEndFrom = toDate(this.filterPeriodEndFrom);
    q.periodEndTo = toDate(this.filterPeriodEndTo);
    q.paymentDateFrom = this.filterPaymentDateFrom || undefined;
    q.paymentDateTo = this.filterPaymentDateTo || undefined;
    q.amountMin = this.filterAmountMin;
    q.amountMax = this.filterAmountMax;
    return q;
  }

  applyFilters(): void {
    this.query.page = 1;
    this.load();
  }

  resetFilters(): void {
    this.query = { page: 1, pageSize: this.query.pageSize };
    this.filterPeriodStartFrom = this.filterPeriodStartTo = undefined;
    this.filterPeriodEndFrom = this.filterPeriodEndTo = undefined;
    this.filterPaymentDateFrom = this.filterPaymentDateTo = undefined;
    this.filterAmountMin = this.filterAmountMax = undefined;
    this.load();
  }

  changePageSize(size: number): void {
    this.query.pageSize = size;
    this.query.page = 1;
    this.load();
  }

  prevPage(): void {
    if ((this.query.page || 1) > 1) {
      this.query.page = (this.query.page || 1) - 1;
      this.load();
    }
  }

  nextPage(): void {
    const totalPages = this.totalPages;
    if ((this.query.page || 1) < totalPages) {
      this.query.page = (this.query.page || 1) + 1;
      this.load();
    }
  }

  get totalPages(): number {
    const size = this.query.pageSize || 10;
    return Math.max(1, Math.ceil(this.total / size));
  }

  approve(contribution: Contribution): void {
    this.contributionService.approve(contribution.ContributionId, {Approve : true, AdminNote: "approve"}).subscribe({
      next: () => this.load()
    });
  }

  reject(contribution: Contribution): void {
    const note = prompt('Alasan penolakan:');
    if (!note) {
      return;
    }
    this.contributionService.reject(contribution.ContributionId, note).subscribe({
      next: () => this.load()
    });
  }

  cancel(contribution: Contribution): void {
    const ok = confirm('Ubah status iuran ini menjadi BATAL?');
    if (!ok) return;
    this.contributionService
      .reject(contribution.ContributionId, 'Dibatalkan oleh admin')
      .subscribe({ next: () => this.load() });
  }

  fullUrl(rel?: string | null): string | undefined {
    if (!rel) return undefined;
    if (rel.startsWith('http://') || rel.startsWith('https://')) return rel;
    return `${environment.fileBaseUrl}${rel}`;
  }
}
