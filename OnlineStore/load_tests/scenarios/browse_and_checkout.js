import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend } from 'k6/metrics';

// Per-endpoint timing metrics
const categoriesDuration = new Trend('duration_categories');
const productsDuration = new Trend('duration_products');
const productInfoDuration = new Trend('duration_product_info');
const profileDuration = new Trend('duration_profile');

// 1. Setup global options
export const options = {
    // A typical load test scenario: ramp up to 10 users over 10 seconds, stay for 30s, ramp down over 10s.
    stages: [
        { duration: '10s', target: 100 },
        { duration: '30s', target: 1000 },
        { duration: '10s', target: 0 },
    ],
};  

const BASE_URL = 'http://lagbar.runasp.net';

export default function () {
    // ==========================================
    // 1. Category Pages API
    // ==========================================
    group('Browse Categories', () => {
        const res = http.get(`${BASE_URL}/api/Categories`);
        categoriesDuration.add(res.timings.duration);
        check(res, {
            'Categories fetched successfully (status 200)': (r) => r.status === 200,
        });
        sleep(1);
    });

    // ==========================================
    // 2. Products API
    // ==========================================
    let productId = 1; // Default fallback ID if no products are returned
    group('Browse Products', () => {
        const res = http.get(`${BASE_URL}/api/Products?page=1&limit=10`);
        productsDuration.add(res.timings.duration);
        check(res, {
            'Products fetched successfully (status 200)': (r) => r.status === 200,
        });

        // Try to dynamically extract a product ID for the product info details test
        try {
            const data = res.json();
            // Handle both flat array and object response wrappers
            const items = Array.isArray(data) ? data : (data.items || data.products || []);
            if (items.length > 0) {
                productId = items[0].id || items[0].productId || 1;
            }
        } catch (e) {
            // Ignore parsing errors and keep fallback
        }
        sleep(1);
    });

    // ==========================================
    // 3. Product Info Details API
    // ==========================================
    group('Browse Product Info', () => {
        const res = http.get(`${BASE_URL}/api/products/${productId}/details`);
        productInfoDuration.add(res.timings.duration);
        check(res, {
            'Product details fetched successfully (status 200)': (r) => r.status === 200,
        });
        sleep(1);
    });

    // ==========================================
    // 4. Profile API (Without Login)
    // ==========================================
    group('Browse Profile', () => {
        // Without authentication header, this is expected to return 401 Unauthorized
        const res = http.get(`${BASE_URL}/api/Me`);
        profileDuration.add(res.timings.duration);
        check(res, {
            'Profile request returned unauthorized status': (r) => r.status === 401,
        });
        sleep(1);
    });
}

