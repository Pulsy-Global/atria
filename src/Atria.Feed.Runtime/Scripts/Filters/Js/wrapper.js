(function() {
    'use strict';

    if (typeof global === 'undefined') {
        globalThis.global = globalThis;
    }
    if (typeof window === 'undefined') {
        globalThis.window = globalThis;
    }
    if (typeof self === 'undefined') {
        globalThis.self = globalThis;
    }

    globalThis.console = Object.freeze({
        log: function() {},
        error: function() {},
        warn: function() {},
        info: function() {},
        debug: function() {},
        trace: function() {}
    });
})();

globalThis.__execute = function(fn, jsonInput) {
    var input = JSON.parse(jsonInput);
    var result = fn(input);
    if (result === null || result === undefined) return null;
    return JSON.stringify(result);
};

globalThis.__executeAsync = function(fn, jsonInput) {
    var input = JSON.parse(jsonInput);
    return Promise.resolve(fn(input)).then(function(result) {
        if (result === null || result === undefined) return null;
        return JSON.stringify(result);
    });
};

globalThis.__cleanupDangerousFunctions = function() {
    'use strict';

    try {
        Object.defineProperty(Function.prototype, 'constructor', {
            get: function() {
                throw new Error('Function constructor is disabled');
            },
            configurable: false
        });
    } catch(e) {}

    try { delete globalThis.WebAssembly; } catch(e) {}
    try { delete globalThis.SharedArrayBuffer; } catch(e) {}
    try { delete globalThis.Atomics; } catch(e) {}

    try {
        globalThis.eval = function() {
            throw new Error('eval is disabled');
        };
        Object.freeze(globalThis.eval);
    } catch(e) {}

    try {
        Object.defineProperty(Object.prototype, 'constructor', {
            get: function() { return function() {}; },
            configurable: false
        });
    } catch(e) {}

    var prototypes = [
        Object.prototype,
        Array.prototype,
        String.prototype,
        Number.prototype,
        Boolean.prototype,
        RegExp.prototype,
        Error.prototype,
        Function.prototype,
        Promise.prototype,
        Map.prototype,
        Set.prototype,
        WeakMap.prototype,
        WeakSet.prototype,
        Symbol.prototype,
        Date.prototype,
        JSON
    ];

    prototypes.forEach(function(proto) {
        try {
            Object.freeze(proto);
        } catch(e) {}
    });

    delete globalThis.__cleanupDangerousFunctions;
};
