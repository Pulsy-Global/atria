import { STRING_EMPTY } from 'shared/core/constants/common.constants';
import { FilterType, FilterOperator } from '../../table/odata.types';
import { ColumnConfig } from '../../table/table.types';
import { Tag } from '../../../api/api.client';

export interface EnumOption {
    value: any;
    label: string;
}

export interface FilterModalResult {
    columnConfig: ColumnConfig;
    value: StringFilterValue | NumberFilterValue | EnumFilterValue | TagFilterValue | null;
}

export interface StringFilterValue {
    value: string;
    operator: FilterOperator;
}

export interface NumberFilterValue {
    from: number | null;
    to: number | null;
    fromOperator: FilterOperator;
    toOperator: FilterOperator;
}

export interface EnumFilterValue {
    values: any[];
}

export interface TagFilterValue {
    tagIds: string[];
}

export interface FilterModalData {
    columnConfig: ColumnConfig;
    currentFilter?: FilterElement;
    enumOptions?: EnumOption[];
    tagOptions?: Tag[];
}

export class FilterElement {
    field: string;
    type: FilterType;
    value: any;
    displayText: string;

    constructor(
        field: string, 
        type: FilterType, 
        value: any, 
        displayText: string = STRING_EMPTY
    ) {
        this.field = field;
        this.type = type;
        this.value = value;
        this.displayText = displayText;
    }

    static fromModalResult(modalResult: FilterModalResult): FilterElement {
        const displayText = FilterElement._createDisplayText(modalResult);
        
        return new FilterElement(
            modalResult.columnConfig.key,
            modalResult.columnConfig.filterType,
            modalResult.value,
            displayText
        );
    }

    private static _createDisplayText(modalResult: FilterModalResult): string {
        const columnLabel = modalResult.columnConfig.label;
        
        switch (modalResult.columnConfig.filterType) {
            case FilterType.String:
                const stringValue = modalResult.value as StringFilterValue;

                const operatorText = stringValue.operator === FilterOperator.Contains 
                    ? 'Contains' 
                    : 'Equals';

                return `${columnLabel} ${operatorText} '${stringValue.value}'`;

            case FilterType.Number:
                const numberValue = modalResult.value as NumberFilterValue;
                const parts: string[] = [];

                if (numberValue.from !== null) 
                    parts.push(`From: ${numberValue.from}`);

                if (numberValue.to !== null) 
                    parts.push(`To: ${numberValue.to}`);

                return `${columnLabel}: ${parts.join(', ')}`;

            case FilterType.Enum:
                const enumValue = modalResult.value as EnumFilterValue;
                return `${columnLabel}: ${enumValue.values.length} selected`;

            case FilterType.Tag:
                const tagValue = modalResult.value as TagFilterValue;
                return `${columnLabel}: ${tagValue.tagIds.length} selected`;

            default:
                return `${columnLabel}: filtered`;
        }
    }
}