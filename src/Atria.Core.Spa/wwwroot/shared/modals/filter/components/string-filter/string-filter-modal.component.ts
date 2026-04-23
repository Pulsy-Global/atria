import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { 
    FilterModalData, 
    FilterModalResult, 
    StringFilterValue 
} from '../../filter-modal.types';
import { FilterOperator, FilterType } from '../../../../table/odata.types';
import { STRING_EMPTY } from '../../../../core/constants/common.constants';

@Component({
    selector: 'string-filter-modal',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatSelectModule,
        FormsModule
    ],
    templateUrl: './string-filter-modal.component.html'
})
export class StringFilterModalComponent {
    @Input() data!: FilterModalData;
    @Output() result = new EventEmitter<FilterModalResult | null>();

    filterValue = STRING_EMPTY;
    operator = FilterOperator.Contains;
    
    readonly operators = [
        { value: FilterOperator.Contains, label: 'Contains' },
        { value: FilterOperator.Equals, label: 'Equals' }
    ];

    ngOnInit(): void {
        if (this.data.currentFilter) {
            const stringValue = this.data.currentFilter.value as StringFilterValue;

            this.filterValue = stringValue.value || STRING_EMPTY;
            this.operator = stringValue.operator || FilterOperator.Contains;
        }
    }

    get canConfirm(): boolean {
        return this.filterValue.trim() !== STRING_EMPTY;
    }

    onClear(): void {
        this.filterValue = STRING_EMPTY;
    }

    onFilter(): void {
        if (!this.canConfirm) {
            return;
        }

        const stringValue: StringFilterValue = {
            value: this.filterValue.trim(),
            operator: this.operator
        };

        const modalResult: FilterModalResult = {
            columnConfig: this.data.columnConfig,
            value: stringValue
        };

        this.result.emit(modalResult);
    }

    onCancel(): void {
        this.result.emit(null);
    }
}