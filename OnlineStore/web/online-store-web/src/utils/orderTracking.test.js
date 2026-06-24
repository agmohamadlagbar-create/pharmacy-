import { countReadyStores, countTotalItems, getTrackingInfo } from './orderTracking'

describe('orderTracking utils', () => {
  it('counts stores that reached pickup-or-later readiness', () => {
    const stores = [
      { status: 'Pending' },
      { status: 'Preparing' },
      { status: 'ReadyForPickup' },
      { status: 'PickedUp' },
      { status: 'OnTheWay' },
      { status: 'Delivered' }
    ]

    expect(countReadyStores(stores)).toBe(4)
  })

  it('sums item quantities across all stores', () => {
    const stores = [
      { items: [{ quantity: 2 }, { quantity: 1 }] },
      { items: [{ quantity: 4 }] },
      { items: [] }
    ]

    expect(countTotalItems(stores)).toBe(7)
  })

  it('returns assigned tracking copy and style metadata', () => {
    expect(getTrackingInfo('Assigned', 3, 5)).toEqual({
      label: 'Courier collecting from stores',
      eta: 'Driver is visiting every store',
      color: '#3B8BD4'
    })
  })
})
