import { Injectable } from '@angular/core';
import { OutputType } from '../../api/api.client';
import { OutputConfig } from '../../shared/output/output-config.types';

@Injectable({
    providedIn: 'root'
})
export class OutputState {
    
    private configsByType = new Map<OutputType, OutputConfig>();

    saveConfig(type: OutputType, config: OutputConfig): void {
        if (!type || !config) {
            return;
        }

        this.configsByType.set(type, config);
    }

    getConfig(type: OutputType): OutputConfig | null {
        if (!type) {
            return null;
        }

        return this.configsByType.get(type) || null;
    }

    hasConfig(type: OutputType): boolean {
        if (!type) {
            return false;
        }

        return this.configsByType.has(type);
    }

    getAllConfigs(): Map<OutputType, OutputConfig> {
        return new Map(this.configsByType);
    }

    clearConfig(type: OutputType): void {
        if (!type) {
            return;
        }

        this.configsByType.delete(type);
    }

    clearAllConfigs(): void {
        this.configsByType.clear();
    }
}