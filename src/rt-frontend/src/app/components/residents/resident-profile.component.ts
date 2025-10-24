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

  loading = false;
  success = false;
  error: string | null = null;

  // ---- 3. Build form dengan nonNullable biar nggak jadi string|null ----
  form: ProfileForm = this.fb.nonNullable.group({
    residentId: this.fb.nonNullable.control(''),
    nationalIdNumber: this.fb.nonNullable.control('', Validators.required),
    fullName: this.fb.nonNullable.control('', Validators.required),
    birthDate: this.fb.nonNullable.control('', Validators.required),

    // Perhatikan ini: kita pakai control<'L' | 'P'>
    gender: this.fb.nonNullable.control<'L' | 'P'>('L', Validators.required),

    address: this.fb.nonNullable.control('', Validators.required),
    phoneNumber: this.fb.nonNullable.control('', Validators.required),

    // array of FamilyMemberForm
    familyMembers: this.fb.array<FamilyMemberForm>([])
  });

  get familyMembers(): FormArray<FamilyMemberForm> {
    return this.form.get('familyMembers') as FormArray<FamilyMemberForm>;
  }

  ngOnInit(): void {
    this.loadProfile();
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

    // getRawValue() sekarang sudah strongly typed
    const payload: Partial<ResidentProfile> = {
      ...this.form.getRawValue(),
      familyMembers: this.familyMembers.getRawValue()
    };

    this.residentService.updateProfile(payload).subscribe({
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
      error: () => {
        this.loading = false;
      }
    });
  }

  private patchProfile(profile: ResidentProfile): void {
    this.form.patchValue({
      residentId: profile.residentId ?? '',
      nationalIdNumber: profile.nationalIdNumber ?? '',
      fullName: profile.fullName ?? '',
      birthDate: profile.birthDate ?? '',
      gender: profile.gender ?? 'L',
      address: profile.address ?? '',
      phoneNumber: profile.phoneNumber ?? ''
    });

    this.familyMembers.clear();
    profile.familyMembers.forEach(member => this.addFamilyMember(member));

    this.loading = false;
  }

  private createFamilyMemberGroup(member?: Partial<ResidentFamilyMember>): FamilyMemberForm {
    return this.fb.nonNullable.group({
      familyMemberId: this.fb.nonNullable.control(member?.familyMemberId ?? ''),
      fullName: this.fb.nonNullable.control(member?.fullName ?? '', Validators.required),
      birthDate: this.fb.nonNullable.control(member?.birthDate ?? '', Validators.required),
      gender: this.fb.nonNullable.control<'L' | 'P'>(member?.gender ?? 'L', Validators.required),
      relationship: this.fb.nonNullable.control<'ISTRI' | 'SUAMI' | 'ANAK' | 'LAINNYA'>(
        member?.relationship ?? 'ANAK',
        Validators.required
      )
    });
  }
}
