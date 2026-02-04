// Arriba - Aruba Instant On Radio Control
// Pure client-side implementation for GitHub Pages

(function() {
    'use strict';

    // Configuration
    const CONFIG = {
        AUTH_URL: 'https://sso.arubainstanton.com',
        API_URL: 'https://nb.portal.arubainstanton.com/api',
        DEFAULT_SITE_ID: 'f7a52cf3-a421-4438-b2a3-be6ca7551265',
        DEFAULT_DEVICE_ID: '20:9c:b4:c5:dd:be',
        STORAGE_KEY: 'arriba_auth',
        // CORS proxy for browsers that block direct API calls
        CORS_PROXY: 'https://corsproxy.io/?'
    };

    // Storage helper
    const Storage = {
        get() {
            try {
                const data = localStorage.getItem(CONFIG.STORAGE_KEY);
                return data ? JSON.parse(data) : null;
            } catch {
                return null;
            }
        },
        set(data) {
            try {
                localStorage.setItem(CONFIG.STORAGE_KEY, JSON.stringify(data));
            } catch {
                // Storage not available
            }
        },
        clear() {
            try {
                localStorage.removeItem(CONFIG.STORAGE_KEY);
            } catch {
                // Storage not available
            }
        }
    };

    // Debug logger
    const log = {
        info: (...args) => console.log('[Arriba]', ...args),
        error: (...args) => console.error('[Arriba ERROR]', ...args),
        debug: (...args) => console.log('[Arriba DEBUG]', ...args),
    };

    // API helper with CORS proxy
    async function apiRequest(url, options = {}, useProxy = true) {
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        };

        const mergedOptions = {
            ...defaultOptions,
            ...options,
            headers: { ...defaultOptions.headers, ...options.headers }
        };

        // Use CORS proxy
        const finalUrl = useProxy ? CONFIG.CORS_PROXY + encodeURIComponent(url) : url;

        log.info('=== API Request ===');
        log.info('Original URL:', url);
        log.info('Proxy URL:', finalUrl);
        log.info('Method:', mergedOptions.method || 'GET');
        log.info('Headers:', JSON.stringify(mergedOptions.headers, null, 2));
        log.info('Body:', mergedOptions.body);

        try {
            const response = await fetch(finalUrl, mergedOptions);
            log.info('Response status:', response.status, response.statusText);
            log.info('Response headers:', JSON.stringify([...response.headers.entries()], null, 2));
            return response;
        } catch (error) {
            log.error('Fetch error:', error.message);
            throw error;
        }
    }

    // Auth API
    const Auth = {
        async login(email, password) {
            log.info('=== Login Attempt ===');
            log.info('Email:', email);
            log.info('Password length:', password.length);

            const requestBody = {
                username: email,
                password: password
            };

            log.info('Request body object:', JSON.stringify(requestBody, null, 2));
            log.info('Stringified body:', JSON.stringify(requestBody));

            const response = await apiRequest(`${CONFIG.AUTH_URL}/aio/api/v1/mfa/validate/full`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(requestBody)
            });

            log.info('Login response status:', response.status);

            const responseText = await response.text();
            log.info('Login response body:', responseText);

            if (!response.ok) {
                log.error('Login failed:', responseText);
                throw new Error(`Authentication failed: ${responseText}`);
            }

            let data;
            try {
                data = JSON.parse(responseText);
                log.info('Parsed response:', JSON.stringify(data, null, 2));
            } catch (e) {
                log.error('Failed to parse response as JSON:', e);
                throw new Error('Invalid response from server');
            }

            if (!data.access_token) {
                log.error('No access_token in response');
                throw new Error('Invalid response from authentication server');
            }

            const authData = {
                accessToken: data.access_token,
                refreshToken: data.refresh_token,
                expiresAt: Date.now() + (data.expires_in * 1000),
                email: email
            };

            log.info('Auth successful, storing tokens');
            Storage.set(authData);
            return authData;
        },

        async refresh() {
            const auth = Storage.get();
            if (!auth?.refreshToken) {
                throw new Error('No refresh token available');
            }

            const response = await apiRequest(`${CONFIG.AUTH_URL}/aio/api/v1/refresh`, {
                method: 'POST',
                body: JSON.stringify({ refresh_token: auth.refreshToken })
            });

            if (!response.ok) {
                Storage.clear();
                throw new Error('Token refresh failed');
            }

            const data = await response.json();

            const authData = {
                ...auth,
                accessToken: data.access_token,
                refreshToken: data.refresh_token || auth.refreshToken,
                expiresAt: Date.now() + (data.expires_in * 1000)
            };

            Storage.set(authData);
            return authData;
        },

        logout() {
            Storage.clear();
        },

        isAuthenticated() {
            const auth = Storage.get();
            return auth?.accessToken && auth.expiresAt > Date.now();
        },

        getToken() {
            const auth = Storage.get();
            return auth?.accessToken;
        }
    };

    // Aruba API
    const ArubaAPI = {
        async request(endpoint, options = {}) {
            const token = Auth.getToken();
            if (!token) {
                throw new Error('Not authenticated');
            }

            const response = await apiRequest(`${CONFIG.API_URL}${endpoint}`, {
                ...options,
                headers: {
                    ...options.headers,
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.status === 401) {
                // Try to refresh token
                try {
                    await Auth.refresh();
                    return this.request(endpoint, options);
                } catch {
                    Auth.logout();
                    throw new Error('Session expired. Please login again.');
                }
            }

            if (!response.ok) {
                const error = await response.text();
                throw new Error(`API error: ${error}`);
            }

            return response.json();
        },

        async getSites() {
            const data = await this.request('/sites');
            return data.elements || [];
        },

        async getSite(siteId) {
            return this.request(`/sites/${siteId}`);
        },

        async getDevices(siteId) {
            const data = await this.request(`/sites/${siteId}/devices`);
            return data.elements || [];
        },

        async getDevice(siteId, deviceId) {
            return this.request(`/sites/${siteId}/devices/${deviceId}`);
        },

        async getRadios(siteId, deviceId) {
            const data = await this.request(`/sites/${siteId}/devices/${deviceId}/radios`);
            return data.elements || [];
        },

        async toggleRadio(siteId, deviceId, radioId, enabled) {
            return this.request(`/sites/${siteId}/devices/${deviceId}/radios/${radioId}`, {
                method: 'PATCH',
                body: JSON.stringify({ enabled })
            });
        }
    };

    // UI Controller
    const UI = {
        elements: {},

        init() {
            this.elements = {
                loginSection: document.getElementById('login-section'),
                dashboardSection: document.getElementById('dashboard-section'),
                loginForm: document.getElementById('login-form'),
                loginBtn: document.getElementById('login-btn'),
                loginError: document.getElementById('login-error'),
                logoutBtn: document.getElementById('logout-btn'),
                loadingState: document.getElementById('loading-state'),
                errorState: document.getElementById('error-state'),
                errorMessage: document.getElementById('error-message'),
                sitesContainer: document.getElementById('sites-container'),
                sitesList: document.getElementById('sites-list')
            };

            this.bindEvents();
            this.checkAuth();
        },

        bindEvents() {
            this.elements.loginForm.addEventListener('submit', (e) => this.handleLogin(e));
            this.elements.logoutBtn.addEventListener('click', () => this.handleLogout());
        },

        async handleLogin(e) {
            e.preventDefault();

            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;

            this.setLoginLoading(true);
            this.elements.loginError.classList.add('hidden');

            try {
                await Auth.login(email, password);
                this.showDashboard();
                this.loadSites();
            } catch (error) {
                this.elements.loginError.textContent = error.message;
                this.elements.loginError.classList.remove('hidden');
            } finally {
                this.setLoginLoading(false);
            }
        },

        handleLogout() {
            Auth.logout();
            this.showLogin();
            this.elements.sitesList.innerHTML = '';
        },

        setLoginLoading(loading) {
            this.elements.loginBtn.disabled = loading;
            this.elements.loginBtn.querySelector('.btn-text').classList.toggle('hidden', loading);
            this.elements.loginBtn.querySelector('.btn-loading').classList.toggle('hidden', !loading);
        },

        checkAuth() {
            if (Auth.isAuthenticated()) {
                this.showDashboard();
                this.loadSites();
            } else {
                this.showLogin();
            }
        },

        showLogin() {
            this.elements.loginSection.classList.remove('hidden');
            this.elements.dashboardSection.classList.add('hidden');
        },

        showDashboard() {
            this.elements.loginSection.classList.add('hidden');
            this.elements.dashboardSection.classList.remove('hidden');
        },

        async loadSites() {
            this.showLoading();

            try {
                // Try to load the specific site first
                const devices = await ArubaAPI.getDevices(CONFIG.DEFAULT_SITE_ID);

                // Load radios for each device
                const devicesWithRadios = await Promise.all(
                    devices.map(async (device) => {
                        try {
                            const radios = await ArubaAPI.getRadios(CONFIG.DEFAULT_SITE_ID, device.id);
                            return { ...device, radios };
                        } catch {
                            return { ...device, radios: [] };
                        }
                    })
                );

                this.displaySites([{
                    id: CONFIG.DEFAULT_SITE_ID,
                    name: 'Aruba Instant On Site',
                    devices: devicesWithRadios
                }]);
            } catch (error) {
                console.error('Failed to load sites:', error);
                this.showError(error.message || 'Failed to load access points');
            }
        },

        showLoading() {
            this.elements.loadingState.classList.remove('hidden');
            this.elements.errorState.classList.add('hidden');
            this.elements.sitesContainer.classList.add('hidden');
        },

        showError(message) {
            this.elements.loadingState.classList.add('hidden');
            this.elements.errorState.classList.remove('hidden');
            this.elements.sitesContainer.classList.add('hidden');
            this.elements.errorMessage.textContent = message;
        },

        displaySites(sites) {
            this.elements.loadingState.classList.add('hidden');
            this.elements.errorState.classList.add('hidden');
            this.elements.sitesContainer.classList.remove('hidden');
            this.elements.sitesList.innerHTML = '';

            sites.forEach(site => {
                const siteEl = this.createSiteElement(site);
                this.elements.sitesList.appendChild(siteEl);
            });
        },

        createSiteElement(site) {
            const div = document.createElement('div');
            div.className = 'site-section';
            div.innerHTML = `
                <div class="site-header">
                    <h2>${this.escapeHtml(site.name)}</h2>
                </div>
                <div class="devices-grid"></div>
            `;

            const devicesGrid = div.querySelector('.devices-grid');

            if (site.devices && site.devices.length > 0) {
                site.devices.forEach(device => {
                    const deviceCard = this.createDeviceCard(site.id, device);
                    devicesGrid.appendChild(deviceCard);
                });
            } else {
                devicesGrid.innerHTML = '<p style="padding: 1rem; color: var(--gray-600);">No devices found</p>';
            }

            return div;
        },

        createDeviceCard(siteId, device) {
            const template = document.getElementById('device-card-template');
            const card = template.content.cloneNode(true).querySelector('.device-card');

            card.querySelector('.device-name').textContent = device.name || 'Access Point';
            card.querySelector('.device-model').textContent = device.model || 'Unknown Model';
            card.querySelector('.device-mac').textContent = device.macAddress || device.id;
            card.querySelector('.device-serial').textContent = device.serialNumber || 'N/A';

            const status = (device.status || 'unknown').toLowerCase();
            const statusEl = card.querySelector('.device-status');
            statusEl.textContent = status;
            statusEl.className = `device-status ${status === 'up' ? 'online' : status}`;

            const radiosList = card.querySelector('.radios-list');

            if (device.radios && device.radios.length > 0) {
                device.radios.forEach(radio => {
                    const radioControl = this.createRadioControl(siteId, device.id, radio);
                    radiosList.appendChild(radioControl);
                });
            } else {
                radiosList.innerHTML = '<p style="color: var(--gray-500); font-size: 0.9rem;">No radios available</p>';
            }

            return card;
        },

        createRadioControl(siteId, deviceId, radio) {
            const template = document.getElementById('radio-control-template');
            const control = template.content.cloneNode(true).querySelector('.radio-control');

            const band = radio.radioBand || radio.band || 'Unknown';
            control.querySelector('.radio-band').textContent = band;
            control.querySelector('.radio-channel').textContent = `CH ${radio.channel || '?'}`;
            control.querySelector('.radio-power').textContent = `${radio.txPower || radio.transmitPower || '?'}dBm`;

            const toggle = control.querySelector('.radio-toggle');
            toggle.checked = radio.enabled !== false;

            const statusEl = control.querySelector('.radio-status');
            this.updateRadioStatus(statusEl, radio);

            toggle.addEventListener('change', async (e) => {
                const enabled = e.target.checked;
                toggle.disabled = true;
                statusEl.textContent = 'Updating...';

                try {
                    await ArubaAPI.toggleRadio(siteId, deviceId, radio.id, enabled);
                    radio.enabled = enabled;
                    this.updateRadioStatus(statusEl, radio);
                } catch (error) {
                    toggle.checked = !enabled;
                    statusEl.textContent = 'Error';
                    alert(`Failed to toggle radio: ${error.message}`);
                } finally {
                    toggle.disabled = false;
                }
            });

            return control;
        },

        updateRadioStatus(element, radio) {
            const status = radio.enabled !== false ? 'Active' : 'Disabled';
            element.textContent = status;
            element.className = `radio-status ${status.toLowerCase()}`;
        },

        escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text || '';
            return div.innerHTML;
        }
    };

    // Expose loadSites globally for retry button
    window.loadSites = () => UI.loadSites();

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', () => UI.init());
})();
