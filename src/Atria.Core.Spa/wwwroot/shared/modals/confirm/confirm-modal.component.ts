import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmModalData {
    title?: string;
    message: string;
    confirmText?: string;
    cancelText?: string;
    type?: 'warning' | 'danger' | 'info';
}

@Component({
    selector: 'confirm-modal',
    standalone: true,
    templateUrl: './confirm-modal.component.html',
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule
    ]
})
export class ConfirmModalComponent {
    
    constructor(
        public dialogRef: MatDialogRef<ConfirmModalComponent>,
        @Inject(MAT_DIALOG_DATA) public data: ConfirmModalData
    ) {}

    onConfirm(): void {
        this.dialogRef.close(true);
    }

    onCancel(): void {
        this.dialogRef.close(false);
    }

    getIcon(): string {
        switch (this.data.type) {
            case 'warning':
                return 'warning';
            case 'danger':
                return 'error';
            case 'info':
            default:
                return 'help';
        }
    }

    getButtonColor(): string {
        switch (this.data.type) {
            case 'danger':
                return 'warn';
            case 'warning':
                return 'accent';
            case 'info':
            default:
                return 'primary';
        }
    }
}