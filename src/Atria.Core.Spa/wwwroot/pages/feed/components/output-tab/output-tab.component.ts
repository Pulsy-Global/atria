import { Component, Input, Output as AngularOutput, EventEmitter, OnInit, OnChanges, SimpleChanges, ViewEncapsulation, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule, MatSelect } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { Output, OutputType } from '../../../../api/api.client';
import { WebhookConfigComponent } from '../../../../shared/output/webhook/webhook-config.component';
import { TelegramConfigComponent } from '../../../../shared/output/telegram/telegram-config.component';
import { DiscordConfigComponent } from '../../../../shared/output/discord/discord-config.component';
import { EmailConfigComponent } from '../../../../shared/output/email/email-config.component';
import { PostgresConfigComponent } from '../../../../shared/output/postgres/postgres-config.component';
import { OutputConfigMode } from '../../../../shared/output/output-config.types';
import { OUTPUT_TYPE_CONFIG } from '../../../../shared/output/output-config.config';

@Component({
    selector: 'output-tab',
    standalone: true,
    templateUrl: './output-tab.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatFormFieldModule,
        MatSelectModule,
        MatButtonModule,
        MatIconModule,
        MatCardModule,
        MatChipsModule,
        WebhookConfigComponent,
        TelegramConfigComponent,
        DiscordConfigComponent,
        EmailConfigComponent,
        PostgresConfigComponent
    ]
})
export class OutputTabComponent implements OnInit, OnChanges {
    
    @Input() availableOutputs: Output[] = [];
    @Input() selectedOutputs: Output[] = [];
    @Input() initialOutputs: string[] = [];
    @Input() showHeader: boolean = true;
    @Input() showConfiguration: boolean = true;
    
    @ViewChild('outputSelect') outputSelect: MatSelect;

    selectedConfigIndex: number = 0;
    currentOutput: Output;
    selectValue: string | null = null;

    @AngularOutput() configurationChange = new EventEmitter<Output[]>();

    readonly OutputType = OutputType;
    readonly OutputConfigMode = OutputConfigMode;

    constructor(private cdr: ChangeDetectorRef) {}

    ngOnInit(): void {
        this._updateCurrentOutput();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['selectedOutputs']) {
            this._updateCurrentOutput();
        }
    }

    onOutputAdd(outputId: string): void {
        const output = this.availableOutputs.find(o => o.id === outputId);

        if (output) {
            this.selectedOutputs.push(output);

            this._updateCurrentOutput();

            this.outputSelect.value = null;
            this.selectValue = null;

            this._emitConfiguration();
        }
    }

    removeOutput(index: number): void {
        this.selectedOutputs.splice(index, 1);
        
        this._updateCurrentOutput();

        this.outputSelect.value = null;
        this.selectValue = null;
        
        this._emitConfiguration();
    }

    selectConfig(index: number): void {
        this.selectedConfigIndex = index;

        this.currentOutput = this.selectedOutputs[this.selectedConfigIndex];

        this.cdr.detectChanges();
    }

    getAvailableOptions(): Output[] {
        return this.availableOutputs.filter(output => 
            !this.isOutputSelected(output.id!)
        );
    }

    isOutputSelected(outputId: string): boolean {
        return this.selectedOutputs
            .some(output => output.id === outputId);
    }

    isOutputSaved(outputId: string): boolean {
        return this.initialOutputs?.some(
            output => output === outputId) ?? false;
    }

    getTypeIcon(type: OutputType | undefined): string {
        return OUTPUT_TYPE_CONFIG.getTypeIcon(type);
    }

    getTypeLabel(type: OutputType | undefined): string {
        return OUTPUT_TYPE_CONFIG.getTypeLabel(type);
    }

    private _emitConfiguration(): void {
        this.configurationChange.emit(this.selectedOutputs);
    }

    private _updateCurrentOutput(): void {
        const outputs = this.selectedOutputs ?? [];
        const lastIndex = outputs.length - 1;
    
        if (lastIndex >= 0) {
            this.selectedConfigIndex = lastIndex;
            this.currentOutput = outputs[lastIndex];
        } else {
            this.currentOutput = null;
        }

        this.cdr.detectChanges();
    }
}