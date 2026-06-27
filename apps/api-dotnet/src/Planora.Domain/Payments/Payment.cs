using Planora.Domain.Enums;
using Planora.Domain.Shared.Abstractions;
using Planora.Domain.Shared.Results;

namespace Planora.Domain.Payments;

public sealed class Payment : AuditableEntity
{
    public Guid UserId { get; private set; }
    public PaymentGateway Gateway { get; private set; }
    public string GatewayPaymentId { get; private set; } = string.Empty;
    public string GatewayEventId { get; private set; } = string.Empty; // Idempotency key 
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal AmountUsd { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? RawPayload { get; private set; } // Mapped to JSONB 
    public DateTime? RefundedAt { get; private set; }
    public decimal? RefundAmount { get; private set; }
    public string? RefundReason { get; private set; }

    private Payment() { }

    public static Result<Payment> Create(Guid id, Guid userId, PaymentGateway gateway, string paymentId, string eventId, decimal amount, string currency, decimal amountUsd, string? rawPayload)
    {
        if (amount <= 0) return PaymentErrors.InvalidAmount;

        return new Payment
        {
            Id = id,
            UserId = userId,
            Gateway = gateway,
            GatewayPaymentId = paymentId,
            GatewayEventId = eventId,
            Amount = amount,
            Currency = currency,
            AmountUsd = amountUsd,
            RawPayload = rawPayload,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}