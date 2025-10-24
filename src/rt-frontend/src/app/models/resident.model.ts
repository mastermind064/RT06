export interface ResidentProfile {
  residentId: string;
  nationalIdNumber: string;
  fullName: string;
  birthDate: string;
  gender: 'L' | 'P';
  address: string;
  phoneNumber: string;
  kkDocumentPath?: string;
  approvalStatus: 'DRAFT' | 'PENDING' | 'APPROVED' | 'REJECTED';
  approvalNote?: string;
  familyMembers: ResidentFamilyMember[];
}

export interface ResidentFamilyMember {
  familyMemberId: string;
  fullName: string;
  birthDate: string;
  gender: 'L' | 'P';
  relationship: 'ISTRI' | 'SUAMI' | 'ANAK' | 'LAINNYA';
}
