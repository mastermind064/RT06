import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { CashSummaryService } from '../../services/cash-summary.service';
import { CashSummary, YearlyCashSummary } from '../../models/cash-summary.model';

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly summaryService = inject(CashSummaryService);
  private readonly fb = inject(FormBuilder);

  summary?: CashSummary;
  yearlySummary?: YearlyCashSummary;
  loading = false;

  filterForm = this.fb.nonNullable.group({
    year: new Date().getFullYear(),
    month: new Date().getMonth() + 1
  });

  ngOnInit(): void {
    this.loadSummary();
    this.filterForm.valueChanges.subscribe(() => this.loadSummary());
  }

  private loadSummary(): void {
    const { year, month } = this.filterForm.getRawValue();
    this.loading = true;
    this.summaryService.getMonthlySummary(year, month).subscribe({
      next: (summary) => {//console.log(summary);
        this.summary = summary.Monthly;
        this.yearlySummary = summary.Yearly;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }
}
