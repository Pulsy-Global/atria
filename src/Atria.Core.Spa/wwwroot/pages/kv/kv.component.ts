import {
    Component,
    ElementRef,
    OnDestroy,
    OnInit,
    ViewChild,
    ViewEncapsulation,
    inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';
import { KvService } from './kv.service';
import { NotificationService } from '../../shared/services/notification/notification.service';
import { KvBucketsComponent } from './kv-buckets/kv-buckets.component';
import { KvDetailComponent } from './kv-detail/kv-detail.component';
import { KvValuePaneComponent } from './kv-value-pane/kv-value-pane.component';
import { KvSelection } from './kv.util';

@Component({
    selector: 'kv',
    standalone: true,
    templateUrl: './kv.component.html',
    styleUrl: './kv.component.scss',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule,
        MatTooltipModule,
        KvBucketsComponent,
        KvDetailComponent,
        KvValuePaneComponent,
    ],
})
export class KvComponent implements OnInit, OnDestroy {
    private readonly _activatedRoute = inject(ActivatedRoute);
    private readonly _router = inject(Router);
    private readonly _kvService = inject(KvService);
    private readonly _notificationService = inject(NotificationService);
    private readonly _unsubscribeAll: Subject<void> = new Subject<void>();

    @ViewChild('keySearchInput') private readonly _keySearchInput?: ElementRef<HTMLInputElement>;

    selectedBucket = STRING_EMPTY;
    focused: KvSelection | null = null;

    buckets: string[] = [];
    bucketsLoading = true;
    bucketsLoadingMore = false;
    bucketsHasMore = false;
    private _bucketsCursor: string | undefined;
    private readonly _bucketsPageSize = 10;
    private readonly _searchDebounceMs = 500;
    private readonly _searchInput$: Subject<void> = new Subject<void>();

    bucketFilter = STRING_EMPTY;

    searchTerm = STRING_EMPTY;

    ngOnInit(): void {
        this._activatedRoute.queryParamMap
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((params) => {
                const bucket = params.get('bucket') ?? STRING_EMPTY;

                if (bucket === this.selectedBucket) {
                    return;
                }

                this.selectedBucket = bucket;
                this.focused = null;
                this._resetSearch();
            });

        this._searchInput$
            .pipe(
                debounceTime(this._searchDebounceMs),
                takeUntil(this._unsubscribeAll)
            )
            .subscribe(() => {
                this.searchTerm = this._readSearchInput();
            });

        this._loadBuckets(false);
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }

    get isEmptyState(): boolean {
        return !this.selectedBucket && !this.focused;
    }

    onKeySearchInput(): void {
        this._searchInput$.next();
    }

    onKeySearchSubmit(event: Event): void {
        event.preventDefault();
        this.searchTerm = this._readSearchInput();
    }

    private _readSearchInput(): string {
        return this._keySearchInput?.nativeElement?.value?.trim() ?? STRING_EMPTY;
    }

    onBucketFilterChanged(event: Event): void {
        this.bucketFilter = (event.target as HTMLInputElement | null)?.value ?? STRING_EMPTY;
    }

    selectBucket(name: string): void {
        this.focused = null;
        this._resetSearch();

        this._router.navigate([], {
            relativeTo: this._activatedRoute,
            queryParams: { bucket: name },
        });
    }

    onValueSelected(selection: KvSelection): void {
        this.focused = selection;
    }

    clearFocus(): void {
        this.focused = null;
    }

    onBack(): void {
        this.focused = null;
        this._resetSearch();

        this._router.navigate([], {
            relativeTo: this._activatedRoute,
            queryParams: {},
        });
    }

    onBucketsLoadMore(): void {
        if (this.bucketsLoading || this.bucketsLoadingMore || !this.bucketsHasMore) {
            return;
        }

        this._loadBuckets(true);
    }

    private _resetSearch(): void {
        this.searchTerm = STRING_EMPTY;

        const input = this._keySearchInput?.nativeElement;
        if (input) {
            input.value = STRING_EMPTY;
        }
    }

    private _loadBuckets(append: boolean): void {
        if (append) {
            this.bucketsLoadingMore = true;
        } else {
            this.bucketsLoading = true;
        }

        this._kvService
            .listBuckets(
                this._bucketsPageSize,
                append ? this._bucketsCursor : undefined
            )
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe({
                next: (list) => {
                    const page = (list.buckets ?? [])
                        .map((name: string) => name ?? STRING_EMPTY)
                        .sort((a: string, b: string) => a.localeCompare(b));

                    this.buckets = append ? [...this.buckets, ...page] : page;

                    this.bucketsHasMore = !!list.hasMore;
                    this._bucketsCursor = list.cursor ?? undefined;

                    this.bucketsLoading = false;
                    this.bucketsLoadingMore = false;
                },
                error: (error) => {
                    this.bucketsLoading = false;
                    this.bucketsLoadingMore = false;
                    this._notificationService.showErrorAlert(
                        'Error Loading Buckets',
                        error?.title || 'Failed to load buckets'
                    );
                },
            });
    }
}
