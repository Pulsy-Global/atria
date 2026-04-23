import { Routes } from '@angular/router';
import { FeedTableComponent } from './feed-table.component';
import { FeedTableResolver } from './feed-table.resolver';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

export default [
    {
        path: STRING_EMPTY,
        component: FeedTableComponent,
        resolve: {
            data: FeedTableResolver
        }
    }
] as Routes;