const CACHE_NAME = "qamishlo-delivery-v14";

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

// Background Web Push handler so Delivery Workers receive alerts outside the browser/app
self.addEventListener("push", event => {
    let data = { title: "🛵 طلب توصيل جديد — قامشلو", body: "لديك طلب توصيل جديد الآن!", url: "/" };
    try {
        if (event.data) {
            const parsed = event.data.json();
            data.title = parsed.notification?.title || parsed.title || data.title;
            data.body = parsed.notification?.body || parsed.body || data.body;
            data.url = parsed.data?.url || parsed.url || "/";
        }
    } catch {
        if (event.data) data.body = event.data.text() || data.body;
    }
    event.waitUntil(
        self.registration.showNotification(data.title, {
            body: data.body,
            icon: "/icons/icon-192.png",
            badge: "/icons/icon-192.png",
            vibrate: [250, 120, 250, 120, 400],
            requireInteraction: true,
            tag: "qamishlo-delivery-alert",
            renotify: true,
            data: { url: data.url }
        })
    );
});

self.addEventListener("notificationclick", event => {
    event.notification.close();
    const targetUrl = event.notification.data?.url || "/";
    event.waitUntil(
        clients.matchAll({ type: "window", includeUncontrolled: true }).then(list => {
            for (const c of list) {
                if ("focus" in c) return c.focus();
            }
            return clients.openWindow(targetUrl);
        })
    );
});
