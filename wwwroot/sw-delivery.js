self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', e => e.waitUntil(self.clients.claim()));

self.addEventListener('push', event => {
  let payload = { title: 'طلب توصيل جديد — قامشلو 🛵', body: 'وصلك طلب توصيل جديد، افتح الصفحة الآن!', url: '/' };
  try {
    if (event.data) {
      const parsed = event.data.json();
      payload.title = parsed.notification?.title || parsed.title || payload.title;
      payload.body = parsed.notification?.body || parsed.body || payload.body;
      payload.url = parsed.data?.url || parsed.url || '/';
    }
  } catch {
    if (event.data) payload.body = event.data.text();
  }
  event.waitUntil(
    self.registration.showNotification(payload.title, {
      body: payload.body,
      vibrate: [250, 120, 250, 120, 300],
      requireInteraction: true,
      data: { url: payload.url }
    })
  );
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(list => {
      for (const c of list) { if ('focus' in c) return c.focus(); }
      if (clients.openWindow) return clients.openWindow(event.notification.data?.url || '/');
    })
  );
});
