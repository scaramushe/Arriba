// Authentication handler
const Auth = {
    isAuthenticated() {
        const token = API.getToken();
        if (!token) return false;

        // Check if token is expired
        const expiresAt = localStorage.getItem('arriba_token_expires') ||
                         sessionStorage.getItem('arriba_token_expires');

        if (expiresAt) {
            const expiryDate = new Date(expiresAt);
            const now = new Date();
            // Consider expired if less than 5 minutes remaining
            if (expiryDate.getTime() - now.getTime() < 5 * 60 * 1000) {
                // Token is expiring soon, but don't wait for refresh here
                // Let the auto-refresh or API request handle it
                return true;
            }
        }

        return true;
    },

    async checkAndRefresh() {
        if (!API.getToken()) return false;

        const expiresAt = localStorage.getItem('arriba_token_expires') ||
                         sessionStorage.getItem('arriba_token_expires');

        if (expiresAt) {
            const expiryDate = new Date(expiresAt);
            const now = new Date();
            // Refresh if less than 10 minutes remaining
            if (expiryDate.getTime() - now.getTime() < 10 * 60 * 1000) {
                return await API.refreshToken();
            }
        }

        return true;
    },

    // Setup auto-refresh of tokens
    startAutoRefresh() {
        // Check every 5 minutes
        setInterval(async () => {
            if (API.getToken()) {
                await this.checkAndRefresh();
            }
        }, 5 * 60 * 1000);
    }
};
