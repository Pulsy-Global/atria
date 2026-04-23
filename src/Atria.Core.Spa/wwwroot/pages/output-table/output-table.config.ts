import { ColumnConfig } from '../../shared/table/table.types';
import { FilterType } from '../../shared/table/odata.types';

export const OUTPUT_TABLE_CONFIG = {
    searchFields: ['name', 'description'],
    
    fields: [
        {
            key: 'name',
            label: 'Name',
            sortable: true,
            filterType: FilterType.String
        },
        {
            key: 'description',
            label: 'Description',
            sortable: true,
            filterType: FilterType.String
        },
        {
            key: 'type',
            label: 'Type',
            sortable: true,
            filterType: FilterType.Enum
        },
        {
            key: 'tagIds',
            label: 'Tags',
            sortable: false,
            filterType: FilterType.Tag
        },
        {
            key: 'createdAt',
            label: 'Created',
            sortable: true,
            filterType: FilterType.None
        },
        {
            key: 'controls',
            label: 'Actions',
            sortable: false,
            filterType: FilterType.None
        }
    ] as ColumnConfig[],

    getSearchFields: (): string[] => {
        return OUTPUT_TABLE_CONFIG.searchFields;
    },

    getField: (field: string): ColumnConfig | undefined => {
        return OUTPUT_TABLE_CONFIG.fields.find(col => col.key === field);
    },

    getFields: (): ColumnConfig[] => {
        return OUTPUT_TABLE_CONFIG.fields;
    },

    getFieldLabel: (field: string): string => {
        const tableColumn = OUTPUT_TABLE_CONFIG.getField(field);
        return tableColumn?.label || field;
    }
};