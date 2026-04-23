import { Injectable, inject } from '@angular/core';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { ApiService } from '../../api/api.service';
import { CreateFeed, Feed, ProblemDetails, Output, Deploy, UpdateFeed, Networks, Network, PagedList_OutputDto, Tag, CreateTag, TestRequest, TestResult, LatestBlock } from '../../api/api.client';
import { FEED_SERVICE_MIDDLEWARE } from '../../shared/services/feed-service-middleware.token';

@Injectable({
    providedIn: 'root'
})
export class FeedService {

    private readonly _middleware = inject(FEED_SERVICE_MIDDLEWARE, { optional: true });
    
    private _networks: BehaviorSubject<Network[] | null> = new BehaviorSubject<Network[] | null>(null);
    private _outputs: BehaviorSubject<Output[] | null> = new BehaviorSubject<Output[] | null>(null);
    private _feedOutputs: BehaviorSubject<Output[] | null> = new BehaviorSubject<Output[] | null>(null);
    private _deployHistory: BehaviorSubject<Deploy[] | null> = new BehaviorSubject<Deploy[] | null>(null);
    private _feed: BehaviorSubject<Feed | null> = new BehaviorSubject<Feed | null>(null);
    private _tags: BehaviorSubject<Tag[]> = new BehaviorSubject<Tag[]>([]);

    get networks$(): Observable<Network[]> {
        return this._networks.asObservable();
    }

    get outputs$(): Observable<Output[]> {
        return this._outputs.asObservable();
    }

    get feedOutputs$(): Observable<Output[]> {
        return this._feedOutputs.asObservable();
    }

    get deployHistory$(): Observable<Deploy[]> {
        return this._deployHistory.asObservable();
    }

    get feed$(): Observable<Feed> {
        return this._feed.asObservable();
    }

    get tags$(): Observable<Tag[]> {
        return this._tags.asObservable();
    }

    constructor(private apiService: ApiService) {}

    clearState(): void {
        this._networks.next(null);
        this._outputs.next(null);
        this._feedOutputs.next(null);
        this._deployHistory.next(null);
        this._feed.next(null);
        this._tags.next([]);
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

    getOutputs(): Observable<Output[]> {
        return this.apiService.apiClient.getOutputs(undefined, undefined, undefined, undefined, undefined).pipe(
            map((response: PagedList_OutputDto) => response.items || []),
            tap((outputs: Output[]) => {
                this._outputs.next(outputs);
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

    createTag(name: string, color: string): Observable<Tag> {
        const createTagDto = new CreateTag({ 
            name: name, 
            color: color, 
            type: 'Feed' 
        });
        
        return this.apiService.apiClient.createTag(createTagDto).pipe(
            tap((tag: Tag) => {
                const currentTags = this._tags.value;
                this._tags.next([...currentTags, tag]);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    getFeedOutputs(feedId: string): Observable<Output[]> {
        return this.apiService.apiClient.getFeedOutputs(feedId).pipe(
            tap((outputs: Output[]) => {
                this._feedOutputs.next(outputs);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    getDeployHistory(feedId: string): Observable<Deploy[]> {
        return this.apiService.apiClient.getFeedDeploys(feedId).pipe(
            tap((history: Deploy[]) => {
                this._deployHistory.next(history);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    getFeed(feedId: string): Observable<Feed> {
        return this.apiService.apiClient.getFeed(feedId).pipe(
            tap((feed: Feed) => {
                this._feed.next(feed);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    testFeed(testArguments: TestRequest): Observable<TestResult> {
        return this.apiService.apiClient.testFeed(testArguments).pipe(
            catchError((error) => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    createFeed(createFeed: CreateFeed): Observable<Feed> {
        return this.apiService.apiClient.createFeed(createFeed).pipe(
            tap((feed: Feed) => {
                this._feed.next(feed);
            }),
            catchError((error) => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    updateFeed(feedId: string, updateFeed: UpdateFeed): Observable<Feed> {
        return this.apiService.apiClient.updateFeed(feedId, updateFeed).pipe(
            tap((feed: Feed) => {
                this._feed.next(feed);
            }),
            catchError((error) => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    startFeed(feedId: string): Observable<void> {
        return this._wrap(this.apiService.apiClient.startFeed(feedId, undefined).pipe(
            catchError((error) => {
                return throwError(() => new ProblemDetails(error));
            })
        ));
    }

    resetCursorAndStart(feedId: string): Observable<void> {
        return this._wrap(this.apiService.apiClient.startFeed(feedId, true).pipe(
            catchError((error) => {
                return throwError(() => new ProblemDetails(error));
            })
        ));
    }

    private _wrap<T>(source$: Observable<T>): Observable<T> {
        return this._middleware ? this._middleware(source$) : source$;
    }

    getLatestBlock(networkId: string): Observable<LatestBlock> {
        return this.apiService.apiClient.getLatestBlock(networkId).pipe(
            catchError((error) => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }
}
