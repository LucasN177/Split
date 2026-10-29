window.split = {
    prefersDarkMode: () => window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches,

    downloadFile: (fileName, content, contentType) => {
        const blob = new Blob([content], { type: contentType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    },

    // Entfernt alle localStorage-Einträge mit dem angegebenen Präfix
    clearStorage: (prefix) => {
        Object.keys(localStorage)
            .filter(key => key.startsWith(prefix))
            .forEach(key => localStorage.removeItem(key));
    },

    copyToClipboard: async (text) => {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    },

    // Natives Teilen-Menü (Mobilgeräte); false, wenn nicht verfügbar oder abgebrochen
    share: async (title, text, url) => {
        if (!navigator.share) return false;
        try {
            await navigator.share({ title, text, url });
            return true;
        } catch {
            return false;
        }
    },

    connectivity: {
        register: (dotnetRef) => {
            window.addEventListener('online', () => dotnetRef.invokeMethodAsync('OnStatusChanged', true));
            window.addEventListener('offline', () => dotnetRef.invokeMethodAsync('OnStatusChanged', false));
            return navigator.onLine;
        }
    },

    push: {
        isSupported: () => 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window,

        getSubscription: async () => {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();
            return subscription ? split.push.toInfo(subscription) : null;
        },

        subscribe: async (vapidPublicKey) => {
            const permission = await Notification.requestPermission();
            if (permission !== 'granted') return null;

            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: split.push.base64ToUint8Array(vapidPublicKey)
            });
            return split.push.toInfo(subscription);
        },

        unsubscribe: async () => {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();
            if (!subscription) return null;
            const endpoint = subscription.endpoint;
            await subscription.unsubscribe();
            return endpoint;
        },

        toInfo: (subscription) => {
            const json = subscription.toJSON();
            return { endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth };
        },

        base64ToUint8Array: (base64) => {
            const padding = '='.repeat((4 - base64.length % 4) % 4);
            const raw = atob((base64 + padding).replace(/-/g, '+').replace(/_/g, '/'));
            return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
        }
    }
};

// Hinweis anzeigen, sobald eine neue App-Version installiert ist
(() => {
    if (!('serviceWorker' in navigator)) return;

    const showUpdateBanner = (worker) => {
        if (document.getElementById('split-update')) return;
        const banner = document.createElement('div');
        banner.id = 'split-update';
        banner.innerHTML = '<span>Eine neue Version von Split ist verfügbar.</span><button type="button">Aktualisieren</button>';
        banner.querySelector('button').addEventListener('click', () => worker.postMessage({ type: 'SKIP_WAITING' }));
        document.body.appendChild(banner);
    };

    let reloading = false;
    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (reloading) return;
        reloading = true;
        window.location.reload();
    });

    navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).then(registration => {
        if (registration.waiting && navigator.serviceWorker.controller) {
            showUpdateBanner(registration.waiting);
        }
        registration.addEventListener('updatefound', () => {
            const worker = registration.installing;
            worker?.addEventListener('statechange', () => {
                if (worker.state === 'installed' && navigator.serviceWorker.controller) {
                    showUpdateBanner(worker);
                }
            });
        });

        // Regelmäßig nach Updates suchen, solange die App geöffnet ist
        setInterval(() => registration.update().catch(() => { }), 60 * 60 * 1000);
    }).catch(() => { });
})();
