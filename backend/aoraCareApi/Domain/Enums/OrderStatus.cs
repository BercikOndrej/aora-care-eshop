namespace aoraCareApi.Domain.Enums;

public enum OrderStatus
{
    AwaitingPayment,
    Paid,
    Shipped,
    Delivered,
    Failed,
    Cancelled,
}
