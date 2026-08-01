import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend } from 'k6/metrics';

// Step duration metrics (in milliseconds)
const step1LoginDuration = new Trend('duration_step1_login');
const step2CategoriesDuration = new Trend('duration_step2_categories');
const step3BeveragesDuration = new Trend('duration_step3_beverages');
const step4ProfileDuration = new Trend('duration_step4_profile');
const step5LogoutDuration = new Trend('duration_step5_logout');

export const options = {
    // Start with 10 Virtual Users (VUs)
    stages: [
        { duration: '5s', target: 10 },  // Ramp up to 10 users over 5 seconds
        { duration: '20s', target: 10 }, // Stay at 10 users for 20 seconds
        { duration: '5s', target: 0 },   // Ramp down over 5 seconds
    ],
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_EMAIL = __ENV.TEST_EMAIL || 'muhammed0949857914@gmail.com';
const TEST_PASSWORD = __ENV.TEST_PASSWORD || '012345678';

export default function () {
    let token = null;
    let refreshToken = null;
    let beveragesCategoryId = null;

    // ==========================================
    // 1. Step 1: Login
    // ==========================================
    group('Step 1: Login', () => {
        const payload = JSON.stringify({
            email: TEST_EMAIL,
            password: TEST_PASSWORD,
        });

        const params = {
            headers: {
                'Content-Type': 'application/json',
            },
        };

        const res = http.post(`${BASE_URL}/api/auth/login`, payload, params);
        step1LoginDuration.add(res.timings.duration);

        const success = check(res, {
            'Step 1 - Login status 200': (r) => r.status === 200,
            'Step 1 - Token received': (r) => r.json('token') !== undefined,
        });

        if (success && res.status === 200) {
            token = res.json('token');
            refreshToken = res.json('refreshToken');
        }

        sleep(1);
    });

    // ==========================================
    // 2. Step 2: Fetch Categories
    // ==========================================
    group('Step 2: Fetch Categories', () => {
        const res = http.get(`${BASE_URL}/api/categories`);
        step2CategoriesDuration.add(res.timings.duration);

        check(res, {
            'Step 2 - Fetch categories status 200': (r) => r.status === 200,
        });

        try {
            const categories = res.json();
            if (Array.isArray(categories)) {
                // Find "Beverages" category or fallback to first available category
                const bevCategory = categories.find(
                    (c) => c.name && c.name.toLowerCase().includes('beverages')
                );
                if (bevCategory) {
                    beveragesCategoryId = bevCategory.id;
                } else if (categories.length > 0) {
                    beveragesCategoryId = categories[0].id;
                }
            }
        } catch (e) {
            // fallback
        }

        sleep(1);
    });

    // ==========================================
    // 3. Step 3: Open Beverages
    // ==========================================
    group('Step 3: Open Beverages', () => {
        const categoryParam = beveragesCategoryId ? `?categoryId=${beveragesCategoryId}` : '';
        const res = http.get(`${BASE_URL}/api/products${categoryParam}`);
        step3BeveragesDuration.add(res.timings.duration);

        check(res, {
            'Step 3 - Open Beverages products status 200': (r) => r.status === 200,
        });

        sleep(1);
    });

    // ==========================================
    // 4. Step 4: Open Profile Page
    // ==========================================
    group('Step 4: Open Profile Page', () => {
        const params = {
            headers: token ? { Authorization: `Bearer ${token}` } : {},
        };

        const res = http.get(`${BASE_URL}/api/me`, params);
        step4ProfileDuration.add(res.timings.duration);

        check(res, {
            'Step 4 - Profile fetched status 200': (r) => r.status === 200,
        });

        sleep(1);
    });

    // ==========================================
    // 5. Step 5: Logout
    // ==========================================
    group('Step 5: Logout', () => {
        const payload = JSON.stringify({
            refreshToken: refreshToken || '',
        });

        const params = {
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { Authorization: `Bearer ${token}` } : {}),
            },
        };

        const res = http.post(`${BASE_URL}/api/auth/logout`, payload, params);
        step5LogoutDuration.add(res.timings.duration);

        check(res, {
            'Step 5 - Logout status 200': (r) => r.status === 200,
        });

        sleep(1);
    });
}


