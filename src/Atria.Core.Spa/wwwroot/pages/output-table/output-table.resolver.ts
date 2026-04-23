import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve, RouterStateSnapshot } from '@angular/router';
import { Observable, of, forkJoin } from 'rxjs';
import { TableState } from '../../shared/table/table.types';
import { map } from 'rxjs/operators';
import { TableStateService } from '../../shared/table/services/table.state.service';
import { OutputTableService } from './output-table.service';
import { OUTPUT_TABLE_PAGE_KEY } from '../../shared/core/constants/common.constants';

@Injectable({
    providedIn: 'root'
})
export class OutputTableResolver implements Resolve<any> {
    
    constructor(
        private _tableStateService: TableStateService,
        private _outputTableService: OutputTableService
    ) {}

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        const tableState = this._tableStateService
            .getTableState(OUTPUT_TABLE_PAGE_KEY);
        
        return forkJoin({
            table: of(tableState),
            tags: this._outputTableService.getTags()
        }).pipe(
            map(data => ({
                table: data.table,
                tags: data.tags
            }))
        );
    }
}