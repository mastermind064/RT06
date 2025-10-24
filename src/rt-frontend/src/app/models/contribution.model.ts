export type ContributionStatus = 'DRAFT' | 'PENDING' | 'APPROVED' | 'REJECTED';

export interface Contribution {
  contributionId: string;
  residentId: string;
  periodStart: string;
  periodEnd: string;
  amountPaid: number;
  paymentDate: string;
  proofImagePath: string;
  status: ContributionStatus;
  adminNote?: string;
}
