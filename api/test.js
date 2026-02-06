// Test endpoint to verify API connectivity
export default async function handler(req, res) {
    res.setHeader('Access-Control-Allow-Origin', '*');

    const testUrl = 'https://sso.arubainstanton.com/aio/api/v1/mfa/validate/full';

    console.log('[Test] Testing connectivity to Aruba API...');

    try {
        // Test 1: Check if we can reach the auth endpoint
        const response = await fetch(testUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
            },
            body: JSON.stringify({
                username: 'test@test.com',
                password: 'testpassword'
            })
        });

        const text = await response.text();
        let parsed;
        try {
            parsed = JSON.parse(text);
        } catch {
            parsed = { raw: text };
        }

        return res.status(200).json({
            success: true,
            timestamp: new Date().toISOString(),
            test: 'Aruba API Connectivity',
            result: {
                status: response.status,
                statusText: response.statusText,
                response: parsed
            },
            message: response.status === 401 || response.status === 400
                ? 'API is reachable (auth rejected as expected with test credentials)'
                : `API returned status ${response.status}`
        });
    } catch (error) {
        return res.status(200).json({
            success: false,
            timestamp: new Date().toISOString(),
            test: 'Aruba API Connectivity',
            error: error.message
        });
    }
}
