import assert from 'node:assert/strict'

import { countReadyStores, countTotalItems, getTrackingInfo } from '../src/utils/orderTracking.js'

function run(name, fn) {
    try {
        fn()
        console.log(`PASS ${name}`)
    } catch (error) {
        console.error(`FAIL ${name}`)
        console.error(error)
        process.exitCode = 1
    }
}

run('countReadyStores only counts stores that have reached pickup-or-later readiness', () => {
    const stores = [
        { status: 'Pending' },
        { status: 'Preparing' },
        { status: 'ReadyForPickup' },
        { status: 'PickedUp' },
        { status: 'OnTheWay' },
        { status: 'Delivered' }
    ]

    assert.equal(countReadyStores(stores), 4)
})

run('countTotalItems sums every item quantity across grouped stores', () => {
    const stores = [
        { items: [{ quantity: 2 }, { quantity: 1 }] },
        { items: [{ quantity: 4 }] },
        { items: [] }
    ]

    assert.equal(countTotalItems(stores), 7)
})

run('getTrackingInfo reports assigned grouped deliveries correctly', () => {
    assert.deepEqual(getTrackingInfo('Assigned', 3, 5), {
        label: 'Courier collecting from stores',
        eta: 'Driver is visiting every store',
        color: '#3B8BD4'
    })
})

if (!process.exitCode) {
    console.log('Web grouped-order smoke tests passed.')
}
