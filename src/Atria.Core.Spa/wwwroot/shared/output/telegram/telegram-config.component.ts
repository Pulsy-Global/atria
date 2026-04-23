import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { BaseOutputConfigComponent } from '../output-config.component';
import { TelegramConfig } from '../output-config.types';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

@Component({
    selector: 'telegram-config',
    templateUrl: './telegram-config.component.html',
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
export class TelegramConfigComponent extends BaseOutputConfigComponent<TelegramConfig> {

    constructor(private fb: FormBuilder, cdr: ChangeDetectorRef) {
        super(cdr);
    }

    protected createForm(): FormGroup {
        return this.fb.group({
            botToken: [STRING_EMPTY, [
                Validators.required
            ]],
            chatId: [STRING_EMPTY, [
                Validators.required
            ]],
            messageTemplate: [STRING_EMPTY, [
                Validators.required
            ]],
            enableMarkdown: [true],
            disableWebPagePreview: [false],
            disableNotification: [false],
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
            botToken: config.botToken,
            chatId: config.chatId,
            messageTemplate: config.messageTemplate,
            enableMarkdown: config.enableMarkdown,
            disableWebPagePreview: config.disableWebPagePreview,
            disableNotification: config.disableNotification,
            timeoutSeconds: config.timeoutSeconds
        });
    }

    protected buildConfigFromForm(): TelegramConfig {
        const formValue = this.form.value;

        return {
            botToken: formValue.botToken,
            chatId: formValue.chatId,
            messageTemplate: formValue.messageTemplate,
            enableMarkdown: formValue.enableMarkdown,
            disableWebPagePreview: formValue.disableWebPagePreview,
            disableNotification: formValue.disableNotification,
            timeoutSeconds: formValue.timeoutSeconds
        };
    }

    private getDefaultConfig(): TelegramConfig {
        return {
            botToken: STRING_EMPTY,
            chatId: STRING_EMPTY,
            messageTemplate: STRING_EMPTY,
            enableMarkdown: true,
            disableWebPagePreview: false,
            disableNotification: false,
            timeoutSeconds: 30
        };
    }
}