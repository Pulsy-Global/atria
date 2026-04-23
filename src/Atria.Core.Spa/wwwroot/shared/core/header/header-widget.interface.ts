import { ComponentRef, Type } from '@angular/core';

export type ScreenBreakpoint = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

export interface HeaderWidget {
    id: string;
    component: Type<any>;
    order: number;
    data?: any;
    hideOnBreakpoints?: ScreenBreakpoint[];
}

export interface HeaderWidgetInstance {
    id: string;
    componentRef: ComponentRef<any>;
    order: number;
}