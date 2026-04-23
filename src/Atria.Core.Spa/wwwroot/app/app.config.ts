import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { LuxonDateAdapter } from '@angular/material-luxon-adapter';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideFuse } from 'fuse';
import { appRoutes } from 'app/app.routes';
import { provideIcons } from 'shared/core/icons/icons.provider';
import { MonacoEditorModule } from 'ngx-monaco-editor-v2';

export const appConfig: ApplicationConfig = {
    providers: [
        provideAnimations(),
        provideHttpClient(),
        provideRouter(
            appRoutes,
            withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })
        ),

        // Monaco Editor
        importProvidersFrom(
            MonacoEditorModule.forRoot({
                baseUrl: window.location.origin + "/assets/monaco/min/vs",
                requireConfig: {
                    preferScriptTags: true
                },
                defaultOptions: {
                    scrollBeyondLastLine: false,
                    theme: 'vs-dark',
                    minimap: { enabled: false },
                    automaticLayout: true
                }
            })
        ),

        // Material Date Adapter
        {
            provide: DateAdapter,
            useClass: LuxonDateAdapter,
        },
        {
            provide: MAT_DATE_FORMATS,
            useValue: {
                parse: {
                    dateInput: 'D',
                },
                display: {
                    dateInput: 'DDD',
                    monthYearLabel: 'LLL yyyy',
                    dateA11yLabel: 'DD',
                    monthYearA11yLabel: 'LLLL yyyy',
                },
            },
        },

        // Fuse
        provideIcons(),
        provideFuse({
            fuse: {
                scheme: 'auto',
                screens: {
                    xs: '0px',
                    sm: '600px',
                    md: '960px',
                    lg: '1280px',
                    xl: '1440px',
                },
                theme: 'theme-pulsy',
                themes: [
                    {
                        id: 'theme-default',
                        name: 'Default',
                    },
                    {
                        id: 'theme-pulsy-light',
                        name: 'Pulsy Light',
                    },
                    {
                        id: 'theme-pulsy-dark',
                        name: 'Pulsy Dark',
                    },
                    {
                        id: 'theme-teal',
                        name: 'Teal',
                    },
                    {
                        id: 'theme-rose',
                        name: 'Rose',
                    },
                    {
                        id: 'theme-purple',
                        name: 'Purple',
                    },
                    {
                        id: 'theme-amber',
                        name: 'Amber',
                    },
                ],
            },
        }),
    ],
};