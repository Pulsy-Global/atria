export type HeaderWidgetPosition = 'left' | 'center' | 'right';

export interface HeaderWidgetConfig {
    position: HeaderWidgetPosition;
    priority: number;
}

export const HEADER_WIDGET_POSITIONS = {
    LEFT: 'left' as HeaderWidgetPosition,
    CENTER: 'center' as HeaderWidgetPosition,
    RIGHT: 'right' as HeaderWidgetPosition,
} as const;