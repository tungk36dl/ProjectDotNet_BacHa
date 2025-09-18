// Session-based Authentication Management
(function() {
    'use strict';

    // Logout function
    async function logout() {
        try {
            // Call logout API to clear session
            await fetch('/api/AuthApi/logout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'include'
            });
        } catch (error) {
            console.error('Logout API call failed:', error);
        } finally {
            // Always redirect to login
            window.location.href = '/Auth/Login';
        }
    }

    // Setup request interceptor for 401 handling
    function setupRequestInterceptor() {
        // Override fetch to handle 401 responses
        const originalFetch = window.fetch;
        window.fetch = async function(...args) {
            // Ensure credentials are included for all requests
            if (args[1]) {
                args[1].credentials = 'include';
            } else {
                args[1] = { credentials: 'include' };
            }
            
            const response = await originalFetch(...args);
            
            if (response.status === 401) {
                console.log('Received 401, session may have expired...');
                window.location.href = '/Auth/Login';
                return response;
            }
            
            return response;
        };
    }

    // Public API
    window.AuthManager = {
        logout: logout
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupRequestInterceptor);
    } else {
        setupRequestInterceptor();
    }

})();
