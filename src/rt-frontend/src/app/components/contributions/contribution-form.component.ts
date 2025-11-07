import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ContributionService } from '../../services/contribution.service';
import { Contribution } from '../../models/contribution.model';
import { SafeResourceUrl, DomSanitizer } from '@angular/platform-browser';
import { environment } from 'src/environments/environment';

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
  private readonly sanitizer = inject(DomSanitizer);

  currentProofUrl?: string;
  proofPreviewUrl?: SafeResourceUrl;
  proofDeleted = false;
  // ⬇️ flag untuk menentukan apakah preview berupa PDF atau gambar
  previewIsPdf = false;

  form = this.fb.group({
    // contributionId: [''],
    // periodStart: ['', Validators.required],
    // periodEnd: ['', Validators.required],
    // amountPaid: [0, [Validators.required, Validators.min(0)]],
    // paymentDate: ['', Validators.required],
    // proofFile: [null as File | null]

    
    contributionId: this.fb.control<string>('', { nonNullable: false }),
    periodStart: this.fb.control<string>('', { nonNullable: false }),
    periodEnd: this.fb.control<string>('', { nonNullable: false }),
    amountPaid: this.fb.control<number>(0, { nonNullable: false }),
    paymentDate: this.fb.control<string>('', { nonNullable: false }),
    proofFile: this.fb.control<File | null>(null, { nonNullable: false })
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
      next: (contribution) => {//console.log(contribution)
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
    const file = input.files?.[0];

    if (!file) {
      // kalau user batal pilih file, balikin ke kondisi awal
      this.form.patchValue({ proofFile: null });

      if (this.currentProofUrl) {
        this.proofPreviewUrl =
          this.sanitizer.bypassSecurityTrustResourceUrl(this.currentProofUrl);
        this.previewIsPdf = this.isPdf(this.currentProofUrl);
      } else {
        this.proofPreviewUrl = undefined;
        this.previewIsPdf = false;
      }
      return;
    }

    // simpan file ke form
    this.form.patchValue({ proofFile: file });
    this.proofDeleted = false;

    // set preview untuk file baru
    if (file.type === 'application/pdf') {
      const url = URL.createObjectURL(file);
      this.proofPreviewUrl =
        this.sanitizer.bypassSecurityTrustResourceUrl(url);
      this.previewIsPdf = true;
    } else {
      const reader = new FileReader();
      reader.onload = () => {
        this.proofPreviewUrl =
          this.sanitizer.bypassSecurityTrustResourceUrl(
            reader.result as string
          );
        this.previewIsPdf = false;
      };
      reader.readAsDataURL(file);
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
      contributionId: contribution.ContributionId,
      periodStart: this.toDateYYYYMM(contribution.PeriodStart),
      periodEnd: this.toDateYYYYMM(contribution.PeriodEnd),
      amountPaid: contribution.AmountPaid,
      paymentDate: this.toDateYYYYMMDD(contribution.PaymentDate)
    });

    // simpan URL file saat ini
    this.currentProofUrl = this.fullUrl(
      contribution.ProofImagePath != null
        ? contribution.ProofImagePath.toString()
        : ''
    );

    // reset state hapus/preview
    this.proofDeleted = false;
    if (this.currentProofUrl) {
      this.proofPreviewUrl =
        this.sanitizer.bypassSecurityTrustResourceUrl(this.currentProofUrl);
      this.previewIsPdf = this.isPdf(this.currentProofUrl);
    } else {
      this.proofPreviewUrl = undefined;
      this.previewIsPdf = false;
    }
  }


  // input[type=date] butuh format "YYYY-MM-DD"
  private toDateYYYYMMDD(dateIso: string | null | undefined): string {
    if (!dateIso) return '';
    const d = new Date(dateIso);
    // yyyy-mm-dd manual
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0'); // getMonth() 0-based
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // input[type=date] butuh format "YYYY-MM-DD"
  private toDateYYYYMM(dateIso: string | null | undefined): string {
    if (!dateIso) return '';
    const d = new Date(dateIso);
    // yyyy-mm-dd manual
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0'); // getMonth() 0-based
    // const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}`;
  }

  removePic() {
    this.form.patchValue({ proofFile: null });
    this.proofPreviewUrl = undefined;
    this.currentProofUrl = undefined;
    this.proofDeleted = true;
    this.previewIsPdf = false;
  }

  private fullUrl(rel?: string | null): string | undefined {
      if (!rel) return undefined;
      // jika rel sudah absolut, langsung pakai
      if (rel.startsWith('http://') || rel.startsWith('https://')) return rel;
      // // jika backend mengirim "/uploads/...."
      // return `${this.apiBase}${rel}`;
      // pakai origin untuk static files
      return `${environment.fileBaseUrl}${rel}`;
  }

  isPdf(url?: string | SafeResourceUrl): boolean {
      // console.log('url:', url);
      if (!url) return false;
      const s = typeof url === 'string' ? url : '';
      // console.log('s:', s);
      // console.log('endsWith .pdf:', s.toLowerCase().endsWith('.pdf'));
      return s.toLowerCase().endsWith('.pdf');
}
}
