import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve, RouterStateSnapshot } from '@angular/router';
import { Observable, forkJoin, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { OutputService } from './output.service';

@Injectable({
    providedIn: 'root'
})
export class OutputResolver implements Resolve<any> {

    constructor(private _outputService: OutputService) {}

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        return forkJoin({
            tags: this._outputService.getTags(),
            outputId: of(route.paramMap.get('id'))
        }).pipe(
            map(data => ({
                tags: data.tags,
                outputId: data.outputId
            }))
        );
    }
}