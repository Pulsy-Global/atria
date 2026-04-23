import { NotificationType } from './notification.types';

export const NOTIFICATION_SELECTORS = {
    template: (type: NotificationType) => `[data-${type}-template]`,
    dismissButton: '.fuse-alert-dismiss-button',
    container: '[data-notification-container]',
    message: '.fuse-alert-message',
    title: '[fuseAlertTitle]'
} as const;

export const NOTIFICATION_CLASSES = {
    translateXFull: 'translate-x-full',
    translateX0: 'translate-x-0',
    hidden: 'hidden',
} as const;

export const NOTIFICATION_DURATIONS = {
    animation: 50,
    warning: 5000,
    success: 3000,
    removal: 300,
    error: 3000,
    info: 5000,
} as const;