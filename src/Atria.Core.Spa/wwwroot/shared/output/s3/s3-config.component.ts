import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { BaseOutputConfigComponent } from '../output-config.component';
import { S3Config } from '../output-config.types';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

@Component({
    selector: 's3-config',
    templateUrl: './s3-config.component.html',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatCheckboxModule,
        MatIconModule
    ]
})
export class S3ConfigComponent extends BaseOutputConfigComponent<S3Config> {

    constructor(private fb: FormBuilder, cdr: ChangeDetectorRef) {
        super(cdr);
    }

    protected createForm(): FormGroup {
        return this.fb.group({
            bucketName: [STRING_EMPTY, [
                Validators.required
            ]],
            region: [STRING_EMPTY, [
                Validators.required
            ]],
            accessKeyId: [STRING_EMPTY, [
                Validators.required
            ]],
            secretAccessKey: [STRING_EMPTY, [
                Validators.required
            ]],
            prefix: [STRING_EMPTY],
            fileFormat: ['json', [
                Validators.required
            ]],
            compressionEnabled: [false],
            timeoutSeconds: [30, [
                Validators.required, 
                Validators.min(1), 
                Validators.max(300)
            ]]
        });
    }

    protected updateFormWithConfig(): void {
        const config = this.config || this.getDefaultConfig();

        this.form.patchValue({
            bucketName: config.bucketName,
            region: config.region,
            accessKeyId: config.accessKeyId,
            secretAccessKey: config.secretAccessKey,
            prefix: config.prefix,
            fileFormat: config.fileFormat,
            compressionEnabled: config.compressionEnabled,
            timeoutSeconds: config.timeoutSeconds
        });
    }

    protected buildConfigFromForm(): S3Config {
        const formValue = this.form.value;
        return {
            bucketName: formValue.bucketName,
            region: formValue.region,
            accessKeyId: formValue.accessKeyId,
            secretAccessKey: formValue.secretAccessKey,
            prefix: formValue.prefix || STRING_EMPTY,
            fileFormat: formValue.fileFormat,
            compressionEnabled: formValue.compressionEnabled,
            timeoutSeconds: formValue.timeoutSeconds
        };
    }

    private getDefaultConfig(): S3Config {
        return {
            bucketName: STRING_EMPTY,
            region: STRING_EMPTY,
            accessKeyId: STRING_EMPTY,
            secretAccessKey: STRING_EMPTY,
            prefix: STRING_EMPTY,
            fileFormat: 'json',
            compressionEnabled: false,
            timeoutSeconds: 30
        };
    }
}