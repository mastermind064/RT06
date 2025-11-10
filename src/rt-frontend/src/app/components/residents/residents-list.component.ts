import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, NgIf, NgFor, NgClass, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ResidentService, ResidentsQuery, PagedResult, ResidentListItem } from '../../services/resident.service';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-residents-list',
  imports: [CommonModule, FormsModule, RouterLink, NgIf, NgFor, NgClass, DatePipe],
  templateUrl: './residents-list.component.html'
})
export class ResidentsListComponent implements OnInit {
  private readonly residentService = inject(ResidentService);
  readonly authService = inject(AuthService);

  residents: ResidentListItem[] = [];
  total = 0;
  loading = false;
  error: string | null = null;

  query: ResidentsQuery = { page: 1, pageSize: 10 };
  pageSizes = [10, 20];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.residentService.list(this.query).subscribe({
      next: (res: PagedResult<ResidentListItem>) => {
        this.residents = res.Items;
        this.total = res.Total;
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.error = 'Tidak dapat memuat data warga.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.query.page = 1;
    this.load();
  }

  resetFilters(): void {
    this.query = { page: 1, pageSize: this.query.pageSize };
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

  approve(residentId: string): void {
    this.residentService.review(residentId, true).subscribe({
      next: () => this.load()
    });
  }

  reject(residentId: string): void {
    const note = prompt('Alasan penolakan:');
    if (!note) return;
    this.residentService.review(residentId, false, note).subscribe({
      next: () => this.load()
    });
  }
}
