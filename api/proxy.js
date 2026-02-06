// Vercel Serverless Function - Aruba API Proxy
export default async function handler(req, res) {
    // CORS headers
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PATCH, PUT, DELETE, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');

    if (req.method === 'OPTIONS') {
        return res.status(200).end();
    }

    const { target } = req.query;

    if (!target) {
        return res.status(400).json({ error: 'Missing target parameter' });
    }

    // Decode the target URL
    const targetUrl = decodeURIComponent(target);
    console.log(`[Proxy] ${req.method} -> ${targetUrl}`);

    try {
        const headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
        };

        if (req.headers.authorization) {
            headers['Authorization'] = req.headers.authorization;
        }

        const fetchOptions = {
            method: req.method,
            headers,
        };

        if (['POST', 'PATCH', 'PUT'].includes(req.method) && req.body) {
            fetchOptions.body = JSON.stringify(req.body);
            console.log('[Proxy] Body:', JSON.stringify(req.body));
        }

        const response = await fetch(targetUrl, fetchOptions);
        const text = await response.text();

        console.log(`[Proxy] Response: ${response.status}`);
        console.log(`[Proxy] Response body: ${text.substring(0, 500)}`);

        // Forward response
        res.status(response.status);

        try {
            const json = JSON.parse(text);
            return res.json(json);
        } catch {
            return res.send(text);
        }
    } catch (error) {
        console.error('[Proxy] Error:', error);
        return res.status(500).json({ error: error.message });
    }
}
