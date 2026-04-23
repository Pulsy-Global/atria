import { Component, Inject, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';

export interface FeedStreamPickerModalData {
    feedForm: FormGroup;
}

@Component({
    selector: 'feed-stream-picker-modal',
    standalone: true,
    templateUrl: './feed-stream-picker-modal.component.html',
    styleUrls: ['./feed-stream-picker-modal.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatDialogModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatRadioModule,
        MatTooltipModule,
        MatIconModule
    ]
})
export class FeedStreamPickerModalComponent {
    constructor(
        public dialogRef: MatDialogRef<FeedStreamPickerModalComponent>,
        @Inject(MAT_DIALOG_DATA) public data: FeedStreamPickerModalData
    ) {}

    onClose(): void {
        this.dialogRef.close();
    }
}
