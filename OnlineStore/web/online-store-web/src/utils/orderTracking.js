export function countReadyStores(stores = []) {
    const readyStatuses = new Set(['ReadyForPickup', 'PickedUp', 'OnTheWay', 'Delivered'])
    return stores.filter(store => readyStatuses.has(store?.status)).length
}

export function countTotalItems(stores = []) {
    return stores.reduce(
        (sum, store) => sum + (store.items || []).reduce((storeSum, item) => storeSum + (item.quantity || 0), 0),
        0
    )
}

export function getTrackingInfo(status, readyStoreCount, storeCount, cancellationReason = null, cancelledByRole = null) {
    switch ((status || '').toLowerCase()) {
        case 'pending':
            return { label: 'Waiting for stores', eta: '', color: '#EF9F27' }
        case 'preparing':
            return { label: 'Stores are preparing', eta: '', color: '#3B82F6' }
        case 'readyforpickup':
            return { label: 'All stores are ready', eta: 'Waiting for driver assignment', color: '#22C55E' }
        case 'assigned':
            return { label: 'Courier collecting from stores', eta: 'Driver is visiting every store', color: '#3B8BD4' }
        case 'pickedup':
            return { label: 'All items picked up', eta: 'Courier is almost on the way', color: '#8B5CF6' }
        case 'ontheway':
            return { label: 'On the way', eta: 'Courier is heading to your address', color: '#06B6D4' }
        case 'delivered':
            return { label: 'Delivered', eta: 'Delivered just now', color: '#639922' }
        case 'cancelled': {
            const isShop = cancelledByRole?.toLowerCase() === 'shop'
            const title = isShop ? 'Order Declined by Store' : 'Order Cancelled'
            const reasonText = cancellationReason ? `Reason: ${cancellationReason}` : 'Order was cancelled'
            return { label: title, eta: reasonText, color: '#E8593C' }
        }
        default:
            return { label: 'Processing', eta: '', color: '#999' }
    }
}
