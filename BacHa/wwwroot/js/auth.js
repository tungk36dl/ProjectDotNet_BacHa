// Authentication and Token Management
(function() {
    'use strict';

    // Configuration
    const TOKEN_REFRESH_INTERVAL = 50 * 60 * 1000; // 50 minutes
    const TOKEN_CHECK_INTERVAL = 5 * 60 * 1000; // 5 minutes
    let refreshTimer = null;
    let checkTimer = null;

    // Initialize authentication
    function initAuth() {
        if (isAuthenticated()) {
            startTokenRefresh();
            startTokenCheck();
        }
    }

    // Check if user is authenticated
    function isAuthenticated() {
        return localStorage.getItem('accessToken') !== null || document.cookie.includes('X-Access-Token=');
    }

    // Start automatic token refresh
    function startTokenRefresh() {
        if (refreshTimer) {
            clearInterval(refreshTimer);
        }

        refreshTimer = setInterval(async () => {
            try {
                await refreshToken();
            } catch (error) {
                console.error('Token refresh failed:', error);
                handleAuthFailure();
            }
        }, TOKEN_REFRESH_INTERVAL);
    }

    // Start token validation check
    function startTokenCheck() {
        if (checkTimer) {
            clearInterval(checkTimer);
        }

        checkTimer = setInterval(async () => {
            try {
                const isValid = await validateToken();
                if (!isValid) {
                    handleAuthFailure();
                }
            } catch (error) {
                console.error('Token validation failed:', error);
                handleAuthFailure();
            }
        }, TOKEN_CHECK_INTERVAL);
    }

    // Refresh token
    async function refreshToken() {
        try {
            const response = await fetch('/Auth/RefreshToken', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'include'
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    // Store new tokens in localStorage
                    if (result.token) {
                        localStorage.setItem('accessToken', result.token);
                    }
                    if (result.refreshToken) {
                        localStorage.setItem('refreshToken', result.refreshToken);
                    }
                    console.log('Token refreshed successfully');
                    return true;
                } else {
                    console.warn('Token refresh failed:', result.message);
                    return false;
                }
            } else {
                console.warn('Token refresh request failed:', response.status);
                return false;
            }
        } catch (error) {
            console.error('Token refresh error:', error);
            return false;
        }
    }

    // Validate current token
    async function validateToken() {
        try {
            const response = await fetch('/Auth/ValidateToken', {
                method: 'GET',
                credentials: 'include'
            });
            return response.ok;
        } catch (error) {
            console.error('Token validation error:', error);
            return false;
        }
    }

    // Handle authentication failure
    function handleAuthFailure() {
        console.log('Authentication failed, redirecting to login...');
        
        // Clear timers
        if (refreshTimer) {
            clearInterval(refreshTimer);
            refreshTimer = null;
        }
        if (checkTimer) {
            clearInterval(checkTimer);
            checkTimer = null;
        }

        // Clear localStorage
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        
        // Clear cookies
        document.cookie = 'X-Access-Token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
        document.cookie = 'X-Refresh-Token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';

        // Redirect to login
        window.location.href = '/Auth/Login';
    }

    // Setup AJAX request interceptor
    function setupAjaxInterceptor() {
        // Override fetch to handle 401 responses and add Authorization header
        const originalFetch = window.fetch;
        window.fetch = async function(...args) {
            // Add Authorization header if token exists
            const token = localStorage.getItem('accessToken');
            if (token && args[1]) {
                args[1].headers = {
                    ...args[1].headers,
                    'Authorization': `Bearer ${token}`
                };
            } else if (token) {
                args[1] = {
                    ...args[1],
                    headers: {
                        ...args[1]?.headers,
                        'Authorization': `Bearer ${token}`
                    }
                };
            }
            
            const response = await originalFetch(...args);
            
            if (response.status === 401) {
                console.log('Received 401, attempting token refresh...');
                const refreshSuccess = await refreshToken();
                
                if (refreshSuccess) {
                    // Retry the original request with new token
                    const newToken = localStorage.getItem('accessToken');
                    if (newToken && args[1]) {
                        args[1].headers = {
                            ...args[1].headers,
                            'Authorization': `Bearer ${newToken}`
                        };
                    } else if (newToken) {
                        args[1] = {
                            ...args[1],
                            headers: {
                                ...args[1]?.headers,
                                'Authorization': `Bearer ${newToken}`
                            }
                        };
                    }
                    return originalFetch(...args);
                } else {
                    handleAuthFailure();
                    return response;
                }
            }
            
            return response;
        };
    }

    // Public API
    window.AuthManager = {
        init: initAuth,
        refreshToken: refreshToken,
        isAuthenticated: isAuthenticated,
        logout: function() {
            handleAuthFailure();
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            initAuth();
            setupAjaxInterceptor();
        });
    } else {
        initAuth();
        setupAjaxInterceptor();
    }

})();
