namespace AoraCare.Domain.Common.Enums;

public enum OrderStatus
{
    AwaitingPayment,
    Paid,
    Shipped,
    Delivered,
    Failed,
    Cancelled,
}
