export type ContributionStatus = 'DRAFT' | 'PENDING' | 'APPROVED' | 'REJECTED';

export interface Contribution {
  ContributionId: string;
  ResidentId: string;
  PeriodStart: string;
  PeriodEnd: string;
  AmountPaid: number;
  PaymentDate: string;
  ProofImagePath: string;
  Status: ContributionStatus;
  AdminNote?: string;
}
