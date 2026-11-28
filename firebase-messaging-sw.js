// Qamishlo Service Worker — Background Push Notifications & Offline Support
self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener('push', (event) => {
  let title = 'توصيل قامشلو 🛵';
  let body = 'لديك تحديث جديد من مطعم قامشلو';
  let url = './';
  try {
    const data = event.data ? event.data.json() : {};
    const notif = data.notification || data.data || data;
    title = notif.title || notif.titleAR || title;
    body = notif.body || notif.bodyAR || notif.message || body;
    url = notif.url || notif.click_action || url;
  } catch (_) {
    if (event.data) {
      try { body = event.data.text() || body; } catch (_) {}
    }
  }
  event.waitUntil(
    self.registration.showNotification(title, {
      body,
      icon: './assets/icon-192.png',
      badge: './assets/icon-192.png',
      vibrate: [200, 100, 200],
      data: { url }
    })
  );
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const targetUrl = (event.notification.data && event.notification.data.url) || './';
  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clients) => {
      for (const client of clients) {
        if ('focus' in client) return client.focus();
      }
      if (self.clients.openWindow) return self.clients.openWindow(targetUrl);
    })
  );
});
