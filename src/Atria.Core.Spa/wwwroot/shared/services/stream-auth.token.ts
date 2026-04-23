import { InjectionToken } from '@angular/core';

export const STREAM_AUTH_HEADERS =
    new InjectionToken<() => Promise<Record<string, string>>>('STREAM_AUTH_HEADERS');