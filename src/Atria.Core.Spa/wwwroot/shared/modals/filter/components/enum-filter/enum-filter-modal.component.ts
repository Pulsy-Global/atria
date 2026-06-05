import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { 
    FilterModalData, 
    FilterModalResult, 
    EnumOption,
    EnumFilterValue
} from '../../filter-modal.types';
import { NetworkIconComponent } from '../../../../components/network-display/network-icon.component';

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: 'enum-filter-modal',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        NetworkIconComponent,
        FormsModule
    ],
    templateUrl: './enum-filter-modal.component.html'
})
export class EnumFilterModalComponent implements OnInit {
    @Input() data!: FilterModalData;
    @Output() result = new EventEmitter<FilterModalResult | null>();

    selectedValues: EnumFilterValue['values'] = [];
    filteredOptions: EnumOption[] = [];
    hasOptionIcons = false;

    ngOnInit(): void {
        if (this.data.currentFilter) {
            const enumValue = this.data.currentFilter.value as EnumFilterValue;

            this.selectedValues = [...enumValue.values];
        }

        this.filteredOptions = [...(this.data.enumOptions || [])];
        this.hasOptionIcons = this.filteredOptions
            .some((option) => !!option.iconSource);
    }

    get canConfirm(): boolean {
        return this.selectedValues.length > 0;
    }

    filterOptions(event: Event): void {
        const value = (event.target as HTMLInputElement)
            .value.toLocaleLowerCase();

        this.filteredOptions = (this.data.enumOptions || []).filter(option =>
            option.label.toLowerCase().includes(value)
        );
    }

    isSelected(value: EnumOption['value']): boolean {
        return this.selectedValues.includes(value);
    }

    onSelectionChange(value: EnumOption['value'], checked: boolean): void {
        if (checked) {
            this.selectedValues.push(value);
        } else {
            this.selectedValues = this.selectedValues
                .filter(v => v !== value);
        }
    }

    onClear(): void {
        this.selectedValues = [];
    }

    onFilter(): void {
        if (!this.canConfirm) {
            return;
        }

        const enumValue: EnumFilterValue = {
            values: [...this.selectedValues]
        };

        const modalResult: FilterModalResult = {
            columnConfig: this.data.columnConfig,
            value: enumValue
        };

        this.result.emit(modalResult);
    }

    onCancel(): void {
        this.result.emit(null);
    }
}
