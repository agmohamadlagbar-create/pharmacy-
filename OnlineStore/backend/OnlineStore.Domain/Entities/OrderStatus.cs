namespace OnlineStore.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Preparing = 1,
    ReadyForPickup = 2,
    PickedUp = 3,
    OnTheWay = 4,
    Delivered = 5,
    Cancelled = 6
}
