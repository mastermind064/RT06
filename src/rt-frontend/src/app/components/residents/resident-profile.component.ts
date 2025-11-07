import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormArray,
  FormBuilder,
  Validators,
  FormGroup,
  FormControl
} from '@angular/forms';
import { DomSanitizer, SafeUrl, SafeResourceUrl } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { ResidentService } from '../../services/resident.service';
import { ResidentFamilyMember, ResidentProfile } from '../../models/resident.model';

// ---- 1. Define typed FormGroup for anggota keluarga ----
type FamilyMemberForm = FormGroup<{
  familyMemberId: FormControl<string>;
  fullName: FormControl<string>;
  birthDate: FormControl<string>;
  gender: FormControl<'L' | 'P'>;
  relationship: FormControl<'ISTRI' | 'SUAMI' | 'ANAK' | 'LAINNYA'>;
}>;

// ---- 2. Define typed FormGroup untuk profil utama warga ----
type ProfileForm = FormGroup<{
  residentId: FormControl<string>;
  nationalIdNumber: FormControl<string>;
  fullName: FormControl<string>;
  birthDate: FormControl<string>;
  gender: FormControl<'L' | 'P'>;
  address: FormControl<string>;
  phoneNumber: FormControl<string>;
  familyMembers: FormArray<FamilyMemberForm>;
}>;

@Component({
  standalone: true,
  selector: 'app-resident-profile',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './resident-profile.component.html'
})
export class ResidentProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly residentService = inject(ResidentService);
  private readonly sanitizer = inject(DomSanitizer);

  apiBase = environment.fileBaseUrl; // contoh: 'https://localhost:51630'
  currentKkUrl?: string;
  currentPicUrl?: string;
  kkPreviewUrl?: SafeResourceUrl;
  picPreviewUrl?: SafeResourceUrl;
  kkDeleted = false;
  picDeleted = false;

  // ⬇️ flag untuk menentukan apakah preview berupa PDF atau gambar
  previewKkIsPdf = false;
  previewPicIsPdf = false;

  blokList: string[] = [
    ...Array.from({ length: 47 }, (_, i) => `P${i + 1}`),
    ...Array.from({ length: 27 }, (_, i) => `Q${i + 1}`),
    ...Array.from({ length: 6 }, (_, i) => `R${i + 1}`),
    'P6A','P5A' // tambahan khusus
  ];

  filteredBlokList = [...this.blokList];

  filterBlok(keyword: string) {
    const lower = keyword.toLowerCase();
    this.filteredBlokList = this.blokList.filter(b => b.toLowerCase().includes(lower));
  }

  loading = false;
  success = false;
  error: string | null = null;

  // ---- 3. Build form dengan nonNullable biar nggak jadi string|null ----
  // Form utama
  form = this.fb.group({
    residentId: this.fb.control<string>('', { nonNullable: true }),
    nationalIdNumber: this.fb.control<string>('', { nonNullable: true, validators: [Validators.required] }),
    fullName: this.fb.control<string>('', { nonNullable: true, validators: [Validators.required] }),
    birthDate: this.fb.control<string>('', { nonNullable: true, validators: [Validators.required] }),
    gender: this.fb.control<'L' | 'P'>('L', { nonNullable: true, validators: [Validators.required] }),
    blok: this.fb.control<string>('', { nonNullable: true }),
    phoneNumber: this.fb.control<string>('', { nonNullable: true }),
    picPath: null as File | null,
    kkDocumentPath: null as File | null,
    approvalStatus: this.fb.control<'PENDING' | 'DRAFT' | 'APPROVED' | 'REJECTED'>('PENDING', { nonNullable: true }),
    approvalNote: this.fb.control<string | null>(null),
    familyMembers: this.fb.array<FormGroup>([])
  });

  get familyMembers(): FormArray<FamilyMemberForm> {
    return this.form.get('familyMembers') as FormArray<FamilyMemberForm>;
  }

  ngOnInit(): void {
    this.loadProfile();
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


  addFamilyMember(member?: Partial<ResidentFamilyMember>): void {
    this.familyMembers.push(this.createFamilyMemberGroup(member));
  }

  removeFamilyMember(index: number): void {
    this.familyMembers.removeAt(index);
  }

  submit(): void {
    if (this.form.invalid) {
        this.form.markAllAsTouched();
        return;
    }

    this.loading = true;
    this.success = false;
    this.error = null;

    // map form (camelCase) to backend model (PascalCase)
    const formData = new FormData();
    const raw = this.form.getRawValue();
    formData.append('residentId', raw.residentId ?? '');
    formData.append('nationalIdNumber', raw.nationalIdNumber ?? '');
    formData.append('fullName', raw.fullName ?? '');
    formData.append('birthDate', raw.birthDate ?? '');
    formData.append('gender', raw.gender ?? '');
    formData.append('blok', raw.blok ?? '');
    formData.append('phoneNumber', raw.phoneNumber ?? '');
    formData.append('approvalStatus', (raw as any).approvalStatus);
    formData.append('approvalNote', (raw as any).approvalNote);
    if (raw.kkDocumentPath) {
      formData.append('kkDocumentPath', raw.kkDocumentPath);
    }
    if (raw.picPath) {
      formData.append('picPath', raw.picPath);
    }
    formData.append('familyMembers', JSON.stringify(raw.familyMembers));

    // flag hapus (Anda bisa tangkap di backend sebagai [FromForm] bool?)
    formData.append('kkDelete', String(this.kkDeleted));
    formData.append('picDelete', String(this.picDeleted));

    this.residentService.updateProfile(formData).subscribe({
      next: () => {
        this.success = true;
        this.loading = false;
      },
      error: () => {
        this.error = 'Gagal menyimpan profil.';
        this.loading = false;
      }
    });
  }

  private loadProfile(): void {
    this.loading = true;
    this.residentService.getProfile().subscribe({
      next: (profile) => this.patchProfile(profile),
      error: (err) => {
        console.error('Failed to load profile', err);
        this.error = 'Gagal memuat profil';
        this.loading = false;
      }
    });
  }

  onFileKkChange(ev: Event) {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) {
      // kalau user batal pilih file, balikin ke kondisi awal
      this.form.patchValue({ kkDocumentPath: null });

      if (this.currentKkUrl) {
        this.kkPreviewUrl =
          this.sanitizer.bypassSecurityTrustResourceUrl(this.currentKkUrl);
        this.previewKkIsPdf = this.isPdf(this.currentKkUrl);
      } else {
        this.kkPreviewUrl = undefined;
        this.previewKkIsPdf = false;
      }
      return;
    }
    // simpan file ke form
    this.form.patchValue({ kkDocumentPath: file });
    this.kkDeleted = false;

    // set preview untuk file baru
    if (file.type === 'application/pdf') {
      const url = URL.createObjectURL(file);
      this.kkPreviewUrl =
        this.sanitizer.bypassSecurityTrustResourceUrl(url);
      this.previewKkIsPdf = true;
    } else {
      const reader = new FileReader();
      reader.onload = () => {
        this.kkPreviewUrl =
          this.sanitizer.bypassSecurityTrustResourceUrl(
            reader.result as string
          );
        this.previewKkIsPdf = false;
      };
      reader.readAsDataURL(file);
    }
    // this.form.patchValue({ kkDocumentPath: file }); // pastikan form control 'kk' ada (type: File|null)
    // const blobUrl = URL.createObjectURL(file);
    // this.kkPreviewUrl = this.sanitizer.bypassSecurityTrustUrl(blobUrl);
    // this.kkDeleted = false; // kalau user pilih file baru, batal hapus
  }

  onFilePicChange(ev: Event) {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) {
      // kalau user batal pilih file, balikin ke kondisi awal
      this.form.patchValue({ picPath: null });

      if (this.currentPicUrl) {
        this.picPreviewUrl =
          this.sanitizer.bypassSecurityTrustResourceUrl(this.currentPicUrl);
        this.previewPicIsPdf = this.isPdf(this.currentPicUrl);
      } else {
        this.picPreviewUrl = undefined;
        this.previewPicIsPdf = false;
      }
      return;
    }

    // simpan file ke form
    this.form.patchValue({ picPath: file });
    this.picDeleted = false;

    // set preview untuk file baru
    if (file.type === 'application/pdf') {
      const url = URL.createObjectURL(file);
      this.picPreviewUrl =
        this.sanitizer.bypassSecurityTrustResourceUrl(url);
      this.previewPicIsPdf = true;
    } else {
      const reader = new FileReader();
      reader.onload = () => {
        this.picPreviewUrl =
          this.sanitizer.bypassSecurityTrustResourceUrl(
            reader.result as string
          );
        this.previewPicIsPdf = false;
      };
      reader.readAsDataURL(file);
    }
    // this.form.patchValue({ picPath: file }); // form control 'pic'
    // const blobUrl = URL.createObjectURL(file);
    // this.picPreviewUrl = this.sanitizer.bypassSecurityTrustUrl(blobUrl);
    // this.picDeleted = false;
  }

  removeKk() {
    // tandai untuk dihapus saat submit
    this.form.patchValue({ kkDocumentPath: null });
    this.kkPreviewUrl = undefined;
    this.currentKkUrl = undefined;
    this.kkDeleted = true;
  }

  removePic() {
    this.form.patchValue({ picPath: null });
    this.picPreviewUrl = undefined;
    this.currentPicUrl = undefined;
    this.picDeleted = true;
  }

  isPdf(url?: string | SafeUrl): boolean {
    if (!url) return false;
    const s = typeof url === 'string' ? url : '';
    return s.toLowerCase().endsWith('.pdf');
  }

  private patchProfile(profile: ResidentProfile): void {
    // console.log('Loaded profile:', profile);
    this.form.patchValue({
      residentId: profile.ResidentId ?? '',
      nationalIdNumber: profile.NationalIdNumber ?? '',
      fullName: profile.FullName ?? '',
      birthDate: this.toDateInputValue(profile.BirthDate),
      gender: this.mapGender(profile.Gender),
      blok: profile.Blok ?? '',
      phoneNumber: profile.PhoneNumber ?? ''
    });

    // simpan URL file saat ini
    this.currentKkUrl = this.fullUrl(profile.KkDocumentPath != null ? profile.KkDocumentPath.toString() : '');
    this.currentPicUrl = this.fullUrl(profile.PicPath != null ? profile.PicPath.toString() : '');
    
    // reset state hapus/preview
    this.kkDeleted = false;
    this.picDeleted = false;
    if (this.currentKkUrl) {
      this.kkPreviewUrl =
        this.sanitizer.bypassSecurityTrustResourceUrl(this.currentKkUrl);
      this.previewKkIsPdf = this.isPdf(this.currentKkUrl);
    } else {
      this.kkPreviewUrl = undefined;
      this.previewKkIsPdf = false;
    }

    if (this.currentPicUrl) {
      this.picPreviewUrl =
        this.sanitizer.bypassSecurityTrustResourceUrl(this.currentPicUrl);
      this.previewPicIsPdf = this.isPdf(this.currentPicUrl);
    } else {
      this.picPreviewUrl = undefined;
      this.previewPicIsPdf = false;
    }

    this.familyMembers.clear();
    profile.FamilyMembers?.forEach(member => this.addFamilyMember(member));

    this.loading = false;
  }

  private createFamilyMemberGroup(member?: Partial<ResidentFamilyMember>): FamilyMemberForm {
    return this.fb.nonNullable.group({
      familyMemberId: this.fb.nonNullable.control(member?.FamilyMemberId ?? ''),
      fullName: this.fb.nonNullable.control(member?.FullName ?? '', Validators.required),
      birthDate: this.fb.nonNullable.control(this.toDateInputValue(member?.BirthDate) ?? '', Validators.required),
      gender: this.fb.nonNullable.control<'L' | 'P'>(member?.Gender ?? 'L', Validators.required),
      relationship: this.fb.nonNullable.control<'ISTRI' | 'SUAMI' | 'ANAK' | 'LAINNYA'>(
        member?.Relationship ?? 'ANAK',
        Validators.required
      )
    });
  }

  // input[type=date] butuh format "YYYY-MM-DD"
  private toDateInputValue(dateIso: string | null | undefined): string {
    if (!dateIso) return '';
    const d = new Date(dateIso);
    // yyyy-mm-dd manual
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0'); // getMonth() 0-based
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // helper: backend kirim "-" tapi form expect "L" / "P"
  private mapGender(value: string | null | undefined): 'L' | 'P' {
    if (value === 'L' || value === 'P') return value;
    // fallback default biar form valid
    return 'L';
  }

  // onFileKkChange(event: Event): void {
  //   const input = event.target as HTMLInputElement;console.log(input);
  //   if (input.files?.length) {
  //     this.form.patchValue({ kkDocumentPath: input.files[0] });
  //     console.log(this.form.value);
  //   }
  // }

  // onFilePicChange(event: Event): void {
  //   const input = event.target as HTMLInputElement;
  //   if (input.files?.length) {
  //     this.form.patchValue({ picPath: input.files[0] });
  //   }
  // }
}
