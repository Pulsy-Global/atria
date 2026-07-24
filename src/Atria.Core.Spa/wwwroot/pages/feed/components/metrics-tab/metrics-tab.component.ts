import { CommonModule, DOCUMENT } from '@angular/common';
import {
    Component,
    Input,
    OnChanges,
    OnDestroy,
    SimpleChanges,
    ViewEncapsulation,
    inject,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
    EMPTY,
    Observable,
    Subject,
    Subscription,
    catchError,
    filter,
    finalize,
    merge,
    switchMap,
    tap,
    timer,
} from 'rxjs';
import {
    FeedMetricChartLine,
    FeedMetricChartPoint,
    FeedMetricChartTooltip,
    FeedMetricSeries,
    FeedMetricsRange,
    FeedMetricsRangeOption,
    FeedMetricsResponse,
} from '../../models/feed-metrics.models';
import { FeedMetricsSource } from '../../services/feed-metrics-source';
import { FeedMetricsService } from '../../services/feed-metrics.service';

@Component({
    selector: 'app-metrics-tab',
    standalone: true,
    templateUrl: './metrics-tab.component.html',
    styleUrls: ['./metrics-tab.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatButtonModule,
        MatIconModule,
        MatProgressSpinnerModule,
    ],
})
export class MetricsTabComponent implements OnChanges, OnDestroy {
    @Input() metricsResourceId: string | null = null;
    @Input() metricsSource: FeedMetricsSource | null = null;

    readonly rangeOptions: FeedMetricsRangeOption[] = [
        { value: 'lastHour', label: '1H', longLabel: 'Last hour' },
        { value: 'last24Hours', label: '24H', longLabel: 'Last 24 hours' },
        { value: 'last7Days', label: '7D', longLabel: 'Last 7 days' },
    ];
    readonly chartWidth = 720;
    readonly chartHeight = 180;

    selectedRange: FeedMetricsRange = 'lastHour';
    metrics: FeedMetricsResponse | null = null;
    chartLines: FeedMetricChartLine[] = [];
    chartTooltip: FeedMetricChartTooltip | null = null;
    isLoading = false;
    isRefreshing = false;
    requestFailed = false;

    private readonly _refresh = new Subject<void>();
    private readonly _document = inject(DOCUMENT);
    private readonly _metricsService = inject(FeedMetricsService);
    private _polling?: Subscription;
    private _visibilityHandler = (): void => {
        if (this._document.visibilityState === 'visible') {
            this._refresh.next();
        }
    };

    constructor() {
        this._document.addEventListener(
            'visibilitychange',
            this._visibilityHandler
        );
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['metricsResourceId'] || changes['metricsSource']) {
            this._startPolling();
        }
    }

    ngOnDestroy(): void {
        this._polling?.unsubscribe();
        this._refresh.complete();
        this._document.removeEventListener(
            'visibilitychange',
            this._visibilityHandler
        );
    }

    setRange(range: FeedMetricsRange): void {
        if (range === this.selectedRange) {
            return;
        }

        this.selectedRange = range;
        this.metrics = null;
        this.chartLines = [];
        this.chartTooltip = null;
        this._startPolling();
    }

    refresh(): void {
        this._refresh.next();
    }

    formatCount(value: number | null | undefined): string {
        if (value === null || value === undefined) {
            return '—';
        }

        return Math.round(value).toLocaleString('en-US');
    }

    formatBytes(value: number | null | undefined): string {
        if (value === null || value === undefined) {
            return '—';
        }

        const units = ['B', 'KB', 'MB', 'GB', 'TB'];
        let normalized = Math.max(0, value);
        let unitIndex = 0;

        while (normalized >= 1024 && unitIndex < units.length - 1) {
            normalized /= 1024;
            unitIndex += 1;
        }

        return `${normalized.toLocaleString('en-US', {
            maximumFractionDigits: normalized >= 10 || unitIndex === 0 ? 0 : 1,
        })} ${units[unitIndex]}`;
    }

    get hasActivity(): boolean {
        return (
            this.metrics?.series.some((series) =>
                series.points.some((point) => point.value > 0)
            ) ?? false
        );
    }

    get rangeLabel(): string {
        return (
            this.rangeOptions.find(
                (option) => option.value === this.selectedRange
            )?.longLabel ?? ''
        );
    }

    get isStale(): boolean {
        if (!this.metrics) {
            return false;
        }

        return (
            Date.now() - new Date(this.metrics.generatedAt).getTime() > 60000
        );
    }

    get accessibleChartSeries(): FeedMetricSeries[] {
        const keys = new Set<string>(
            chartDefinitions.map((definition) => definition.key)
        );

        return (
            this.metrics?.series.filter((series) => keys.has(series.key)) ?? []
        );
    }

    chartSeriesLabel(key: string): string {
        return (
            chartDefinitions.find((definition) => definition.key === key)
                ?.label ?? key
        );
    }

    showChartPoint(event: PointerEvent): void {
        const svg = event.currentTarget as SVGSVGElement | null;
        const pointsCount = this._chartPointsCount();
        if (!svg || pointsCount === 0) {
            return;
        }

        const bounds = svg.getBoundingClientRect();
        const ratio = Math.min(
            1,
            Math.max(0, (event.clientX - bounds.left) / bounds.width)
        );
        this._setChartPoint(Math.round(ratio * (pointsCount - 1)));
    }

    focusChart(): void {
        if (this.chartTooltip) {
            return;
        }

        const pointsCount = this._chartPointsCount();
        if (pointsCount > 0) {
            this._setChartPoint(pointsCount - 1);
        }
    }

    moveChartPoint(event: KeyboardEvent): void {
        const pointsCount = this._chartPointsCount();
        if (pointsCount === 0) {
            return;
        }

        const currentIndex = this._chartPointIndex();
        let nextIndex: number;
        switch (event.key) {
            case 'ArrowLeft':
                nextIndex = Math.max(0, currentIndex - 1);
                break;
            case 'ArrowRight':
                nextIndex = Math.min(pointsCount - 1, currentIndex + 1);
                break;
            case 'Home':
                nextIndex = 0;
                break;
            case 'End':
                nextIndex = pointsCount - 1;
                break;
            default:
                return;
        }

        event.preventDefault();
        this._setChartPoint(nextIndex);
    }

    clearChartPoint(): void {
        this.chartTooltip = null;
    }

    formatChartTimestamp(timestamp: string): string {
        return new Date(timestamp).toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    }

    combinedFailures(): number | null {
        const processing = this.metrics?.summary.processingFailures;
        const delivery = this.metrics?.summary.failedDeliveries;

        return processing === null ||
            processing === undefined ||
            delivery === null ||
            delivery === undefined
            ? null
            : processing + delivery;
    }

    formatPercent(value: number | null | undefined): string {
        if (value === null || value === undefined) {
            return this.metrics?.status === 'noData' ? '0%' : '—';
        }

        return `${value.toLocaleString('en-US', { maximumFractionDigits: 2 })}%`;
    }

    trackRange(_: number, option: FeedMetricsRangeOption): string {
        return option.value;
    }

    trackLine(_: number, line: FeedMetricChartLine): string {
        return line.key;
    }

    private _startPolling(): void {
        this._polling?.unsubscribe();
        this.requestFailed = false;

        if (!this.metricsResourceId) {
            this.metrics = null;
            this.chartLines = [];
            this.isLoading = false;

            return;
        }

        this._polling = merge(timer(0, 30000), this._refresh)
            .pipe(
                filter(() => this._document.visibilityState === 'visible'),
                switchMap(() => this._load())
            )
            .subscribe();
    }

    private _load(): Observable<FeedMetricsResponse> {
        if (!this.metricsResourceId) {
            return EMPTY;
        }

        this.isLoading = this.metrics === null;
        this.isRefreshing = this.metrics !== null;
        this.requestFailed = false;

        const source = this.metricsSource ?? this._metricsService;

        return source
            .getMetrics(this.metricsResourceId, this.selectedRange)
            .pipe(
                tap((metrics) => {
                    this.metrics = metrics;
                    this.chartLines = this._createChartLines(metrics);
                    this.chartTooltip = null;
                }),
                catchError(() => {
                    this.requestFailed = true;

                    return EMPTY;
                }),
                finalize(() => {
                    this.isLoading = false;
                    this.isRefreshing = false;
                })
            );
    }

    private _createChartLines(
        metrics: FeedMetricsResponse
    ): FeedMetricChartLine[] {
        const selectedSeries = chartDefinitions
            .map((definition) => ({
                definition,
                series: metrics.series.find(
                    (series) => series.key === definition.key
                ),
            }))
            .filter(
                (
                    item
                ): item is {
                    definition: (typeof chartDefinitions)[number];
                    series: FeedMetricSeries;
                } => item.series !== undefined
            );
        const maximum = Math.max(
            1,
            ...selectedSeries.flatMap((item) =>
                item.series.points.map((point) => point.value)
            )
        );

        return selectedSeries.map(({ definition, series }) => {
            const points = this._createChartPoints(series, maximum);

            return {
                ...definition,
                path: this._createPath(points),
                points,
            };
        });
    }

    private _createChartPoints(
        series: FeedMetricSeries,
        maximum: number
    ): FeedMetricChartPoint[] {
        return series.points.map((point, index) => ({
            ...point,
            x:
                series.points.length === 1
                    ? this.chartWidth / 2
                    : (index / (series.points.length - 1)) * this.chartWidth,
            y:
                this.chartHeight -
                (Math.max(0, point.value) / maximum) * (this.chartHeight - 12) -
                6,
        }));
    }

    private _createPath(points: FeedMetricChartPoint[]): string {
        if (points.length === 0) {
            return '';
        }

        return points
            .map((point, index) => {
                return `${index === 0 ? 'M' : 'L'} ${point.x.toFixed(2)} ${point.y.toFixed(2)}`;
            })
            .join(' ');
    }

    private _chartPointsCount(): number {
        return Math.max(
            0,
            ...this.chartLines.map((line) => line.points.length)
        );
    }

    private _chartPointIndex(): number {
        if (!this.chartTooltip) {
            return this._chartPointsCount() - 1;
        }

        const referenceLine = this.chartLines.find(
            (line) => line.points.length > 0
        );

        return Math.max(
            0,
            referenceLine?.points.findIndex(
                (point) => point.timestamp === this.chartTooltip?.timestamp
            ) ?? 0
        );
    }

    private _setChartPoint(index: number): void {
        const referencePoint = this.chartLines.find(
            (line) => line.points[index]
        )?.points[index];
        if (!referencePoint) {
            this.chartTooltip = null;

            return;
        }

        this.chartTooltip = {
            timestamp: referencePoint.timestamp,
            lineX: referencePoint.x,
            leftPercent: Math.min(
                88,
                Math.max(12, (referencePoint.x / this.chartWidth) * 100)
            ),
            values: this.chartLines.map((line) => ({
                key: line.key,
                label: line.label,
                color: line.color,
                value: line.points[index]?.value ?? null,
                y: line.points[index]?.y ?? null,
            })),
        };
    }
}

const chartDefinitions = [
    {
        key: 'processed_bytes',
        label: 'Input',
        color: '#8b5cf6',
    },
    {
        key: 'produced_bytes',
        label: 'Output',
        color: '#38bdf8',
    },
    {
        key: 'delivered_bytes',
        label: 'Delivered',
        color: '#10b981',
    },
] as const;
