import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { BaseOutputConfigComponent } from '../output-config.component';
import { EmailConfig } from '../output-config.types';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

type EmailType = 'to' | 'cc' | 'bcc';

@Component({
    selector: 'email-config',
    templateUrl: './email-config.component.html',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatCheckboxModule,
        MatButtonModule,
        MatIconModule,
        MatChipsModule
    ]
})
export class EmailConfigComponent extends BaseOutputConfigComponent<EmailConfig> {
    constructor(private fb: FormBuilder, cdr: ChangeDetectorRef) {
        super(cdr);
    }

    get toEmailsArray(): FormArray {
        return this.getEmailArray('to');
    }

    get ccEmailsArray(): FormArray {
        return this.getEmailArray('cc');
    }

    get bccEmailsArray(): FormArray {
        return this.getEmailArray('bcc');
    }

    addEmail(type: EmailType, email: string): void {
        const trimmedEmail = email.trim();

        if (!trimmedEmail || !this.isValidEmail(trimmedEmail)) 
            return;

        const emailArray = this.getEmailArray(type);
        const existingEmails = emailArray.value;
        
        if (!existingEmails.includes(trimmedEmail)) {
            this.withArrayManipulation(() => {
                emailArray.push(this.fb.control(trimmedEmail, [
                    Validators.required, 
                    Validators.email
                ]));
            });
        }
    }

    removeEmail(type: EmailType, index: number): void {
        this.withArrayManipulation(() => {
            this.getEmailArray(type).removeAt(index);
        });
    }

    protected createForm(): FormGroup {
        return this.fb.group({
            smtpServer: [STRING_EMPTY, [
                Validators.required
            ]],
            smtpPort: [587, [
                Validators.required, 
                Validators.min(1), 
                Validators.max(65535)]],
            username: [STRING_EMPTY, [
                Validators.required
            ]],
            password: [STRING_EMPTY, [
                Validators.required
            ]],
            enableSsl: [true],
            fromEmail: [STRING_EMPTY, [
                Validators.required, 
                Validators.email
            ]],
            fromName: [STRING_EMPTY],
            subject: [STRING_EMPTY, [
                Validators.required
            ]],
            bodyTemplate: [STRING_EMPTY, [
                Validators.required
            ]],
            isHtml: [true],
            timeoutSeconds: [30, [
                Validators.required, 
                Validators.min(1), 
                Validators.max(300)]],
            toEmails: this.fb.array([]),
            ccEmails: this.fb.array([]),
            bccEmails: this.fb.array([])
        });
    }

    protected updateFormWithConfig(): void {
        const config = this.config || this.getDefaultConfig();

        this.form.patchValue({
            smtpServer: config.smtpServer,
            smtpPort: config.smtpPort,
            username: config.username,
            password: config.password,
            enableSsl: config.enableSsl,
            fromEmail: config.fromEmail,
            fromName: config.fromName,
            subject: config.subject,
            bodyTemplate: config.bodyTemplate,
            isHtml: config.isHtml,
            timeoutSeconds: config.timeoutSeconds
        });

        this.form.setControl(
            'toEmails', 
            this.updateEmailsArray(config.toEmails));

        this.form.setControl(
            'ccEmails', 
            this.updateEmailsArray(config.ccEmails));

        this.form.setControl(
            'bccEmails', 
            this.updateEmailsArray(config.bccEmails));
    }

    protected buildConfigFromForm(): EmailConfig {
        const formValue = this.form.value;

        return {
            smtpServer: formValue.smtpServer,
            smtpPort: formValue.smtpPort,
            username: formValue.username,
            password: formValue.password,
            enableSsl: formValue.enableSsl,
            fromEmail: formValue.fromEmail,
            fromName: formValue.fromName,
            toEmails: this.toEmailsArray.value,
            ccEmails: this.ccEmailsArray.value,
            bccEmails: this.bccEmailsArray.value,
            subject: formValue.subject,
            bodyTemplate: formValue.bodyTemplate,
            isHtml: formValue.isHtml,
            timeoutSeconds: formValue.timeoutSeconds
        };
    }

    private getDefaultConfig(): EmailConfig {
        return {
            smtpServer: STRING_EMPTY,
            smtpPort: 587,
            username: STRING_EMPTY,
            password: STRING_EMPTY,
            enableSsl: true,
            fromEmail: STRING_EMPTY,
            fromName: STRING_EMPTY,
            toEmails: [],
            ccEmails: [],
            bccEmails: [],
            subject: STRING_EMPTY,
            bodyTemplate: STRING_EMPTY,
            isHtml: true,
            timeoutSeconds: 30
        };
    }

    private getEmailArray(type: EmailType): FormArray {
        return this.form.get(`${type}Emails`) as FormArray;
    }

    private updateEmailsArray(emails: string[]): FormArray {
        if (!emails) {
            return this.fb.array([]);
        }

        const controls = emails.map(email => 
            this.fb.control(email, [
                Validators.required, 
                Validators.email
            ])
        );
    
        return this.fb.array(controls);
    }

    private isValidEmail(email: string): boolean {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }
}