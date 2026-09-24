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
    
    operators = [
        { value: FilterOperator.Contains, label: 'Contains' },
        { value: FilterOperator.Equals, label: 'Equals' }
    ];

    ngOnInit(): void {
        const allowed = this.data.columnConfig.stringOperators;

        if (allowed?.length) {
            this.operators = this.operators.filter(op => allowed.includes(op.value));
            this.operator = this.operators[0].value;
        }

        if (this.data.currentFilter) {
            const stringValue = this.data.currentFilter.value as StringFilterValue;

            this.filterValue = stringValue.value || STRING_EMPTY;

            if (this.operators.some(op => op.value === stringValue.operator)) {
                this.operator = stringValue.operator;
            }
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