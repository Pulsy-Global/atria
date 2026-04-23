import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

export type FeedServiceMiddleware = <T>(source$: Observable<T>) => Observable<T>;

export const FEED_SERVICE_MIDDLEWARE = new InjectionToken<FeedServiceMiddleware>(
    'FEED_SERVICE_MIDDLEWARE'
);
