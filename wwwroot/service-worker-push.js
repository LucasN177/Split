// Gemeinsame Service-Worker-Logik (Entwicklung und Veröffentlichung):
// Push-Benachrichtigungen und Aktivierung neuer Versionen auf Wunsch.

// Payload der Edge Function "split-notify": { title, body, url, tag }
self.addEventListener('push', event => {
    let data = {};
    try {
        data = event.data ? event.data.json() : {};
    } catch {
        data = { body: event.data ? event.data.text() : '' };
    }

    event.waitUntil(self.registration.showNotification(data.title || 'Split', {
        body: data.body || '',
        icon: 'icon-192.png',
        badge: 'icon-192.png',
        tag: data.tag,
        data: { url: data.url || './' }
    }));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const target = new URL(event.notification.data?.url || './', self.registration.scope).href;

    event.waitUntil((async () => {
        const windows = await clients.matchAll({ type: 'window', includeUncontrolled: true });
        const existing = windows.find(w => w.url.startsWith(self.registration.scope));
        if (existing) {
            await existing.focus();
            return existing.navigate(target);
        }
        return clients.openWindow(target);
    })());
});

// Vom Update-Hinweis in der App ausgelöst
self.addEventListener('message', event => {
    if (event.data?.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
