import { DOCUMENT } from '@angular/common';
import {
    Component,
    Inject,
    OnDestroy,
    OnInit,
    Renderer2,
    ViewEncapsulation,
    ViewContainerRef,
    ViewChild,
    AfterViewInit,
    ChangeDetectorRef,
    effect,
    signal,
    ComponentRef,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterOutlet } from '@angular/router';
import { FuseLoadingBarComponent } from 'fuse/components/loading-bar';
import {
    FuseHorizontalNavigationComponent,
    FuseNavigationService,
    FuseVerticalNavigationComponent,
} from 'fuse/components/navigation';
import { FuseAlertComponent } from 'fuse/components/alert';
import { FuseConfig, FuseConfigService } from 'fuse/services/config';
import { FuseMediaWatcherService } from 'fuse/services/media-watcher';
import { FusePlatformService } from 'fuse/services/platform';
import { FUSE_VERSION } from 'fuse/version';
import { NavigationService } from 'shared/core/navigation/navigation.service';
import { Navigation } from 'shared/core/navigation/navigation.types';
import { HeaderWidget, HeaderWidgetInstance, ScreenBreakpoint } from 'shared/core/header/header-widget.interface';
import { HeaderWidgetService } from 'shared/core/header/header-widget.service';
import { SidePanelService } from 'shared/core/side-panel/side-panel.service';
import { Subject, combineLatest, takeUntil, distinctUntilChanged, map } from 'rxjs';

const SIDE_PANEL_SCROLL_LOCK_CLASS = 'layout-side-panel-scroll-locked';

@Component({
    selector: 'layout',
    templateUrl: './layout.component.html',
    styleUrls: ['./layout.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        FuseVerticalNavigationComponent,
        FuseHorizontalNavigationComponent,
        FuseLoadingBarComponent,
        FuseAlertComponent,
        MatButtonModule,
        MatIconModule,
        RouterOutlet,
    ],
})
export class LayoutComponent implements OnInit, OnDestroy, AfterViewInit {
    private _unsubscribeAll: Subject<any> = new Subject<any>();

    @ViewChild('headerWidgetsContainer', { read: ViewContainerRef })
    headerWidgetsContainer!: ViewContainerRef;

    @ViewChild('mobileWidgetsContainer', { read: ViewContainerRef })
    mobileWidgetsContainer!: ViewContainerRef;

    @ViewChild('sidePanelContainer', { read: ViewContainerRef })
    sidePanelContainer!: ViewContainerRef;

    config: FuseConfig;
    navigation: Navigation;

    isScreenSmall: boolean = false;
    isScreenMobile: boolean = false;
    private _currentBreakpoints: ScreenBreakpoint[] = [];

    private _widgetInstances: HeaderWidgetInstance[] = [];
    private _mobileWidgetInstances: HeaderWidgetInstance[] = [];
    private _sidePanelRef: ComponentRef<any> | null = null;
    private _viewReady = signal(false);

    constructor(
        @Inject(DOCUMENT) private _document: any,
        private _renderer2: Renderer2,
        private _fuseConfigService: FuseConfigService,
        private _fuseMediaWatcherService: FuseMediaWatcherService,
        private _fusePlatformService: FusePlatformService,
        private _navigationService: NavigationService,
        private _fuseNavigationService: FuseNavigationService,
        private _headerWidgetService: HeaderWidgetService,
        private _changeDetectorRef: ChangeDetectorRef,
        readonly sidePanelService: SidePanelService,
    ) {
        effect(() => {
            const config = this.sidePanelService.config();
            const ready = this._viewReady();
            if (config && ready && this.sidePanelContainer && !this._sidePanelRef) {
                this._sidePanelRef = this.sidePanelContainer.createComponent(config.component);
            }

            this._syncSidePanelScrollLock();
        });


    }

    ngOnInit(): void {
        this._navigationService.navigation$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((navigation: Navigation) => {
                this.navigation = navigation;
            });

        this._fuseMediaWatcherService.onMediaChange$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(({ matchingAliases }) => {
                this.isScreenSmall = !matchingAliases.includes('lg');
                this.isScreenMobile = !matchingAliases.includes('md');
                this._updateCurrentBreakpoints(matchingAliases);
                this._syncSidePanelScrollLock();
            });

        this._fuseConfigService.config$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((config: FuseConfig) => {
                this.config = config;
            });

        this._renderer2.setAttribute(
            this._document.querySelector('[ng-version]'),
            'fuse-version',
            FUSE_VERSION
        );

        this._renderer2.addClass(
            this._document.body,
            this._fusePlatformService.osName
        );
    }

    ngAfterViewInit(): void {
        this._viewReady.set(true);

        const breakpointChanges$ = this._fuseMediaWatcherService
            .onMediaChange$.pipe(
                map(({ matchingAliases }) =>
                    matchingAliases.sort().join(',')),
                distinctUntilChanged()
        );

        combineLatest([breakpointChanges$, this._headerWidgetService.widgets$])
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(([_, widgets]) => {
                this._renderHeaderWidgets(widgets);

                this._changeDetectorRef
                    .detectChanges();
            });
    }

    ngOnDestroy(): void {
        this._setDocumentScrollLock(false);
        this._destroyWidgetInstances();
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }

    toggleNavigation(name: string): void {
        const navigation = this._fuseNavigationService
            .getComponent<FuseVerticalNavigationComponent>(name);

        if (navigation) {
            navigation.toggle();
        }
    }

    private _renderHeaderWidgets(widgets: HeaderWidget[]): void {
        if (!this.headerWidgetsContainer) return;

        this._destroyWidgetInstances();

        const { headerWidgets, mobileWidgets } = this._splitWidgetsByScreenSize(widgets);

        const sortedHeaderWidgets = [...headerWidgets]
            .sort((a, b) => a.order - b.order);

        sortedHeaderWidgets.forEach(widget => {
            const componentRef = this.headerWidgetsContainer
                .createComponent(widget.component);

            if (widget.data) {
                Object.assign(componentRef.instance, widget.data);
            }

            this._widgetInstances.push({
                id: widget.id,
                componentRef,
                order: widget.order
            });
        });

        if (this.isScreenMobile && this.mobileWidgetsContainer) {
            const sortedMobileWidgets = [...mobileWidgets]
                .sort((a, b) => a.order - b.order);

            sortedMobileWidgets.forEach(widget => {
                const componentRef = this.mobileWidgetsContainer
                    .createComponent(widget.component);

                if (widget.data) {
                    Object.assign(componentRef.instance, widget.data);
                }

                this._mobileWidgetInstances.push({
                    id: widget.id,
                    componentRef,
                    order: widget.order
                });
            });
        }
    }

    private _updateCurrentBreakpoints(matchingAliases: string[]): void {
        const validBreakpoints: ScreenBreakpoint[] = ['xs', 'sm', 'md', 'lg', 'xl'];

        const isValidBreakpoint = (alias: string): alias is ScreenBreakpoint =>
            validBreakpoints.includes(alias as ScreenBreakpoint);

        this._currentBreakpoints = matchingAliases.filter(isValidBreakpoint);
    }

    private _shouldHideWidget(widget: HeaderWidget): boolean {
        if (this._currentBreakpoints.length === 0)
            return false;

        const currentBreakpoint = this._currentBreakpoints
            [this._currentBreakpoints.length - 1];

        return widget.hideOnBreakpoints?.includes(
            currentBreakpoint) ?? false;
    }

    private _splitWidgetsByScreenSize(widgets: HeaderWidget[]): {
        headerWidgets: HeaderWidget[];
        mobileWidgets: HeaderWidget[];
    } {
        const headerWidgets = widgets.filter(widget => !this._shouldHideWidget(widget));
        const mobileWidgets = widgets.filter(widget => this._shouldHideWidget(widget));

        return { headerWidgets, mobileWidgets };
    }

    private _destroyWidgetInstances(): void {
        this._widgetInstances.forEach(instance => {
            instance.componentRef.destroy();
        });

        this._widgetInstances = [];

        if (this.headerWidgetsContainer) {
            this.headerWidgetsContainer.clear();
        }

        this._mobileWidgetInstances.forEach(instance => {
            instance.componentRef.destroy();
        });

        this._mobileWidgetInstances = [];

        if (this.mobileWidgetsContainer) {
            this.mobileWidgetsContainer.clear();
        }
    }

    private _syncSidePanelScrollLock(): void {
        this._setDocumentScrollLock(
            this.sidePanelService.isOpen() && this.isScreenSmall
        );
    }

    private _setDocumentScrollLock(locked: boolean): void {
        const root = this._document.documentElement;
        const body = this._document.body;

        if (!root || !body) {
            return;
        }

        if (locked) {
            this._renderer2.addClass(root, SIDE_PANEL_SCROLL_LOCK_CLASS);
            this._renderer2.addClass(body, SIDE_PANEL_SCROLL_LOCK_CLASS);
            return;
        }

        this._renderer2.removeClass(root, SIDE_PANEL_SCROLL_LOCK_CLASS);
        this._renderer2.removeClass(body, SIDE_PANEL_SCROLL_LOCK_CLASS);
    }
}
