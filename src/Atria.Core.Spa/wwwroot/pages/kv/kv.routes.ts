import { Routes } from '@angular/router';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';
import { KvComponent } from './kv.component';

export default [
    {
        path: STRING_EMPTY,
        pathMatch: 'full',
        component: KvComponent,
    },
] as Routes;
