import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfigService } from '../../../shared/core/config/app.config.service';
import {
    FeedMetricsRange,
    FeedMetricsResponse,
} from '../models/feed-metrics.models';
import { FeedMetricsSource } from './feed-metrics-source';

@Injectable({ providedIn: 'root' })
export class FeedMetricsService implements FeedMetricsSource {
    private readonly _http = inject(HttpClient);
    private readonly _appConfig = inject(AppConfigService);

    getMetrics(
        feedId: string,
        range: FeedMetricsRange
    ): Observable<FeedMetricsResponse> {
        const params = new HttpParams().set('range', range);

        return this._http.get<FeedMetricsResponse>(
            `${this._appConfig.apiServer}/feeds/${encodeURIComponent(feedId)}/metrics`,
            { params }
        );
    }
}
