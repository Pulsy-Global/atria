import { Routes } from '@angular/router';
import { OutputComponent } from './output.component';
import { OutputResolver } from './output.resolver';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

export default [
    {
        path: STRING_EMPTY,
        component: OutputComponent,
        resolve: {
            data: OutputResolver
        }
    },
    {
        path: ':id',
        component: OutputComponent,
        resolve: {
            data: OutputResolver
        }
    }
] as Routes;