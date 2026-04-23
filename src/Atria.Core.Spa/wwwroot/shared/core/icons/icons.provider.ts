import {
    EnvironmentProviders,
    inject,
    provideEnvironmentInitializer,
    Provider,
} from '@angular/core';
import { IconsService } from 'shared/core/icons/icons.service';

export const provideIcons = (): Array<Provider | EnvironmentProviders> => {
    return [provideEnvironmentInitializer(() => inject(IconsService))];
};