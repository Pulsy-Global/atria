import { Observable } from 'rxjs';
import {
    FeedMetricsRange,
    FeedMetricsResponse,
} from '../models/feed-metrics.models';

export interface FeedMetricsSource {
    getMetrics(
        resourceId: string,
        range: FeedMetricsRange
    ): Observable<FeedMetricsResponse>;
}
