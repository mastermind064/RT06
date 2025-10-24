import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe, NgIf, NgFor, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ContributionService } from '../../services/contribution.service';
import { Contribution } from '../../models/contribution.model';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-contribution-list',
  imports: [CommonModule, RouterLink, DatePipe, CurrencyPipe, NgIf, NgFor, NgClass],
  templateUrl: './contribution-list.component.html'
})
export class ContributionListComponent implements OnInit {
  private readonly contributionService = inject(ContributionService);
  readonly authService = inject(AuthService);

  contributions: Contribution[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.contributionService.list().subscribe({
      next: (data) => {
        this.contributions = data;
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.error = 'Tidak dapat memuat data iuran.';
        this.loading = false;
      }
    });
  }

  approve(contribution: Contribution): void {
    this.contributionService.approve(contribution.contributionId).subscribe({
      next: () => this.load()
    });
  }

  reject(contribution: Contribution): void {
    const note = prompt('Alasan penolakan:');
    if (!note) {
      return;
    }
    this.contributionService.reject(contribution.contributionId, note).subscribe({
      next: () => this.load()
    });
  }
}
