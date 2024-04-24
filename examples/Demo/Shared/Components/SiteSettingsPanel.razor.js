export async function removeAll() {
    // get list of all cache instances
    window.caches.keys().then((cacheKeys) => {
        for (const cacheName of cacheKeys) {
            // open each cache and delete all items within it
            window.caches.open(cacheName).then((cache) => {
                cache.keys().then((itemNames) => {
                    for (let name of itemNames) {
                        cache.delete(name);
                    };
                });
            });
        };
    }).catch((error) => {
        console.error('Error fetching cache keys:', error);
    });
}
