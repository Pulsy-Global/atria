import {
    Component,
    Input,
    OnInit,
    OnDestroy,
    ViewEncapsulation,
    ViewChild,
    ElementRef,
    ChangeDetectorRef,
    Inject,
    Optional,
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { firstValueFrom } from 'rxjs';

import { Result } from '../../../../api/api.client';
import { ApiService } from '../../../../api/api.service';
import { CultureAgnosticDatePipe } from '../../../../shared/pipes/date.pipe';
import { STREAM_AUTH_HEADERS } from '../../../../shared/services/stream-auth.token';

@Component({
    selector: 'result-tab',
    standalone: true,
    templateUrl: './result-tab.component.html',
    encapsulation: ViewEncapsulation.None,
    host: { class: 'flex flex-col overflow-hidden' },
    imports: [
        CommonModule,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule,
        MatProgressSpinnerModule,
        CultureAgnosticDatePipe,
    ],
})
export class ResultTabComponent implements OnInit, OnDestroy {
    @Input() feedId: string | null = null;

    @ViewChild('resultsContainer')
    resultsContainer?: ElementRef<HTMLDivElement>;

    resultHistory: Result[] = [];
    selectedResult: Result | null = null;
    showDetailView = false;

    isStreamConnected = false;
    isPaused = false;
    isLoading = false;
    showScrollTop = false;

    readonly RESULT_LIMIT = 1;
    readonly MAX_HISTORY = 1000;
    readonly RECONNECT_DELAY_MS = 2000;

    private upCursor = -1;
    private abortController: AbortController | null = null;
    private isDestroyed = false;

    constructor(
        private apiService: ApiService,
        private cdr: ChangeDetectorRef,
        @Optional()
        @Inject(STREAM_AUTH_HEADERS)
        private getStreamHeaders: (() => Promise<Record<string, string>>) | null,
    ) {}

    async ngOnInit(): Promise<void> {
        if (!this.feedId) return;
        await this.fetchResults();
        this.connectStream(this.upCursor > 0 ? this.upCursor : undefined);
    }

    ngOnDestroy(): void {
        this.isDestroyed = true;
        this.abortController?.abort();
    }

    onScroll(): void {
        const element = this.resultsContainer?.nativeElement;
        if (!element) return;

        this.showScrollTop = element.scrollTop > 100;
    }

    scrollToTop(): void {
        this.resultsContainer?.nativeElement.scrollTo({ top: 0, behavior: 'smooth' });
    }

    trackBySeq(index: number, item: Result): number | undefined {
        return item.seqNumber;
    }

    toggleAutoRefresh(): void {
        if (this.isStreamConnected) {
            this.isPaused = true;
            this.abortController?.abort();
            this.isStreamConnected = false;
        } else {
            this.isPaused = false;
            this.connectStream(this.upCursor > 0 ? this.upCursor : undefined);
        }
    }

    selectResult(item: Result): void {
        this.selectedResult = item;
        this.showDetailView = true;
        this.scrollToSelected();
    }

    backToList(): void {
        this.showDetailView = false;
    }

    onListKeydown(event: KeyboardEvent): void {
        if (!this.resultHistory.length) return;

        const currentIndex = this.selectedResult
            ? this.resultHistory.indexOf(this.selectedResult)
            : -1;

        let newIndex = currentIndex;

        if (event.key === 'ArrowDown') {
            event.preventDefault();
            newIndex = Math.min(currentIndex + 1, this.resultHistory.length - 1);
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            newIndex = Math.max(currentIndex - 1, 0);
        } else {
            return;
        }

        if (newIndex !== currentIndex) {
            this.selectedResult = this.resultHistory[newIndex];
            this.scrollToSelected();
            this.cdr.markForCheck();
        }
    }

    formatBytes(bytes: number | undefined): string {
        if (!bytes) return '0 B';
        const units = ['B', 'KB', 'MB'];
        let i = 0;
        let size = bytes;
        while (size >= 1024 && i < units.length - 1) {
            size /= 1024;
            i++;
        }
        return `${i === 0 ? size : size.toFixed(1)} ${units[i]}`;
    }

    formatJson(data: string | undefined): string {
        if (!data) return 'null';
        try {
            return JSON.stringify(JSON.parse(data), null, 2);
        } catch {
            return data;
        }
    }

    copyData(): void {
        if (!this.selectedResult?.data) return;
        navigator.clipboard.writeText(this.formatJson(this.selectedResult.data));
    }

    private scrollToSelected(): void {
        if (!this.selectedResult) return;

        const index = this.resultHistory.indexOf(this.selectedResult);
        const container = this.resultsContainer?.nativeElement;
        if (!container || index < 0) return;

        const items = container.querySelectorAll('[data-result-item]');
        items[index]?.scrollIntoView({ block: 'nearest' });
    }

    private async connectStream(afterSeq?: number): Promise<void> {
        if (this.isDestroyed || !this.feedId) return;

        this.abortController?.abort();
        this.abortController = new AbortController();

        const params = afterSeq !== undefined ? `?afterSeq=${afterSeq}` : '';
        const url = `${this.apiService.apiServer}/feeds/${this.feedId}/results/stream${params}`;

        this.isStreamConnected = true;
        this.cdr.markForCheck();

        try {
            const headers: Record<string, string> = {};
            if (this.getStreamHeaders) {
                Object.assign(headers, await this.getStreamHeaders());
            }

            const response = await fetch(url, {
                headers,
                signal: this.abortController.signal,
            });

            if (!response.ok || !response.body) {
                throw new Error(`Stream response error: ${response.status}`);
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });

                const lines = buffer.split('\n');
                buffer = lines.pop() ?? '';

                for (const line of lines) {
                    if (!line.startsWith('data: ')) continue;

                    try {
                        const result = JSON.parse(line.slice(6)) as Result;
                        this.resultHistory.unshift(result);

                        if (result.seqNumber !== undefined && result.seqNumber > this.upCursor) {
                            this.upCursor = result.seqNumber;
                        }

                        if (this.resultHistory.length > this.MAX_HISTORY) {
                            this.resultHistory.length = this.MAX_HISTORY;
                        }

                        if (!this.selectedResult) {
                            this.selectedResult = result;
                        }

                        this.cdr.markForCheck();
                    } catch {
                        // Skip malformed SSE data lines
                    }
                }
            }
        } catch (err: unknown) {
            if (err instanceof DOMException && err.name === 'AbortError') {
                return;
            }
            console.error('SSE stream error:', err);
        } finally {
            this.isStreamConnected = false;
            this.cdr.markForCheck();
        }

        if (!this.isDestroyed) {
            setTimeout(() => this.connectStream(this.upCursor > 0 ? this.upCursor : undefined), this.RECONNECT_DELAY_MS);
        }
    }

    private async fetchResults(): Promise<void> {
        if (!this.feedId || this.isLoading) return;

        this.isLoading = true;
        this.cdr.markForCheck();

        try {
            const results = await firstValueFrom(
                this.apiService.apiClient.getFeedResults(this.feedId, this.RESULT_LIMIT),
            );

            if (this.isDestroyed) return;

            if (results?.length) {
                this.resultHistory = results;

                const maxSeq = results
                    .map((r) => r.seqNumber)
                    .filter((s): s is number => s !== undefined)
                    .reduce((a, b) => Math.max(a, b), -1);

                if (maxSeq > 0) {
                    this.upCursor = maxSeq;
                }

                if (!this.selectedResult) {
                    this.selectedResult = this.resultHistory[0];
                }
            }
        } catch (error) {
            console.error('Failed to fetch feed results:', error);
        } finally {
            this.isLoading = false;
            this.cdr.markForCheck();
        }
    }
}
