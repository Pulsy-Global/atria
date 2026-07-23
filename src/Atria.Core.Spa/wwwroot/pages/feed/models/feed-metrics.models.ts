export type FeedMetricsRange =
    | 'lastHour'
    | 'last24Hours'
    | 'last7Days'
    | 'last30Days';

export type FeedMetricsAvailability =
    | 'available'
    | 'noData'
    | 'partial'
    | 'unavailable';

export interface FeedMetricPoint {
    timestamp: string;
    value: number;
}

export interface FeedMetricSeries {
    key: string;
    unit: 'count' | 'bytes';
    status: FeedMetricsAvailability;
    points: FeedMetricPoint[];
}

export interface FeedMetricsSummary {
    processedBlocks: number | null;
    producedOutputs: number | null;
    processingFailures: number | null;
    deliveryAttempts: number | null;
    successfulDeliveries: number | null;
    failedDeliveries: number | null;
    deliverySuccessRate: number | null;
    dataReductionRate: number | null;
    processedBytes: number | null;
    producedBytes: number | null;
    deliveredBytes: number | null;
}

export interface FeedMetricsResponse {
    feedId?: string;
    range: FeedMetricsRange;
    status: FeedMetricsAvailability;
    generatedAt: string;
    start: string;
    end: string;
    resolutionSeconds: number;
    summary: FeedMetricsSummary;
    series: FeedMetricSeries[];
    warnings: string[];
}

export interface FeedMetricsRangeOption {
    value: FeedMetricsRange;
    label: string;
    longLabel: string;
}

export interface FeedMetricChartLine {
    key: string;
    label: string;
    color: string;
    path: string;
    points: FeedMetricChartPoint[];
}

export interface FeedMetricChartPoint {
    timestamp: string;
    value: number;
    x: number;
    y: number;
}

export interface FeedMetricChartTooltipValue {
    key: string;
    label: string;
    color: string;
    value: number | null;
    y: number | null;
}

export interface FeedMetricChartTooltip {
    timestamp: string;
    lineX: number;
    leftPercent: number;
    values: FeedMetricChartTooltipValue[];
}
