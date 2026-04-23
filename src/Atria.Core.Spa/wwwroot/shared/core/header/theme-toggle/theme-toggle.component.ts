import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ThemeService } from './theme.service';
import { ThemeOption } from './theme-toggle.types';
import { THEME_CONFIG } from './theme-toggle.config';

@Component({
    selector: 'theme-toggle',
    templateUrl: './theme-toggle.component.html',
    styleUrls: ['./theme-toggle.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        MatButtonModule,
        MatIconModule,
    ],
})
export class ThemeToggleComponent implements OnInit {
    showThemeMenu: boolean = false;

    constructor(readonly themeService: ThemeService) {}

    ngOnInit(): void {
        this.themeService.init();
    }

    get schemeIcon(): string {
        return this.themeService.schemeIcon;
    }

    get availableThemeOptions(): ThemeOption[] {
        return THEME_CONFIG.getAvailableOptions(this.themeService.currentScheme);
    }

    onThemeHover(show: boolean): void {
        this.showThemeMenu = show;
    }

    selectScheme(scheme: string): void {
        this.themeService.cycleScheme();
        this.showThemeMenu = false;
    }
}
