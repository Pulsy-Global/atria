import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Subject, takeUntil, filter, debounceTime } from 'rxjs';
import { OutputConfigMode } from './output-config.types';
import { isEqual } from 'lodash-es';

@Component({
    template: ''
})
export abstract class BaseOutputConfigComponent<T> implements OnInit, OnChanges, OnDestroy {
    @Input() config: T | null = null;
    @Input() mode: OutputConfigMode = OutputConfigMode.ReadOnly;
    @Output() configChange = new EventEmitter<T>();

    protected form!: FormGroup;
    protected destroy$ = new Subject<void>();
    protected isManipulatingArray = false;

    readonly OutputConfigMode = OutputConfigMode;

    constructor(protected cdr: ChangeDetectorRef) {}

    ngOnInit(): void {
        this.initializeForm();
        this.setupFormSubscription();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['config'] && this.form) {
            const current = changes['config'].currentValue;
            const previous = changes['config'].previousValue;
            
            if (!this.isFormDerivedChange(current, previous)) {
                this.updateFormWithConfig();
            }
        }
        
        if (changes['mode'] && this.form) {
            this.updateFormMode();
        }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    protected abstract createForm(): FormGroup;
    protected abstract updateFormWithConfig(): void;
    protected abstract buildConfigFromForm(): T;

    protected initializeForm(): void {
        this.form = this.createForm();
        this.updateFormWithConfig();
        this.updateFormMode();
    }

    protected setupFormSubscription(): void {
        this.form.valueChanges
            .pipe(
                takeUntil(this.destroy$),
                filter(() => 
                    this.shouldEmitConfig()
                )
            )
            .subscribe(() => {
                this.emitConfig();
            });
    }

    protected shouldEmitConfig(): boolean {
        return !this.isManipulatingArray;
    }

    protected updateFormMode(): void {
        if (this.mode === OutputConfigMode.ReadOnly) {
            this.form.disable();
        } else {
            this.form.enable();
        }
    }

    protected emitConfig(): void {
        const config = this.buildConfigFromForm();
        this.configChange.emit(config);
    }

    protected withArrayManipulation<T>(actionFromChild: () => T): T {
        this.isManipulatingArray = true;

        const result = actionFromChild();

        this.isManipulatingArray = false;
        this.emitConfig();

        return result;
    }

    private isFormDerivedChange(current: T, previous: T): boolean {
        const formValue = this.buildConfigFromForm();
        return isEqual(current, formValue);
    }

    isReadOnly(): boolean {
        return this.mode === OutputConfigMode.ReadOnly;
    }
}