export interface ResidentProfile {
  ResidentId: string;
  NationalIdNumber: string;
  FullName: string;
  BirthDate: string;
  Gender: 'L' | 'P';
  Blok: string;
  PhoneNumber: string;
  Email?: string;
  KkDocumentPath?: File;
  PicPath?: File;
  ApprovalStatus: 'DRAFT' | 'PENDING' | 'APPROVED' | 'REJECTED';
  ApprovalNote?: string;
  FamilyMembers: ResidentFamilyMember[];
}

export interface ResidentFamilyMember {
  FamilyMemberId: string;
  FullName: string;
  BirthDate: string;
  Gender: 'L' | 'P';
  Relationship: 'ISTRI' | 'SUAMI' | 'ANAK' | 'LAINNYA';
}
