namespace App.DTO.v1.Venues.Common;

public class MoneyDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
}
