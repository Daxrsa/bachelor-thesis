using PaymentPlugin.Domain.Entities;

namespace PaymentPlugin.Application.Payments;

public interface IPaymentRepository
{
    Task AddAsync(PaymentRecord payment, CancellationToken cancellationToken);
    Task<PaymentRecord?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
