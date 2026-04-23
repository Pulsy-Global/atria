import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { BaseOutputConfigComponent } from '../output-config.component';
import { DiscordConfig } from '../output-config.types';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

@Component({
    selector: 'discord-config',
    templateUrl: './discord-config.component.html',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatCheckboxModule,
        MatIconModule
    ]
})
export class DiscordConfigComponent extends BaseOutputConfigComponent<DiscordConfig> {

    constructor(private fb: FormBuilder, cdr: ChangeDetectorRef) {
        super(cdr);
    }

    protected createForm(): FormGroup {
        return this.fb.group({
            webhookUrl: [STRING_EMPTY, [
                Validators.required
            ]],
            username: [STRING_EMPTY],
            avatarUrl: [STRING_EMPTY],
            message: [STRING_EMPTY, [
                Validators.required
            ]],
            enableTts: [false],
            timeoutSeconds: [30, [
                Validators.required, 
                Validators.min(1), 
                Validators.max(300)]]
        });
    }

    protected updateFormWithConfig(): void {
        const config = this.config || this.getDefaultConfig();

        this.form.patchValue({
            webhookUrl: config.webhookUrl,
            username: config.username,
            avatarUrl: config.avatarUrl,
            message: config.message,
            enableTts: config.enableTts,
            timeoutSeconds: config.timeoutSeconds
        });
    }

    protected buildConfigFromForm(): DiscordConfig {
        const formValue = this.form.value;
        return {
            webhookUrl: formValue.webhookUrl,
            username: formValue.username || undefined,
            avatarUrl: formValue.avatarUrl || undefined,
            message: formValue.message,
            enableTts: formValue.enableTts,
            timeoutSeconds: formValue.timeoutSeconds
        };
    }

    private getDefaultConfig(): DiscordConfig {
        return {
            webhookUrl: STRING_EMPTY,
            username: STRING_EMPTY,
            avatarUrl: STRING_EMPTY,
            message: STRING_EMPTY,
            enableTts: false,
            timeoutSeconds: 30
        };
    }
}