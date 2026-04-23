import { ComponentRef } from '@angular/core';
import { FuseAlertComponent } from 'fuse/components/alert';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface NotificationConfig {
    title: string;
    message: string;
    duration?: number;
}

export interface NotificationAlert {
    component: ComponentRef<FuseAlertComponent> | null;
    element: HTMLElement;
    timeoutId?: number;
}