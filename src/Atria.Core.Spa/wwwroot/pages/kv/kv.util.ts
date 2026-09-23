// Shared, dependency-free helpers for the read-only KV view. Kept as plain
// functions so both the keys list and the value pane can reuse them.

export type KvKind = 'json' | 'text';

// A focused key and its (already loaded) value, passed up from the keys list to
// the shell so the inline value pane can render it without another request.
export interface KvSelection {
    key: string;
    value: string;
    isJson: boolean;
}

const encoder = new TextEncoder();

export function kvIsJson(value: string): boolean {
    if (!value) {
        return false;
    }

    try {
        const parsed = JSON.parse(value);
        return parsed !== null && typeof parsed === 'object';
    } catch {
        return false;
    }
}

export function kvKind(value: string): KvKind {
    return kvIsJson(value) ? 'json' : 'text';
}

export function kvPretty(value: string): string {
    if (!kvIsJson(value)) {
        return value;
    }

    try {
        return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
        return value;
    }
}

export function kvByteLength(value: string): number {
    return encoder.encode(value ?? '').length;
}

// Classic hex dump of the value's UTF-8 bytes: one line per 16 bytes as
// "offset  hex  hex  ascii". Bytes are uppercased; the two 8-byte halves are
// separated by a wider gap, and non-printable ASCII renders as '.'. Kept as a
// plain string so the read-only Monaco editor (and Copy/Download) can reuse it.
export function kvHexDump(value: string): string {
    const bytes = encoder.encode(value ?? '');

    if (bytes.length === 0) {
        return '';
    }

    const lines: string[] = [];

    for (let offset = 0; offset < bytes.length; offset += 16) {
        const slice = bytes.subarray(offset, Math.min(offset + 16, bytes.length));

        let hex = '';
        let ascii = '';

        for (let i = 0; i < 16; i++) {
            const byte = i < slice.length ? slice[i] : null;
            const hexPart = byte === null ? '  ' : byte.toString(16).toUpperCase().padStart(2, '0');

            if (i === 8) {
                hex += ' ';
            }

            hex += (i === 0 ? '' : ' ') + hexPart;
            ascii += byte === null ? ' ' : byte >= 0x20 && byte <= 0x7e ? String.fromCharCode(byte) : '.';
        }

        lines.push(`${offset.toString(16).toUpperCase().padStart(8, '0')}  ${hex}  ${ascii}`);
    }

    return lines.join('\n');
}

export function kvFormatSize(bytes: number): string {
    if (bytes < 1024) {
        return `${bytes} B`;
    }

    const kb = bytes / 1024;
    if (kb < 1024) {
        return `${kb.toFixed(1)} KB`;
    }

    return `${(kb / 1024).toFixed(1)} MB`;
}

export function kvPreview(value: string, max = 120): string {
    const single = kvPretty(value).replace(/\s+/g, ' ').trim();
    return single.length > max ? single.slice(0, max) + '…' : single;
}
