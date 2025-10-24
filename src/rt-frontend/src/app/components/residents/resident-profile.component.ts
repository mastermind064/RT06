import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormArray, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { ResidentService } from '../../services/resident.service';
import { ResidentFamilyMember, ResidentProfile } from '../../models/resident.model';

type FamilyMemberForm = FormGroup<{
  familyMemberId: import('@angular/forms').FormControl<string>;
  fullName: import('@angular/forms').FormControl<string>;
  birthDate: import('@angular/forms').FormControl<string>;
  gender: import('@angular/forms').FormControl<'L' | 'P'>;
  relationship: import('@angular/forms').FormControl<'ISTRI' | 'SUAMI' | 'ANAK' | 'LAINNYA'>;
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

  form = this.fb.group({
    residentId: [''],
    nationalIdNumber: ['', Validators.required],
    fullName: ['', Validators.required],
    birthDate: ['', Validators.required],
    gender: ['L', Validators.required],
    address: ['', Validators.required],
    phoneNumber: ['', Validators.required],
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
    const payload = {
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
    this.form.patchValue(profile);
    this.familyMembers.clear();
    profile.familyMembers.forEach(member => this.addFamilyMember(member));
    this.loading = false;
  }

  private createFamilyMemberGroup(member?: Partial<ResidentFamilyMember>): FamilyMemberForm {
    return this.fb.nonNullable.group({
      familyMemberId: member?.familyMemberId ?? '',
      fullName: [member?.fullName ?? '', Validators.required],
      birthDate: [member?.birthDate ?? '', Validators.required],
      gender: [member?.gender ?? 'L', Validators.required],
      relationship: [member?.relationship ?? 'ANAK', Validators.required]
    });
  }
}
