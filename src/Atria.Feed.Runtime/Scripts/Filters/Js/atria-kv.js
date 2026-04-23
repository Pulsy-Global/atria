'use strict';

function getHost() {
    if (!globalThis.__kvHost) {
        throw new Error('KV storage is not available for this feed. Contact your administrator.');
    }
    return globalThis.__kvHost;
}

module.exports = {
    bucket: function(name) {
        var host = getHost();
        return {
            add: async function(key, value) {
                await host.BucketAddAsync(name, key, value !== undefined ? JSON.stringify(value) : null);
            },
            addMany: async function(items) {
                await host.BucketAddBatchAsync(name, JSON.stringify(items));
            },
            get: async function(key) {
                var result = await host.BucketGetAsync(name, key);
                if (result === null || result === undefined) return null;
                try { return JSON.parse(result); } catch (e) { return result; }
            },
            getMany: async function(keys) {
                return JSON.parse(await host.BucketGetBatchAsync(name, JSON.stringify(keys)));
            },
            remove: async function(key) {
                await host.BucketRemoveAsync(name, key);
            },
            removeMany: async function(keys) {
                await host.BucketRemoveBatchAsync(name, JSON.stringify(keys));
            },
            list: async function(opts) {
                var limit = (opts && opts.limit) ? opts.limit : 100;
                var cursor = (opts && opts.cursor) ? opts.cursor : '';
                return JSON.parse(await host.BucketValuesAsync(name, limit, cursor));
            },
        };
    }
};
