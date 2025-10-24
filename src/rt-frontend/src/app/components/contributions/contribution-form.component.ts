import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ContributionService } from '../../services/contribution.service';
import { Contribution } from '../../models/contribution.model';

@Component({
  standalone: true,
  selector: 'app-contribution-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './contribution-form.component.html'
})
export class ContributionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly contributionService = inject(ContributionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  form = this.fb.group({
    contributionId: [''],
    periodStart: ['', Validators.required],
    periodEnd: ['', Validators.required],
    amountPaid: [0, [Validators.required, Validators.min(0)]],
    paymentDate: ['', Validators.required],
    proofFile: [null as File | null]
  });

  isEdit = false;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.loadContribution(id);
    }
  }

  private loadContribution(id: string): void {
    this.loading = true;
    this.contributionService.get(id).subscribe({
      next: (contribution) => {
        this.patchForm(contribution);
        this.loading = false;
      },
      error: () => {
        this.error = 'Tidak dapat memuat data iuran.';
        this.loading = false;
      }
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.form.patchValue({ proofFile: input.files[0] });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const formData = new FormData();
    const raw = this.form.getRawValue();
    formData.append('periodStart', raw.periodStart ?? '');
    formData.append('periodEnd', raw.periodEnd ?? '');
    formData.append('amountPaid', (raw.amountPaid ?? 0).toString());
    formData.append('paymentDate', raw.paymentDate ?? '');
    if (raw.proofFile) {
      formData.append('proof', raw.proofFile);
    }

    this.loading = true;
    const request = this.isEdit && raw.contributionId
      ? this.contributionService.update(raw.contributionId, formData)
      : this.contributionService.create(formData);

    request.subscribe({
      next: () => this.router.navigate(['/contributions']),
      error: () => {
        this.error = 'Gagal menyimpan iuran.';
        this.loading = false;
      }
    });
  }

  private patchForm(contribution: Contribution): void {
    this.form.patchValue({
      contributionId: contribution.contributionId,
      periodStart: contribution.periodStart,
      periodEnd: contribution.periodEnd,
      amountPaid: contribution.amountPaid,
      paymentDate: contribution.paymentDate
    });
  }
}
