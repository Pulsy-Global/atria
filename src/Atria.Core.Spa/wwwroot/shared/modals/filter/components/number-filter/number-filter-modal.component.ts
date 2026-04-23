import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { 
    FilterModalData, 
    FilterModalResult, 
    NumberFilterValue 
} from '../../filter-modal.types';
import { FilterOperator, FilterType } from '../../../../table/odata.types';

@Component({
    selector: 'number-filter-modal',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        FormsModule
    ],
    templateUrl: './number-filter-modal.component.html'
})
export class NumberFilterModalComponent {
    @Input() data!: FilterModalData;
    @Output() result = new EventEmitter<FilterModalResult | null>();

    fromValue: number | null = null;
    toValue: number | null = null;

    ngOnInit(): void {
        if (this.data.currentFilter) {
            const numberValue = this.data.currentFilter.value as NumberFilterValue;

            this.fromValue = numberValue.from || null;
            this.toValue = numberValue.to || null;
        }     
    }

    get canConfirm(): boolean {
        return this.fromValue !== null || this.toValue !== null;
    }

    onClear(): void {
        this.fromValue = null;
        this.toValue = null;
    }

    onFilter(): void {
        if (!this.canConfirm) {
            return;
        }

        const numberValue: NumberFilterValue = {
            from: this.fromValue,
            fromOperator: FilterOperator.GreaterOrEqual,
            to: this.toValue,
            toOperator: FilterOperator.LessOrEqual
        };

        const modalResult: FilterModalResult = {
            columnConfig: this.data.columnConfig,
            value: numberValue
        };

        this.result.emit(modalResult);
    }

    onCancel(): void {
        this.result.emit(null);
    }
}