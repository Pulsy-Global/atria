import {
    Component,
    EventEmitter,
    Input,
    Output,
    ViewEncapsulation,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { STRING_EMPTY } from '../../../shared/core/constants/common.constants';

@Component({
    selector: 'kv-buckets',
    standalone: true,
    templateUrl: './kv-buckets.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule,
        MatProgressSpinnerModule,
    ],
})
export class KvBucketsComponent {
    @Input() buckets: string[] = [];
    @Input() loading = false;
    @Input() loadingMore = false;
    @Input() hasMore = false;
    @Input() filter = STRING_EMPTY;
    @Input() selected = STRING_EMPTY;

    @Output() readonly bucketSelected = new EventEmitter<string>();
    @Output() readonly loadMore = new EventEmitter<void>();

    onScroll(event: Event): void {
        const el = event.target as HTMLElement;

        if (!el || this.loading || this.loadingMore || !this.hasMore) {
            return;
        }

        if (el.scrollTop + el.clientHeight >= el.scrollHeight - 80) {
            this.loadMore.emit();
        }
    }

    openBucket(name: string): void {
        this.bucketSelected.emit(name);
    }

    trackByName(_index: number, name: string): string {
        return name;
    }

    get visibleBuckets(): string[] {
        const term = (this.filter ?? STRING_EMPTY).trim().toLowerCase();

        if (!term) {
            return this.buckets;
        }

        return this.buckets.filter((name) => name.toLowerCase().includes(term));
    }
}
