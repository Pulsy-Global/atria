import { Injectable, OnDestroy } from '@angular/core';
import { FuseConfigService, Scheme } from 'fuse/services/config';
import { FuseMediaWatcherService } from 'fuse/services/media-watcher';
import { Subject, combineLatest, takeUntil } from 'rxjs';
import { THEME_CONFIG } from './theme-toggle.config';

@Injectable({ providedIn: 'root' })
export class ThemeService implements OnDestroy {
    private readonly _unsubscribeAll = new Subject<void>();
    private _initialized = false;

    currentScheme: Scheme = 'auto';

    constructor(
        private readonly _fuseConfigService: FuseConfigService,
        private readonly _fuseMediaWatcherService: FuseMediaWatcherService,
    ) {}

    init(): void {
        if (this._initialized) return;
        this._initialized = true;

        const saved = THEME_CONFIG.loadFromStorage();
        if (saved) {
            this._fuseConfigService.config = { scheme: saved };
        }

        combineLatest([
            this._fuseConfigService.config$,
            this._fuseMediaWatcherService.onMediaQueryChange$([
                '(prefers-color-scheme: dark)',
                '(prefers-color-scheme: light)',
            ]),
        ])
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe(([config]) => {
            this.currentScheme = config.scheme;

            let scheme: Scheme = config.scheme;
            if (scheme === 'auto') {
                scheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            }

            document.body.classList.remove('light', 'dark');
            document.body.classList.add(scheme);

            document.body.classList.forEach((cls) => {
                if (cls.startsWith('theme-')) {
                    document.body.classList.remove(cls, cls.split('-')[1]);
                }
            });

            let theme = config.theme;
            if (theme === 'theme-pulsy') {
                theme = scheme === 'dark' ? 'theme-pulsy-dark' : 'theme-pulsy-light';
            }
            document.body.classList.add(theme);
        });
    }

    cycleScheme(): void {
        const order: Scheme[] = ['light', 'dark', 'auto'];
        const idx = order.indexOf(this.currentScheme);
        const next = order[(idx + 1) % order.length];
        THEME_CONFIG.saveToStorage(next);
        this._fuseConfigService.config = { scheme: next };
    }

    get schemeIcon(): string {
        return THEME_CONFIG.getThemeIcon(this.currentScheme);
    }

    get schemeLabel(): string {
        return THEME_CONFIG.getThemeLabel(this.currentScheme);
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }
}
