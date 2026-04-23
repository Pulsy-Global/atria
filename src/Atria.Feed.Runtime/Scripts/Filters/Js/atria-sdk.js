'use strict';

function decodeEVMLogs(data, abis) {
    var ethers = require('ethers');
    var iface = new ethers.Interface(abis.flat());

    var isReceipts = Array.isArray(data) && data.length > 0 && data[0].logs;
    var logs = isReceipts ? data.flatMap(function(r) { return r.logs || []; }) : data;
    var results = [];

    for (var i = 0; i < logs.length; i++) {
        var decoded = tryDecode(logs[i], iface);
        if (decoded) {
            results.push(Object.assign({}, logs[i], { decodedLog: decoded }));
        }
    }

    return results;
}

function tryDecode(log, iface) {
    try {
        var parsed = iface.parseLog({
            topics: log.topics,
            data: log.data
        });

        if (parsed) {
            return {
                name: parsed.name,
                signature: parsed.signature,
                args: convertArgs(parsed.args, parsed.fragment.inputs)
            };
        }
    } catch (e) {
        // Ignore parsing errors (mismatched ABI)
    }
    return null;
}

function convertValue(val) {
    if (typeof val === 'bigint') return val.toString();
    if (Array.isArray(val)) return val.map(convertValue);
    if (val && typeof val === 'object') {
        var result = {};
        Object.keys(val).forEach(function(k) { result[k] = convertValue(val[k]); });
        return result;
    }
    return val;
}

function convertArgs(args, inputs) {
    var obj = {};
    if (inputs && args) {
        inputs.forEach(function(input, index) {
            if (input.name) {
                obj[input.name] = convertValue(args[index]);
            }
        });
    }
    return obj;
}

var evm = {
    decodeEVMLogs: decodeEVMLogs
};

module.exports = {
    evm: evm
};
