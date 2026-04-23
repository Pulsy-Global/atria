import { Component, Inject, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { FilterModalData, FilterModalResult } from './filter-modal.types';
import { FilterType } from '../../table/odata.types';
import { StringFilterModalComponent } from './components/string-filter/string-filter-modal.component';
import { NumberFilterModalComponent } from './components/number-filter/number-filter-modal.component';
import { EnumFilterModalComponent } from './components/enum-filter/enum-filter-modal.component';
import { TagFilterModalComponent } from './components/tag-filter/tag-filter-modal.component';

@Component({
    selector: 'filter-modal',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        StringFilterModalComponent,
        NumberFilterModalComponent,
        EnumFilterModalComponent,
        TagFilterModalComponent
    ],
    templateUrl: './filter-modal.component.html',
    styleUrls: ['./filter-modal.component.scss'],
    encapsulation: ViewEncapsulation.None,
})
export class FilterModalComponent {
    readonly FilterType = FilterType;

    constructor(
        public dialogRef: MatDialogRef<FilterModalComponent>,
        @Inject(MAT_DIALOG_DATA) public data: FilterModalData
    ) {}

    onResult(result: FilterModalResult | null): void {
        this.dialogRef.close(result);
    }
}