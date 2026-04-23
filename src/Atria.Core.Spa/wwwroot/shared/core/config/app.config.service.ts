import { Injectable } from '@angular/core';
import { environment } from 'app/env/environment';
import { AppServerConfig } from 'shared/core/config/app.config.types';
import { STRING_EMPTY } from '../constants/common.constants';

@Injectable({
    providedIn: 'root'
})
export class AppConfigService
{
    public apiServer = STRING_EMPTY;
    public functionsEnabled = false;

    constructor()
    {
        if (environment.published)
        {
            const apiServerConfig: AppServerConfig =
                (window as any).appConfig;

            this.apiServer = apiServerConfig.apiServer;
            this.functionsEnabled = apiServerConfig.functionsEnabled;
        }
        else
        {
            this.apiServer = environment.apiServer;
            this.functionsEnabled = environment.functionsEnabled ?? false;
        }
    }
}
