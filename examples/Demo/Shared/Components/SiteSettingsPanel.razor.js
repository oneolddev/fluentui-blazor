async function openCacheStorage() {
    try {
        return await window.caches.open("FluentUI.Demo")
    }
    catch (err) {
        return undefined;
    }
}

export async function removeAll() {
    let cache = await openCacheStorage();

    if (cache != null) {
        cache.keys().then(function (names) {
            for (let name of names)
                cache.delete(name);
        });
    }
}
