// Main Application
document.addEventListener('DOMContentLoaded', () => {
    // DOM Elements
    const loginSection = document.getElementById('login-section');
    const dashboardSection = document.getElementById('dashboard-section');
    const loginForm = document.getElementById('login-form');
    const loginBtn = document.getElementById('login-btn');
    const loginError = document.getElementById('login-error');
    const logoutBtn = document.getElementById('logout-btn');
    const loadingState = document.getElementById('loading-state');
    const errorState = document.getElementById('error-state');
    const errorMessage = document.getElementById('error-message');
    const sitesContainer = document.getElementById('sites-container');
    const sitesList = document.getElementById('sites-list');

    // Default site ID from the portal URL
    const DEFAULT_SITE_ID = 'f7a52cf3-a421-4438-b2a3-be6ca7551265';

    // Initialize app
    init();

    function init() {
        // Check if already authenticated
        if (Auth.isAuthenticated()) {
            showDashboard();
            loadSites();
        } else {
            showLogin();
        }

        // Start auto-refresh of tokens
        Auth.startAutoRefresh();

        // Setup event listeners
        setupEventListeners();
    }

    function setupEventListeners() {
        // Login form
        loginForm.addEventListener('submit', handleLogin);

        // Logout button
        logoutBtn.addEventListener('click', handleLogout);
    }

    async function handleLogin(e) {
        e.preventDefault();

        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;
        const remember = document.getElementById('remember-me').checked;

        console.log('[App] Login form submitted');

        // Disable button and show loading
        loginBtn.disabled = true;
        loginBtn.querySelector('.btn-text').classList.add('hidden');
        loginBtn.querySelector('.btn-loading').classList.remove('hidden');
        loginError.classList.add('hidden');

        try {
            console.log('[App] Calling API.login...');
            await API.login(email, password, remember);
            console.log('[App] Login successful, showing dashboard');
            showDashboard();
            loadSites();
        } catch (error) {
            console.error('[App] Login failed:', error);
            loginError.textContent = error.message || 'Login failed. Please try again.';
            loginError.classList.remove('hidden');
        } finally {
            console.log('[App] Restoring login button state');
            loginBtn.disabled = false;
            loginBtn.querySelector('.btn-text').classList.remove('hidden');
            loginBtn.querySelector('.btn-loading').classList.add('hidden');
        }
    }

    function handleLogout() {
        API.logout();
        showLogin();
        // Clear the sites list
        sitesList.innerHTML = '';
    }

    function showLogin() {
        loginSection.classList.remove('hidden');
        dashboardSection.classList.add('hidden');
    }

    function showDashboard() {
        loginSection.classList.add('hidden');
        dashboardSection.classList.remove('hidden');
    }

    async function loadSites() {
        showLoading();

        try {
            // Try to load the specific site from the URL first
            const site = await API.getSite(DEFAULT_SITE_ID, true);
            displaySites([site]);
        } catch (error) {
            // If specific site fails, try to get all sites
            try {
                const sites = await API.getSites();
                if (sites.length === 0) {
                    showError('No sites found in your account');
                    return;
                }

                // Load devices for each site
                const sitesWithDevices = await Promise.all(
                    sites.map(async (site) => {
                        try {
                            return await API.getSite(site.id, true);
                        } catch {
                            return site;
                        }
                    })
                );

                displaySites(sitesWithDevices);
            } catch (err) {
                showError(err.message || 'Failed to load sites');
            }
        }
    }

    function showLoading() {
        loadingState.classList.remove('hidden');
        errorState.classList.add('hidden');
        sitesContainer.classList.add('hidden');
    }

    function showError(message) {
        loadingState.classList.add('hidden');
        errorState.classList.remove('hidden');
        sitesContainer.classList.add('hidden');
        errorMessage.textContent = message;
    }

    function displaySites(sites) {
        loadingState.classList.add('hidden');
        errorState.classList.add('hidden');
        sitesContainer.classList.remove('hidden');
        sitesList.innerHTML = '';

        sites.forEach(site => {
            const siteElement = createSiteElement(site);
            sitesList.appendChild(siteElement);
        });
    }

    function createSiteElement(site) {
        const siteDiv = document.createElement('div');
        siteDiv.className = 'site-section';
        siteDiv.dataset.siteId = site.id;

        siteDiv.innerHTML = `
            <div class="site-header">
                <h2>${escapeHtml(site.name)}</h2>
                ${site.description ? `<p>${escapeHtml(site.description)}</p>` : ''}
            </div>
            <div class="devices-grid"></div>
        `;

        const devicesGrid = siteDiv.querySelector('.devices-grid');

        if (site.devices && site.devices.length > 0) {
            site.devices.forEach(device => {
                const deviceCard = createDeviceCard(site.id, device);
                devicesGrid.appendChild(deviceCard);
            });
        } else {
            devicesGrid.innerHTML = '<p style="padding: 1rem; color: var(--gray-600);">No devices found</p>';
        }

        return siteDiv;
    }

    function createDeviceCard(siteId, device) {
        const template = document.getElementById('device-card-template');
        const card = template.content.cloneNode(true).querySelector('.device-card');

        card.dataset.deviceId = device.id;
        card.querySelector('.device-name').textContent = device.name;
        card.querySelector('.device-model').textContent = device.model;
        card.querySelector('.device-mac').textContent = device.macAddress;
        card.querySelector('.device-serial').textContent = device.serialNumber;

        const statusEl = card.querySelector('.device-status');
        const statusMap = { 0: 'Online', 1: 'Offline', 2: 'Updating', 3: 'Unknown' };
        const statusText = statusMap[device.status] || device.status;
        statusEl.textContent = statusText;
        statusEl.className = `device-status ${String(statusText).toLowerCase()}`;

        const radiosList = card.querySelector('.radios-list');

        if (device.radios && device.radios.length > 0) {
            device.radios.forEach(radio => {
                const radioControl = createRadioControl(siteId, device.id, radio);
                radiosList.appendChild(radioControl);
            });
        } else {
            radiosList.innerHTML = '<p style="color: var(--gray-500); font-size: 0.9rem;">No radios available</p>';
        }

        return card;
    }

    function createRadioControl(siteId, deviceId, radio) {
        const template = document.getElementById('radio-control-template');
        const control = template.content.cloneNode(true).querySelector('.radio-control');

        control.dataset.radioId = radio.id;
        control.querySelector('.radio-band').textContent = radio.band;
        control.querySelector('.radio-channel').textContent = `CH ${radio.channel}`;
        control.querySelector('.radio-power').textContent = `${radio.transmitPower}dBm`;

        const toggle = control.querySelector('.radio-toggle');
        toggle.checked = radio.enabled;
        toggle.dataset.siteId = siteId;
        toggle.dataset.deviceId = deviceId;
        toggle.dataset.radioId = radio.id;

        const statusEl = control.querySelector('.radio-status');
        updateRadioStatus(statusEl, radio);

        // Add toggle event listener
        toggle.addEventListener('change', async (e) => {
            const enabled = e.target.checked;
            toggle.disabled = true;
            statusEl.textContent = 'Updating...';

            try {
                await API.toggleRadio(siteId, deviceId, radio.id, enabled);
                radio.enabled = enabled;
                radio.status = enabled ? 'Active' : 'Disabled';
                updateRadioStatus(statusEl, radio);
            } catch (error) {
                // Revert toggle on error
                toggle.checked = !enabled;
                statusEl.textContent = 'Error';
                statusEl.className = 'radio-status';
                console.error('Failed to toggle radio:', error);
                alert(`Failed to toggle radio: ${error.message}`);
            } finally {
                toggle.disabled = false;
            }
        });

        return control;
    }

    function updateRadioStatus(element, radio) {
        const statusMap = { 0: 'Active', 1: 'Inactive', 2: 'Disabled', 3: 'Unknown' };
        const statusText = radio.enabled ? (statusMap[radio.status] || radio.status) : 'Disabled';
        element.textContent = statusText;
        element.className = `radio-status ${String(statusText).toLowerCase()}`;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Expose loadSites globally for retry button
    window.loadSites = loadSites;
});
