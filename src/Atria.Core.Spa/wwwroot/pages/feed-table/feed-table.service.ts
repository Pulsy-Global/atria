import { Injectable, OnDestroy, inject } from '@angular/core';
import { Observable, throwError, BehaviorSubject, Subscription } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';
import { ApiService } from '../../api/api.service';
import { Feed, PagedList_FeedDto, Networks, ProblemDetails, Network, FeedStatus, Tag } from '../../api/api.client';
import { StreamService } from '../../shared/services/stream/stream.service';
import { FeedStatusStreamItem } from '../../shared/services/stream/stream.types';
import { FEED_SERVICE_MIDDLEWARE } from '../../shared/services/feed-service-middleware.token';

@Injectable({
    providedIn: 'root'
})
export class FeedTableService implements OnDestroy {

    private readonly _middleware = inject(FEED_SERVICE_MIDDLEWARE, { optional: true });
    
    private _feeds = new BehaviorSubject<Feed[]>([]);
    private _totalCount = new BehaviorSubject<number>(0);
    private _networks = new BehaviorSubject<Network[] | null>(null);
    private _tags = new BehaviorSubject<Tag[]>([]);
    private _feedStatuses = new BehaviorSubject<{ [key: string]: { feedCursor: number, chainHead: number } }>({});
    private _streamSubscriptions: Subscription[] = [];

    get feeds$(): Observable<Feed[]> {
        return this._feeds.asObservable();
    }

    get totalCount$(): Observable<number> {
        return this._totalCount.asObservable();
    }

    get networks$(): Observable<Network[] | null> {
        return this._networks.asObservable();
    }

    get tags$(): Observable<Tag[]> {
        return this._tags.asObservable();
    }

    get feedStatuses$(): Observable<{ [key: string]: { feedCursor: number, chainHead: number } }> {
        return this._feedStatuses.asObservable();
    }

    constructor(
        private readonly apiService: ApiService,
        private readonly streamService: StreamService,
    ) {}

    ngOnDestroy(): void {
        this.disconnectFromFeedStatusStream();
    }

    clearState(): void {
        this._feeds.next([]);
        this._totalCount.next(0);
        this._networks.next(null);
        this._tags.next([]);
        this.disconnectFromFeedStatusStream();
    }

    getNetworks(): Observable<Network[]> {
        return this.apiService.apiClient.getNetworks().pipe(
            map((response: Networks) => response.networks || []),
            tap((networks: Network[]) => {
                this._networks.next(networks);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    getTags(): Observable<Tag[]> {
        return this.apiService.apiClient.getFeedTags().pipe(
            tap((tags: Tag[]) => {
                this._tags.next(tags);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    getFeeds(
        search?: string,
        orderby?: string,
        filter?: string,
        skip?: number,
        top?: number
    ): Observable<PagedList_FeedDto> {
        return this.apiService.apiClient.getFeeds(
            orderby,
            filter,
            skip,
            top,
            search
        ).pipe(
            tap((pagedResult: PagedList_FeedDto) => {
                this._feeds.next(pagedResult.items || []);
                this._totalCount.next(pagedResult.totalCount || 0);
                this.connectToFeedStatusStream();
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    pauseFeed(feed: Feed): Observable<void> {
        return this._wrap(this.apiService.apiClient.pauseFeed(feed.id).pipe(
            tap(() => {
                feed.status = FeedStatus.Paused;
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        ));
    }

    startFeed(feed: Feed): Observable<void> {
        return this._wrap(this.apiService.apiClient.startFeed(feed.id, undefined).pipe(
            tap(() => {
                feed.status = FeedStatus.Running;
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        ));
    }

    resetCursorAndStart(feed: Feed): Observable<void> {
        return this._wrap(this.apiService.apiClient.startFeed(feed.id, true).pipe(
            tap(() => {
                feed.status = FeedStatus.Running;
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        ));
    }

    deleteFeed(feedId: string): Observable<void> {
        return this._wrap(this.apiService.apiClient.deleteFeed(feedId).pipe(
            tap(() => {
                const currentFeeds = this._feeds.value;
                const currentTotal = this._totalCount.value;

                const updatedFeeds = currentFeeds.filter(feed => feed.id !== feedId);

                this._feeds.next(updatedFeeds);
                this._totalCount.next(Math.max(0, currentTotal - 1));
                this.connectToFeedStatusStream();
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        ));
    }

    private _wrap<T>(source$: Observable<T>): Observable<T> {
        return this._middleware ? this._middleware(source$) : source$;
    }

    private connectToFeedStatusStream(): void {
        this.disconnectFromFeedStatusStream();
    
        const feedsByNetwork = this._feeds.value.reduce((acc, feed) => {
            if (feed.networkId) {
                if (!acc[feed.networkId]) {
                    acc[feed.networkId] = [];
                }
                acc[feed.networkId].push(feed.id);
            }
            return acc;
        }, {});
    
        for (const networkId in feedsByNetwork) {
            if (feedsByNetwork.hasOwnProperty(networkId)) {
                const feedIds = feedsByNetwork[networkId];
                const feedIdsQuery = feedIds.map(id => `feedIds=${id}`).join('&');
                const url = `${this.apiService.apiServer}/feeds/statuses?chainId=${networkId}&${feedIdsQuery}`;
                
                const stream$ = this.streamService.connect<FeedStatusStreamItem[]>(url);
                const subscription = stream$.subscribe(items => {
                    const statuses = {...this._feedStatuses.value};
                    items.forEach(item => {
                        statuses[item.feedId] = {
                            feedCursor: item.feedCursor,
                            chainHead: item.chainHead
                        };
                    });
                    this._feedStatuses.next(statuses);
                });

                this._streamSubscriptions.push(subscription);
            }
        }
    }

    private disconnectFromFeedStatusStream(): void {
        this._streamSubscriptions.forEach(sub => sub.unsubscribe());
        this._streamSubscriptions = [];
    }
}