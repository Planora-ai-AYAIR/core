using Planora.Domain.Shared.Results;

namespace Planora.Domain.Payments;

public static class PaymentErrors
{
    public static readonly Error InvalidAmount = Error.Validation("Payment.Amount.Invalid", "Payment amount must be greater than zero.");
}
