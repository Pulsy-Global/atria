import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Navigation } from 'shared/core/navigation/navigation.types';
import { Observable, ReplaySubject, tap, forkJoin, map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NavigationService {
    private _httpClient = inject(HttpClient);
    
    private _navigation: ReplaySubject<Navigation> = new ReplaySubject<Navigation>(1);
    private _navigationSources: string[] = [];

    get navigation$(): Observable<Navigation> {
        return this._navigation.asObservable();
    }

    setNavigationSources(sources: string[]): void {
        this._navigationSources = sources;
    }

    get(): Observable<Navigation> {
        const requests = this._navigationSources.map(source => 
            this._httpClient.get<Navigation>(source)
        );

        return forkJoin(requests).pipe(
            map((navigations: Navigation[]) => {
                const mergedNavigation: Navigation = {
                    items: navigations.reduce((acc, nav) => [...acc, ...nav.items], [])
                };
                return mergedNavigation;
            }),
            tap((navigation) => {
                this._navigation.next(navigation);
            })
        );
    }
}