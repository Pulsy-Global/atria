import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve, RouterStateSnapshot } from '@angular/router';
import { Observable, of, forkJoin } from 'rxjs';
import { TableState } from '../../shared/table/table.types';
import { map } from 'rxjs/operators';
import { FeedTableService } from './feed-table.service'
import { TableStateService } from '../../shared/table/services/table.state.service';
import { FEED_TABLE_PAGE_KEY } from '../../shared/core/constants/common.constants';

@Injectable({
    providedIn: 'root'
})
export class FeedTableResolver implements Resolve<any> {
    
    constructor(
        private _tableStateService: TableStateService, 
        private _feedTableService: FeedTableService) {}

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        const tableState = this._tableStateService
            .getTableState(FEED_TABLE_PAGE_KEY);
        
        return forkJoin({
            networks: this._feedTableService.getNetworks(),
            tags: this._feedTableService.getTags(),
            table: of(tableState)
        }).pipe(
            map(data => ({
                table: data.table,
                networks: data.networks,
                tags: data.tags
            }))
        );
    }
}