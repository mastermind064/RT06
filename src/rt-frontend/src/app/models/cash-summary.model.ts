export interface CashSummary {
  Year: number;
  Month: number;
  TotalContributionIn: number;
  TotalExpenseOut: number;
  BalanceEnd: number;
}

export interface YearlyCashSummary {
  Year: number;
  TotalContributionIn: number;
  TotalExpenseOut: number;
  BalanceEnd: number;
}

export type CashSummaryResponse = {Monthly: CashSummary, Yearly: YearlyCashSummary};
