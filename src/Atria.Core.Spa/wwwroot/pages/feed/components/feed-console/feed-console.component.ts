import { Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import type { TestResult } from '../../../../api/api.client';
import { FeedConsoleOutput } from './feed-console-output';

@Component({
    selector: 'feed-console',
    standalone: true,
    templateUrl: './feed-console.component.html',
    styleUrls: ['./feed-console.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
        MatCheckboxModule,
        MatTooltipModule,
        MatProgressSpinnerModule
    ]
})
export class FeedConsoleComponent implements OnChanges, OnDestroy {
    @Input({ required: true }) testForm!: FormGroup;
    @Input() isTestRunning = false;
    @Input() isLoadingLatestBlock = false;
    @Input() testPassed = false;
    @Input() testFailed = false;
    @Input() showTestButton = false;
    @Input() showDeployButton = false;
    @Input() isDeploying = false;
    @Input() deploySucceeded = false;
    @Input() deployFailed = false;
    @Input() isDeployDisabled = false;
    @Input() canLoadLatestBlock = false;
    @Input() canAttemptTest = false;
    @Input() hasOutputs = false;

    @Output() testFeed = new EventEmitter<void>();
    @Output() deploy = new EventEmitter<void>();
    @Output() loadLatestBlock = new EventEmitter<void>();
    @Output() runTest = new EventEmitter<void>();

    isExpanded = false;
    isAutoCollapse = true;
    isProcessingAnimation = false;
    statusPulse: 'success' | 'error' | null = null;
    private readonly _consoleOutput = new FeedConsoleOutput();
    private _pendingStatus: 'success' | 'error' | null = null;

    private _processingAnimationStart = 0;
    private _processingAnimationTimeout?: number;
    private readonly _processingAnimationMinMs = 1000;
    private _statusPulseTimeout?: number;
    private readonly _statusPulseMinMs = 1000;

    constructor(private readonly _elementRef: ElementRef<HTMLElement>) {}

    get consoleOutput(): string[] {
        return this._consoleOutput.lines;
    }

    @HostListener('document:click', ['$event'])
    onDocumentClick(event: MouseEvent): void {
        if (!this.isExpanded || !this.isAutoCollapse) {
            return;
        }

        const target = event.target as Node | null;

        if (!target) {
            return;
        }

        if (this._elementRef.nativeElement.contains(target)) {
            return;
        }

        this.isExpanded = false;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['isTestRunning'] || changes['isDeploying']) {
            const isRunning = this.isTestRunning || this.isDeploying;

            if (isRunning) {
                this._startProcessingAnimation();
            } else {
                this._stopProcessingAnimation();
            }
        }

        if (changes['testPassed'] && this.testPassed) {
            this._queueStatusPulse('success');
        }

        if (changes['testFailed'] && this.testFailed) {
            this._queueStatusPulse('error');
        }

        if (changes['deploySucceeded'] && this.deploySucceeded) {
            this._queueStatusPulse('success');
        }

        if (changes['deployFailed'] && this.deployFailed) {
            this._queueStatusPulse('error');
        }
    }

    ngOnDestroy(): void {
        this._clearProcessingAnimationTimeout();
        this._clearStatusPulseTimeout();
    }

    toggle(): void {
        this.isExpanded = !this.isExpanded;
    }

    open(): void {
        this.isExpanded = true;
    }

    collapse(): void {
        this.isExpanded = false;
    }

    clearOutput(): void {
        this._consoleOutput.clear();
    }

    logTestStart(): void {
        const testBlockNumber = this.testForm.get('testBlockNumber')?.value;
        const shouldExecuteOutputs = this.hasOutputs && this.testForm.get('executeOutputs')?.value;

        this.clearOutput();
        this._consoleOutput.addLine('=== Starting Test Execution ===');
        this._consoleOutput.addLine(`Block Number: ${testBlockNumber}`);
        this._consoleOutput.addLine(`Execute Outputs: ${shouldExecuteOutputs ? 'Yes' : 'No'}`);
        this._consoleOutput.addLine('');
    }

    logTestSuccess(result: TestResult): void {
        this._consoleOutput.addLine('--- Filter Results ---');

        if (result.filterError) {
            this._consoleOutput.addLine('Filter Error:');
            this._consoleOutput.addLine(`Message: ${result.filterError.message}`);

            if (result.filterError.line) {
                this._consoleOutput.addLine(`Line: ${result.filterError.line}, Column: ${result.filterError.column}`);
            }
        } else if (result.filterResult) {
            this._consoleOutput.addLine('Filter executed successfully');
            this._consoleOutput.addResult('Result', result.filterResult);
        } else {
            this._consoleOutput.addLine('Warning: No filter configured');
        }

        this._consoleOutput.addLine('');
        this._consoleOutput.addLine('--- Function Results ---');

        if (result.functionError) {
            this._consoleOutput.addLine('Function Error:');
            this._consoleOutput.addLine(`Message: ${result.functionError.message}`);

            if (result.functionError.line) {
                this._consoleOutput.addLine(`Line: ${result.functionError.line}, Column: ${result.functionError.column}`);
            }
        } else if (result.functionResult) {
            this._consoleOutput.addLine('Function executed successfully');
            this._consoleOutput.addResult('Result', result.functionResult);
        } else {
            this._consoleOutput.addLine('Warning: No function configured');
        }

        this._consoleOutput.addLine('');

        if (!result.filterError && !result.functionError) {
            this._consoleOutput.addLine('=== All Tests Passed Successfully ===');
        } else {
            this._consoleOutput.addLine('=== Tests Failed ===');
        }
    }

    logTestError(error: any): void {
        this._consoleOutput.addLine('');
        this._consoleOutput.addLine('=== Test Execution Failed ===');
        this._consoleOutput.addLine(`Error: ${error?.title || 'Unknown error occurred'}`);
    }

    private _startProcessingAnimation(): void {
        this._clearProcessingAnimationTimeout();
        this._clearStatusPulseTimeout();
        this.statusPulse = null;
        this._pendingStatus = null;
        this.isProcessingAnimation = true;
        this._processingAnimationStart = Date.now();
    }

    private _stopProcessingAnimation(): void {
        const elapsed = Date.now() - this._processingAnimationStart;
        const remaining = this._processingAnimationMinMs - elapsed;

        if (remaining <= 0) {
            this.isProcessingAnimation = false;
            this._flushStatusPulse();
            return;
        }

        this._processingAnimationTimeout = window.setTimeout(() => {
            this.isProcessingAnimation = false;
            this._flushStatusPulse();
            this._processingAnimationTimeout = undefined;
        }, remaining);
    }

    private _clearProcessingAnimationTimeout(): void {
        if (this._processingAnimationTimeout) {
            clearTimeout(this._processingAnimationTimeout);
            this._processingAnimationTimeout = undefined;
        }
    }

    private _triggerStatusPulse(state: 'success' | 'error'): void {
        this._clearStatusPulseTimeout();
        this.statusPulse = state;

        this._statusPulseTimeout = window.setTimeout(() => {
            this.statusPulse = null;
            this._statusPulseTimeout = undefined;
        }, this._statusPulseMinMs);
    }

    private _clearStatusPulseTimeout(): void {
        if (this._statusPulseTimeout) {
            clearTimeout(this._statusPulseTimeout);
            this._statusPulseTimeout = undefined;
        }
    }

    private _queueStatusPulse(state: 'success' | 'error'): void {
        this._pendingStatus = state;

        if (!this.isProcessingAnimation) {
            this._flushStatusPulse();
        }
    }

    private _flushStatusPulse(): void {
        if (!this._pendingStatus) {
            return;
        }

        const state = this._pendingStatus;
        this._pendingStatus = null;
        this._triggerStatusPulse(state);
    }
}
