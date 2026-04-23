import { Injectable, } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve, RouterStateSnapshot } from '@angular/router';
import { Observable, forkJoin, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { FeedService } from './feed.service';

@Injectable({
    providedIn: 'root'
})
export class FeedResolver implements Resolve<any> {

    constructor(private _feedService: FeedService) {}

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        return forkJoin({
            networks: this._feedService.getNetworks(),
            outputs: this._feedService.getOutputs(),
            tags: this._feedService.getTags(),
            feedId: of(route.paramMap.get('id'))
        }).pipe(
            map(data => ({
                networks: data.networks,
                outputs: data.outputs,
                tags: data.tags,
                feedId: data.feedId
            }))
        );
    }
}