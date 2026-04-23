import { Injectable } from '@angular/core';
import { Observable, ReplaySubject, tap, of } from 'rxjs';
import { HeaderWidget } from './header-widget.interface';

@Injectable({
    providedIn: 'root'
})
export class HeaderWidgetService {
    private _widgets: ReplaySubject<HeaderWidget[]> = new ReplaySubject<HeaderWidget[]>(1);
    private _widgetsList: HeaderWidget[] = [];

    get widgets$(): Observable<HeaderWidget[]> {
        return this._widgets.asObservable();
    }

    setWidgets(widgets: HeaderWidget[]): void {
        this._widgetsList = [...widgets];
    }

    get(): Observable<HeaderWidget[]> {
        const sortedWidgets = [...this._widgetsList]
            .sort((a, b) => a.order - b.order);
        
        return of(sortedWidgets).pipe(
            tap((widgets) => {
                this._widgets.next(widgets);
            })
        );
    }

    addWidget(widget: HeaderWidget): void {
        const existingIndex = this._widgetsList
            .findIndex(w => w.id === widget.id);
        
        if (existingIndex >= 0) {
            this._widgetsList[existingIndex] = widget;
        } else {
            this._widgetsList.push(widget);
        }
        
        const sortedWidgets = [...this._widgetsList]
            .sort((a, b) => a.order - b.order);

        this._widgets.next(sortedWidgets);
    }

    removeWidget(widgetId: string): void {
        this._widgetsList = this._widgetsList
            .filter(w => w.id !== widgetId);
            
        this._widgets.next([...this._widgetsList]);
    }

    hasWidget(widgetId: string): boolean {
        return this._widgetsList
            .some(w => w.id === widgetId);
    }

    clearWidgets(): void {
        this._widgetsList = [];
        this._widgets.next([]);
    }
}