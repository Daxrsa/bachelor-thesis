using Microsoft.EntityFrameworkCore;
using OrderPlugin.Application.Orders;
using OrderPlugin.Domain.Entities;

namespace OrderPlugin.Infrastructure.Persistence;

public sealed class EfOrderRepository(OrdersDbContext db) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        db.Orders.AddAsync(order, cancellationToken).AsTask();

    public Task<Order?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken) =>
        db.Orders.Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.PaymentId == paymentId, cancellationToken);

    public Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken) =>
        db.Orders.Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public Task<Order?> GetByIdForUserAsync(string orderId, string userId, CancellationToken cancellationToken) =>
        db.Orders.Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId && order.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await db.Orders.Include(order => order.Items)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListAllAsync(CancellationToken cancellationToken) =>
        await db.Orders.Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
