import { Injectable, Signal, signal, computed, Type } from '@angular/core';

export interface SidePanelConfig {
    component: Type<any>;
    width: number;
}

@Injectable({ providedIn: 'root' })
export class SidePanelService {
    private readonly _config = signal<SidePanelConfig | null>(null);
    private readonly _isOpenRef = signal<Signal<boolean>>(signal(false));

    readonly config = this._config.asReadonly();
    readonly isOpen = computed(() => this._isOpenRef()());

    register(config: SidePanelConfig, isOpen: Signal<boolean>): void {
        this._config.set(config);
        this._isOpenRef.set(isOpen);
    }
}
