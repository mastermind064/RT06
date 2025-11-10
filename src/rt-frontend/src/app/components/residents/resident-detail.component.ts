import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe, NgIf, NgFor } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ResidentService, ResidentDetail } from '../../services/resident.service';
import { environment } from '../../../environments/environment';

@Component({
  standalone: true,
  selector: 'app-resident-detail',
  imports: [CommonModule, RouterLink, DatePipe, NgIf, NgFor],
  templateUrl: './resident-detail.component.html'
})
export class ResidentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly residentService = inject(ResidentService);

  resident?: ResidentDetail;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'Invalid resident id';
      return;
    }
    this.loading = true;
    this.residentService.getById(id).subscribe({
      next: (r) => {
        this.resident = r;
        this.loading = false;
      },
      error: () => {
        this.error = 'Gagal memuat data warga';
        this.loading = false;
      }
    });
  }

  fullUrl(rel?: string | null): string | undefined {
    if (!rel) return undefined;
    if (rel.startsWith('http://') || rel.startsWith('https://')) return rel;
    return `${environment.fileBaseUrl}${rel}`;
  }
}

