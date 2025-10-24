namespace RTMultiTenant.Api.Dtos.Cash;

public class CashExpenseRequest
{
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
}

public class CashExpenseUpdateRequest
{
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
}
