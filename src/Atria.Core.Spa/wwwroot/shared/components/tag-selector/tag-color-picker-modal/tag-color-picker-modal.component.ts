import { Component, Inject, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

interface TagColor {
    name: string;
    value: string;
}

export interface TagColorPickerData {
    tagName: string;
}

@Component({
    selector: 'tag-color-picker-modal',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule
    ],
    templateUrl: './tag-color-picker-modal.component.html',
    styleUrls: ['./tag-color-picker-modal.component.scss'],
    encapsulation: ViewEncapsulation.None
})
export class TagColorPickerModalComponent {
    readonly PRESET_COLORS: TagColor[] = [
        { name: 'Red', value: '#FECDD3' },
        { name: 'Orange', value: '#FED7AA' },
        { name: 'Yellow', value: '#FDE68A' },
        { name: 'Green', value: '#A7F3D0' },
        { name: 'Blue', value: '#BAE6FD' },
        { name: 'Gray', value: '#E2E8F0' },
    ];

    selectedColor = this.PRESET_COLORS[0].value;

    constructor(
        private readonly dialogRef: MatDialogRef<TagColorPickerModalComponent>,
        @Inject(MAT_DIALOG_DATA) public data: TagColorPickerData
    ) {}

    selectColor(color: string): void {
        this.selectedColor = color;
    }

    onCreate(): void {
        this.dialogRef.close({
            color: this.selectedColor,
            name: this.data.tagName
        });
    }

    onCancel(): void {
        this.dialogRef.close();
    }
}
