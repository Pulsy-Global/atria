import { Routes } from '@angular/router';
import { FeedComponent } from 'pages/feed/feed.component';
import { FeedResolver } from './feed.resolver';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

export default [
    {
        path: STRING_EMPTY,
        component: FeedComponent,
        resolve: {
            data: FeedResolver
        }
    },
    {
        path: ':id',
        component: FeedComponent,
        resolve: {
            data: FeedResolver
        }
    },
] as Routes;