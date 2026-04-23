import { inject } from '@angular/core';
import { forkJoin } from 'rxjs';
import { NavigationService } from 'shared/core/navigation/navigation.service';
import { HeaderWidgetService } from 'shared/core/header/header-widget.service';
import { ThemeToggleComponent } from 'shared/core/header/theme-toggle/theme-toggle.component';

export const initialDataResolver = () => {
    const navigationService = inject(NavigationService);
    const headerWidgetService = inject(HeaderWidgetService);

    navigationService.setNavigationSources(['./navigation/navigation.json']);

    headerWidgetService.setWidgets([
        {
            id: 'theme-toggle',
            component: ThemeToggleComponent,
            order: 1
        }
    ]);

    return forkJoin([
        navigationService.get(),
        headerWidgetService.get()
    ]);
};