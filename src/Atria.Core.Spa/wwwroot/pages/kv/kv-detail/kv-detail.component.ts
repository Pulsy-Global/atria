import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnDestroy,
    Output,
    SimpleChanges,
    ViewEncapsulation,
    inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil } from 'rxjs';
import { KvService } from '../kv.service';
import { NotificationService } from '../../../shared/services/notification/notification.service';
import { STRING_EMPTY } from '../../../shared/core/constants/common.constants';
import { KvSelection, kvByteLength, kvFormatSize, kvIsJson, kvPreview } from '../kv.util';

interface KvRow {
    key: string;
    value: string;
    isJson: boolean;
    preview: string;
    size: string;
}

@Component({
    selector: 'kv-detail',
    standalone: true,
    templateUrl: './kv-detail.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatTooltipModule,
    ],
})
export class KvDetailComponent implements OnChanges, OnDestroy {
    private readonly _kvService = inject(KvService);
    private readonly _notificationService = inject(NotificationService);
    private readonly _unsubscribeAll: Subject<void> = new Subject<void>();

    @Input() bucket = STRING_EMPTY;
    @Input() search = STRING_EMPTY;
    @Output() readonly valueSelected = new EventEmitter<KvSelection>();

    rows: KvRow[] = [];
    selectedKey = STRING_EMPTY;

    isLoading = false;
    isLoadingMore = false;
    hasMore = false;

    private readonly _pageSize = 10;
    private _nextCursor: string | undefined;

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['bucket'] && this.bucket) {
            this._reset();
            this._load(false);
            return;
        }

        if (changes['search'] && !changes['search'].firstChange) {
            this._reset();
            this._load(false);
        }
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }

    onScroll(event: Event): void {
        const el = event.target as HTMLElement;

        if (!el || this.isLoading || this.isLoadingMore || !this.hasMore) {
            return;
        }

        if (el.scrollTop + el.clientHeight >= el.scrollHeight - 80) {
            this._load(true);
        }
    }

    selectRow(row: KvRow): void {
        this.selectedKey = row.key;
        this.valueSelected.emit({ key: row.key, value: row.value, isJson: row.isJson });
    }

    trackByKey(_index: number, row: KvRow): string {
        return row.key;
    }

    private _reset(): void {
        this.rows = [];
        this.selectedKey = STRING_EMPTY;
        this.hasMore = false;
        this._nextCursor = undefined;
    }

    private _load(append: boolean): void {
        if (!append && this.search) {
            this._loadExact();
            return;
        }

        if (append) {
            this.isLoadingMore = true;
        } else {
            this.isLoading = true;
        }

        const cursor = append ? this._nextCursor : undefined;

        this._kvService
            .getBucketValues(this.bucket, this._pageSize, cursor)
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe({
                next: (values) => {
                    const page = (values.items ?? []).map((item) =>
                        this._toRow(item.key ?? STRING_EMPTY, item.value ?? STRING_EMPTY)
                    );

                    this.rows = append ? [...this.rows, ...page] : page;
                    this.hasMore = !!values.hasMore;
                    this._nextCursor = values.cursor ?? undefined;

                    this.isLoading = false;
                    this.isLoadingMore = false;
                },
                error: (error) => {
                    this.isLoading = false;
                    this.isLoadingMore = false;
                    this._notificationService.showErrorAlert(
                        'Error Loading Values',
                        error?.title || `Failed to load values for "${this.bucket}"`
                    );
                },
            });
    }

    private _loadExact(): void {
        this.isLoading = true;

        const key = this.search;

        this._kvService
            .getBucketValue(this.bucket, key)
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe({
                next: (value: string | undefined) => {
                    this.rows = value === undefined ? [] : [this._toRow(key, value)];
                    this.hasMore = false;
                    this._nextCursor = undefined;
                    this.isLoading = false;
                },
                error: (error) => {
                    this.isLoading = false;
                    this._notificationService.showErrorAlert(
                        'Error Loading Value',
                        error?.title || `Failed to load key "${key}" from "${this.bucket}"`
                    );
                },
            });
    }

    private _toRow(key: string, value: string): KvRow {
        const isJson = kvIsJson(value);

        return {
            key,
            value,
            isJson,
            preview: kvPreview(value),
            size: kvFormatSize(kvByteLength(value)),
        };
    }
}
