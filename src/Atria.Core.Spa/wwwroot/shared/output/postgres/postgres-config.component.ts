import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BaseOutputConfigComponent } from '../output-config.component';
import { PostgresConfig } from '../output-config.types';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

@Component({
    selector: 'postgres-config',
    templateUrl: './postgres-config.component.html',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatCheckboxModule,
        MatButtonModule,
        MatIconModule
    ]
})
export class PostgresConfigComponent extends BaseOutputConfigComponent<PostgresConfig> {
    constructor(private fb: FormBuilder, cdr: ChangeDetectorRef) {
        super(cdr);
    }

    get columnMappingsArray(): FormArray {
        return this.form.get('columnMappings') as FormArray;
    }

    addColumnMapping(): void {
        this.withArrayManipulation(() => {
            const mappingGroup = this.fb.group({
                key: [STRING_EMPTY, [Validators.required]], 
                value: [STRING_EMPTY, [Validators.required]]
            });
            this.columnMappingsArray.push(mappingGroup);
        });
    }

    removeColumnMapping(index: number): void {
        this.withArrayManipulation(() => {
            this.columnMappingsArray.removeAt(index);
        });
    }

    protected createForm(): FormGroup {
        return this.fb.group({
            connectionString: [STRING_EMPTY, [
                Validators.required
            ]],
            tableName: [STRING_EMPTY, [
                Validators.required
            ]],
            schema: ['public', [
                Validators.required
            ]],
            createTableIfNotExists: [true],
            batchSize: [1000, [
                Validators.required, 
                Validators.min(1), 
                Validators.max(10000)
            ]],
            timeoutSeconds: [30, [
                Validators.required, 
                Validators.min(1), 
                Validators.max(300)
            ]],
            columnMappings: this.fb.array([])
        });
    }

    protected updateFormWithConfig(): void {
        const config = this.config || this.getDefaultConfig();

        this.form.patchValue({
            connectionString: config.connectionString,
            tableName: config.tableName,
            schema: config.schema,
            createTableIfNotExists: config.createTableIfNotExists,
            batchSize: config.batchSize,
            timeoutSeconds: config.timeoutSeconds
        });

        this.form.setControl(
            'columnMappings', 
            this.updateColumnMappingsArray(config.columnMappings));
    }

    protected buildConfigFromForm(): PostgresConfig {
        const formValue = this.form.value;

        return {
            connectionString: formValue.connectionString,
            tableName: formValue.tableName,
            schema: formValue.schema,
            createTableIfNotExists: formValue.createTableIfNotExists,
            batchSize: formValue.batchSize,
            timeoutSeconds: formValue.timeoutSeconds,
            columnMappings: this.buildColumnMappingsObject(
                this.columnMappingsArray),
        };
    }

    private getDefaultConfig(): PostgresConfig {
        return {
            connectionString: STRING_EMPTY,
            tableName: STRING_EMPTY,
            schema: 'public',
            columnMappings: {},
            createTableIfNotExists: true,
            batchSize: 1000,
            timeoutSeconds: 30
        };
    }

    private updateColumnMappingsArray(mappings: { [key: string]: string }): FormArray {
        if (!mappings || Object.keys(mappings).length === 0) {
            return this.fb.array([]);
        }

        const controls = Object.entries(mappings).map(([key, value]) => 
            this.fb.group({ 
                key: [key, [Validators.required]], 
                value: [value, [Validators.required]] 
            })
        );

        return this.fb.array(controls);
    }

    private buildColumnMappingsObject(columnMappingsArray: FormArray): { [key: string]: string } {
        const mappings: { [key: string]: string } = {};
        
        columnMappingsArray.controls.forEach(control => {
            const key = control.get('key')?.value?.trim();
            const value = control.get('value')?.value?.trim();

            if (key && value) {
                mappings[key] = value;
            }
        });

        return mappings;
    }
}