using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Application.Payments;
using PaymentPlugin.Domain.Entities;

namespace PaymentPlugin.Infrastructure.Persistence;

public sealed class EfPaymentRepository(PaymentsDbContext db) : IPaymentRepository
{
    public Task AddAsync(PaymentRecord payment, CancellationToken cancellationToken) =>
        db.Payments.AddAsync(payment, cancellationToken).AsTask();

    public Task<PaymentRecord?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken) =>
        db.Payments.FirstOrDefaultAsync(payment => payment.CorrelationId == correlationId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
