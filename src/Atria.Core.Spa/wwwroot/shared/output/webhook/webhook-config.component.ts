import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BaseOutputConfigComponent } from '../output-config.component';
import { WebhookConfig, WebhookHttpMethod } from '../output-config.types';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

@Component({
    selector: 'webhook-config',
    templateUrl: './webhook-config.component.html',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatButtonModule,
        MatIconModule
    ]
})
export class WebhookConfigComponent extends BaseOutputConfigComponent<WebhookConfig> {
    readonly httpMethods = [
        { value: WebhookHttpMethod.Post, label: 'POST' },
        { value: WebhookHttpMethod.Put, label: 'PUT' },
    ];

    constructor(private fb: FormBuilder, cdr: ChangeDetectorRef) {
        super(cdr);
    }

    get headersArray(): FormArray {
        return this.form.get('headers') as FormArray;
    }

    addHeader(): void {
        this.withArrayManipulation(() => {
            const headerGroup = this.fb.group({
                key: [STRING_EMPTY, [Validators.required]], 
                value: [STRING_EMPTY, [Validators.required]]
            });
            this.headersArray.push(headerGroup);
        });
    }

    removeHeader(index: number): void {
        this.withArrayManipulation(() => {
            this.headersArray.removeAt(index);
        });
    }

    protected createForm(): FormGroup {
        return this.fb.group({
            url: [STRING_EMPTY, [
                Validators.required
            ]],
            method: [WebhookHttpMethod.Post, [
                Validators.required
            ]],
            timeoutSeconds: [30, [
                Validators.required,
                Validators.min(1),
                Validators.max(45)
            ]],
            headers: this.fb.array([])
        });
    }

    protected updateFormWithConfig(): void {
        const config = this.config || this.getDefaultConfig();

        this.form.patchValue({
            url: config.url,
            method: config.method,
            timeoutSeconds: config.timeoutSeconds
        });

        this.form.setControl(
            'headers', 
            this.updateHeadersArray(config.headers));
    }

    protected buildConfigFromForm(): WebhookConfig {
        const formValue = this.form.value;

        return {
            url: formValue.url,
            method: formValue.method,
            timeoutSeconds: formValue.timeoutSeconds,
            headers: this.buildHeadersObject(
                this.headersArray),
        };
    }

    private getDefaultConfig(): WebhookConfig {
        return {
            url: STRING_EMPTY,
            method: WebhookHttpMethod.Post,
            timeoutSeconds: 30,
            headers: {}
        };
    }

    private updateHeadersArray(headers: { [key: string]: string }): FormArray {
        if (!headers || Object.keys(headers).length === 0) {
            return this.fb.array([]);
        }

        const controls = Object.entries(headers).map(([key, value]) => 
            this.fb.group({ 
                key: [key, [Validators.required]], 
                value: [value, [Validators.required]] 
            })
        );

        return this.fb.array(controls);
    }

    private buildHeadersObject(headersArray: FormArray): { [key: string]: string } {
        const headers: { [key: string]: string } = {};

        headersArray.controls.forEach(control => {
            const key = control.get('key')?.value?.trim();
            const value = control.get('value')?.value?.trim();

            if (key && value) {
                headers[key] = value;
            }
        });

        return headers;
    }
}