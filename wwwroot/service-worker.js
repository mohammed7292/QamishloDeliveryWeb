const CACHE_NAME = "qamishlo-delivery-bilingual-v3";

const APP_SHELL = [
    "/manifest.json"
];

// Install service worker
self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(APP_SHELL))
            .then(() => self.skipWaiting())
    );
});

// Activate service worker
self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys
                    .filter(key => key !== CACHE_NAME)
                    .map(key => caches.delete(key))
            )
        ).then(() => self.clients.claim())
    );
});

// Network only for pages and API requests.
// Do not cache login pages, passwords, orders, or API responses.
self.addEventListener("fetch", event => {
    const request = event.request;
    const url = new URL(request.url);

    if (
        request.method !== "GET" ||
        url.origin !== self.location.origin ||
        url.pathname.startsWith("/api/")
    ) {
        return;
    }

    // Always get HTML pages from the server.
    // This avoids displaying stale or session-dependent pages.
    if (
        request.mode === "navigate" ||
        url.pathname === "/Login" ||
        url.pathname === "/"
    ) {
        event.respondWith(fetch(request));
        return;
    }

    // Static resources: try network first, then cache.
    event.respondWith(
        fetch(request)
            .then(response => {
                if (response.ok) {
                    const copy = response.clone();

                    caches.open(CACHE_NAME)
                        .then(cache => cache.put(request, copy));
                }

                return response;
            })
            .catch(async () => {
                const cached = await caches.match(request);

                if (cached) {
                    return cached;
                }

                return new Response(
                    "You are offline. Please reconnect to the internet.",
                    {
                        status: 503,
                        headers: {
                            "Content-Type": "text/plain; charset=utf-8"
                        }
                    }
                );
            })
    );
});