export function getErrorCode(error: any): string | null {
    if (typeof error?.errorCode === 'string') {
        return error.errorCode;
    }

    if (typeof error?.response !== 'string') {
        return null;
    }

    try {
        const body = JSON.parse(error.response);
        return typeof body?.errorCode === 'string' ? body.errorCode : null;
    } catch {
        return null;
    }
}
