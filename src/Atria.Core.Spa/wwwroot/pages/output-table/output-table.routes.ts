import { Routes } from '@angular/router';
import { OutputTableComponent } from './output-table.component';
import { OutputTableResolver } from './output-table.resolver';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

export default [
    {
        path: STRING_EMPTY,
        component: OutputTableComponent,
        resolve: {
            data: OutputTableResolver
        }
    }
] as Routes;