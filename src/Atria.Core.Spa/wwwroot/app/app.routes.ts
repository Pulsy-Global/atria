import { Route } from '@angular/router';
import { initialDataResolver } from 'app/app.resolvers';
import { LayoutComponent } from 'app/layout/layout.component';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

// @formatter:off
/* eslint-disable max-len */
/* eslint-disable @typescript-eslint/explicit-function-return-type */
export const appRoutes: Route[] = [
    { path: STRING_EMPTY, pathMatch: 'full', redirectTo: 'feeds' },

    {
        path: STRING_EMPTY,
        component: LayoutComponent,
        resolve: {
            initialData: initialDataResolver,
        },
        children: [
            {
                path: 'feed',
                loadChildren: () => 
                    import('pages/feed/feed.routes'),
            },
            {
                path: 'feeds',
                loadChildren: () => 
                    import('pages/feed-table/feed-table.routes'),
            },
            {
                path: 'output',
                loadChildren: () =>
                    import('pages/output/output.routes'),
            },
            {
                path: 'outputs',
                loadChildren: () =>
                    import('pages/output-table/output-table.routes'),
            },
        ],
    },
];