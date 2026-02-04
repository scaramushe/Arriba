// API Client for Arriba backend
const API = {
    baseUrl: '/api',

    // Get stored auth token
    getToken() {
        return localStorage.getItem('arriba_token') || sessionStorage.getItem('arriba_token');
    },

    // Get stored refresh token
    getRefreshToken() {
        return localStorage.getItem('arriba_refresh_token') || sessionStorage.getItem('arriba_refresh_token');
    },

    // Make authenticated request
    async request(endpoint, options = {}) {
        const token = this.getToken();
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            ...options,
            headers
        });

        // Handle token expiration
        if (response.status === 401 && this.getRefreshToken()) {
            const refreshed = await this.refreshToken();
            if (refreshed) {
                // Retry the request with new token
                headers['Authorization'] = `Bearer ${this.getToken()}`;
                return fetch(`${this.baseUrl}${endpoint}`, {
                    ...options,
                    headers
                });
            }
        }

        return response;
    },

    // Authentication
    async login(email, password, remember = true) {
        console.log(`[API] Starting login request for: ${email}`);
        
        try {
            const response = await fetch(`${this.baseUrl}/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });

            console.log(`[API] Login response received: ${response.status} ${response.statusText}`);

            if (!response.ok) {
                const error = await response.json().catch(() => ({ message: 'Login failed' }));
                console.error(`[API] Login failed: ${error.message || 'Unknown error'}`);
                throw new Error(error.message || 'Login failed');
            }

            const data = await response.json();
            console.log('[API] Login successful, storing tokens');

            // Store tokens based on remember preference
            const storage = remember ? localStorage : sessionStorage;
            storage.setItem('arriba_token', data.accessToken);
            storage.setItem('arriba_refresh_token', data.refreshToken);
            storage.setItem('arriba_token_expires', data.expiresAt);

            return data;
        } catch (error) {
            console.error('[API] Login error:', error);
            throw error;
        }
    },

    async refreshToken() {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            console.log('[API] No refresh token available');
            return false;
        }

        try {
            console.log('[API] Attempting to refresh token');
            const response = await fetch(`${this.baseUrl}/auth/refresh`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken })
            });

            if (!response.ok) {
                console.error(`[API] Token refresh failed: ${response.status} ${response.statusText}`);
                this.clearTokens();
                return false;
            }

            const data = await response.json();
            console.log('[API] Token refresh successful');

            // Update stored tokens
            const storage = localStorage.getItem('arriba_token') ? localStorage : sessionStorage;
            storage.setItem('arriba_token', data.accessToken);
            storage.setItem('arriba_refresh_token', data.refreshToken);
            storage.setItem('arriba_token_expires', data.expiresAt);

            return true;
        } catch (error) {
            console.error('[API] Token refresh error:', error);
            this.clearTokens();
            return false;
        }
    },

    logout() {
        console.log('[API] Logging out');
        this.clearTokens();
        fetch(`${this.baseUrl}/auth/logout`, { method: 'POST' }).catch(() => {});
    },

    clearTokens() {
        console.log('[API] Clearing tokens');
        localStorage.removeItem('arriba_token');
        localStorage.removeItem('arriba_refresh_token');
        localStorage.removeItem('arriba_token_expires');
        sessionStorage.removeItem('arriba_token');
        sessionStorage.removeItem('arriba_refresh_token');
        sessionStorage.removeItem('arriba_token_expires');
    },

    // Sites
    async getSites() {
        const response = await this.request('/sites');
        if (!response.ok) {
            throw new Error('Failed to fetch sites');
        }
        return response.json();
    },

    async getSite(siteId, includeDevices = true) {
        const response = await this.request(`/sites/${siteId}?includeDevices=${includeDevices}`);
        if (!response.ok) {
            throw new Error('Failed to fetch site');
        }
        return response.json();
    },

    // Devices
    async getDevices(siteId) {
        const response = await this.request(`/sites/${siteId}/devices`);
        if (!response.ok) {
            throw new Error('Failed to fetch devices');
        }
        return response.json();
    },

    async getDevice(siteId, deviceId) {
        const response = await this.request(`/sites/${siteId}/devices/${deviceId}`);
        if (!response.ok) {
            throw new Error('Failed to fetch device');
        }
        return response.json();
    },

    // Radios
    async getRadios(siteId, deviceId) {
        const response = await this.request(`/sites/${siteId}/devices/${deviceId}/radios`);
        if (!response.ok) {
            throw new Error('Failed to fetch radios');
        }
        return response.json();
    },

    async toggleRadio(siteId, deviceId, radioId, enabled) {
        const response = await this.request(`/sites/${siteId}/devices/${deviceId}/radios/${radioId}/toggle`, {
            method: 'POST',
            body: JSON.stringify({ enabled })
        });
        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Failed to toggle radio' }));
            throw new Error(error.message || 'Failed to toggle radio');
        }
        return response.json();
    },

    async updateRadio(siteId, deviceId, radioId, settings) {
        const response = await this.request(`/sites/${siteId}/devices/${deviceId}/radios/${radioId}`, {
            method: 'PATCH',
            body: JSON.stringify(settings)
        });
        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Failed to update radio' }));
            throw new Error(error.message || 'Failed to update radio');
        }
        return response.json();
    }
};
