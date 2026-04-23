import { Injectable, Renderer2, RendererFactory2 } from '@angular/core';
import { NotificationAlert, NotificationType, NotificationConfig } from './notification.types';
import { NOTIFICATION_SELECTORS, NOTIFICATION_CLASSES, NOTIFICATION_DURATIONS } from './notification.config';

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private readonly alerts: NotificationAlert[] = [];
    private readonly renderer: Renderer2;

    constructor(rendererFactory: RendererFactory2) {
        this.renderer = rendererFactory.createRenderer(null, null);
    }

    showSuccessAlert(title: string, message: string, duration?: number): void {
        this.createAlert('success', { title, message, duration });
    }

    showErrorAlert(title: string, message: string, duration?: number): void {
        this.createAlert('error', { title, message, duration });
    }

    showWarningAlert(title: string, message: string, duration?: number): void {
        this.createAlert('warning', { title, message, duration });
    }

    showInfoAlert(title: string, message: string, duration?: number): void {
        this.createAlert('info', { title, message, duration });
    }

    clearAll(): void {
        [...this.alerts].forEach(alert => this.removeAlert(alert));
    }

    private createAlert(type: NotificationType, config: NotificationConfig): void {
        const elements = this.getRequiredElements(type);
        if (!elements) return;

        const { container, template } = elements;

        let duration = config.duration ?? NOTIFICATION_DURATIONS[type];

        const alertElement = this.cloneAndPrepareAlert(template, config);
        const alert = this.createAlertObject(alertElement);

        this.setupDismissHandler(alert);
        this.addToContainer(container, alertElement);
        this.scheduleRemoval(alert, duration);
        this.animateIn(alertElement);

        this.alerts.push(alert);
    }

    private getRequiredElements(type: NotificationType): { container: HTMLElement; template: HTMLElement } | null {
        const container = document.querySelector(
            NOTIFICATION_SELECTORS.container) as HTMLElement;

        const template = document.querySelector(
            NOTIFICATION_SELECTORS.template(type)) as HTMLElement;

        if (!container) {
            console.warn('Notification container not found');
            return null;
        }

        if (!template) {
            console.warn(`Template for type ${type} not found`);
            return null;
        }

        return { container, template };
    }

    private cloneAndPrepareAlert(
        template: HTMLElement, 
        config: NotificationConfig
    ): HTMLElement {
        const alertElement = template.cloneNode(true) as HTMLElement;
        
        this.renderer.removeClass(
            alertElement, 
            NOTIFICATION_CLASSES.hidden);

        this.updateAlertContent(alertElement, config);
        
        return alertElement;
    }

    private updateAlertContent(alertElement: HTMLElement, config: NotificationConfig): void {
        const titleElement = alertElement.querySelector(
            NOTIFICATION_SELECTORS.title);

        const messageElement = alertElement.querySelector(
            NOTIFICATION_SELECTORS.message);

        if (titleElement) {
            titleElement.textContent = config.title;
        }

        if (messageElement) {
            messageElement.textContent = config.message;
        }
    }

    private createAlertObject(alertElement: HTMLElement): NotificationAlert {
        return {
            component: null,
            element: alertElement
        };
    }

    private setupDismissHandler(alert: NotificationAlert): void {
        const dismissButton = alert.element.querySelector(
            NOTIFICATION_SELECTORS.dismissButton);
        
        if (dismissButton) {
            this.renderer.listen(dismissButton, 'click', () => {
                this.removeAlert(alert);
            });
        }
    }

    private addToContainer(
        container: HTMLElement, 
        alertElement: HTMLElement
    ): void {
        this.renderer.appendChild(
            container, 
            alertElement);
    }

    private animateIn(alertElement: HTMLElement): void {
        setTimeout(() => {
            this.renderer.removeClass(
                alertElement, NOTIFICATION_CLASSES.translateXFull);

            this.renderer.addClass(
                alertElement, NOTIFICATION_CLASSES.translateX0);

        }, NOTIFICATION_DURATIONS.animation);
    }

    private scheduleRemoval(alert: NotificationAlert, duration: number): void {
        alert.timeoutId = window.setTimeout(() => {
            this.removeAlert(alert);
        }, duration);
    }

    private removeAlert(alert: NotificationAlert): void {
        const index = this.alerts.indexOf(alert);

        if (index === -1) {
            return;
        }

        this.clearTimeout(alert);
        this.animateOut(alert.element);
        this.scheduleCleanup(alert, index);
    }

    private clearTimeout(alert: NotificationAlert): void {
        if (alert.timeoutId) {
            clearTimeout(alert.timeoutId);
            alert.timeoutId = undefined;
        }
    }

    private animateOut(element: HTMLElement): void {
        this.renderer.removeClass(element, NOTIFICATION_CLASSES.translateX0);
        this.renderer.addClass(element, NOTIFICATION_CLASSES.translateXFull);
    }

    private scheduleCleanup(alert: NotificationAlert, index: number): void {
        setTimeout(() => {
            this.alerts.splice(index, 1);
            this.removeFromDOM(alert.element);
        }, NOTIFICATION_DURATIONS.removal);
    }

    private removeFromDOM(element: HTMLElement): void {
        if (element.parentNode) {
            this.renderer.removeChild(
                element.parentNode, 
                element);
        }
    }
}