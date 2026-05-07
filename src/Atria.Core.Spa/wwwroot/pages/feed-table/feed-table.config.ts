import { ColumnConfig } from '../../shared/table/table.types';
import { FilterType } from '../../shared/table/odata.types';
import { FeedStatus } from '../../api/api.client';
import { EnumOption } from '../../shared/modals/filter/filter-modal.types';

export const FEED_TABLE_CONFIG = {
    searchFields: ['name', 'version'],

    fields: [
        {
            key: 'name',
            label: 'Name',
            sortable: true,
            filterType: FilterType.String
        },
        {
            key: 'version',
            label: 'Version',
            sortable: true,
            filterType: FilterType.String
        },
        {
            key: 'status',
            label: 'Status',
            sortable: true,
            filterType: FilterType.Enum
        },
        {
            key: 'networkId',
            label: 'Network',
            sortable: true,
            filterType: FilterType.Enum
        },
        {
            key: 'dataType',
            label: 'Data Type',
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
            key: 'controls',
            label: 'Actions',
            sortable: false,
            filterType: FilterType.None
        }
    ] as ColumnConfig[],

    getSearchFields: (): string[] => {
        return FEED_TABLE_CONFIG.searchFields;
    },

    getField: (field: string): ColumnConfig | undefined => {
        return FEED_TABLE_CONFIG.fields.find(col => col.key === field);
    },

    getFields: (): ColumnConfig[] => {
        return FEED_TABLE_CONFIG.fields;
    },

    getFieldLabel: (field: string): string => {
        const tableColumn = FEED_TABLE_CONFIG.getField(field);
        return tableColumn?.label || field;
    }
};

export const STATUS_CONFIG = {
    statuses: [
        { value: FeedStatus.Draft, label: 'Draft' },
        { value: FeedStatus.Pending, label: 'Pending' },
        { value: FeedStatus.Running, label: 'Running' },
        { value: FeedStatus.Paused, label: 'Paused' },
        { value: FeedStatus.Completed, label: 'Completed' },
        { value: FeedStatus.Error, label: 'Error' }
    ] as EnumOption[],

    statusColors: {
        [FeedStatus.Running]: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300',
        [FeedStatus.Paused]: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300',
        [FeedStatus.Error]: 'bg-rose-100 text-rose-800 dark:bg-rose-900/30 dark:text-rose-300',
        [FeedStatus.Completed]: 'bg-sky-100 text-sky-800 dark:bg-sky-900/30 dark:text-sky-300',
        [FeedStatus.Draft]: 'bg-neutral-100 text-neutral-800 dark:bg-neutral-800/30 dark:text-neutral-300',
        [FeedStatus.Pending]: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'
    } as { [key in FeedStatus]: string },

    getStatuses: (): EnumOption[] => {
        return STATUS_CONFIG.statuses;
    },

    getStatusLabel: (value: FeedStatus): string => {
        const option = STATUS_CONFIG.statuses.find(o => o.value === value);
        return option?.label || 'Unknown';
    },

    getStatusColor: (value: FeedStatus): string => {
        return STATUS_CONFIG.statusColors[value] || 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-300';
    }
};

export const BLOCK_CONFIG = {
    getStartBlockLabel: (value: string | undefined): string => {
        if (!value) return 'Latest';
        return `#${value}`;
    },

    getEndBlockLabel: (value: string | undefined): string => {
        if (!value) return 'Continuous';
        return `#${value}`;
    }
};
