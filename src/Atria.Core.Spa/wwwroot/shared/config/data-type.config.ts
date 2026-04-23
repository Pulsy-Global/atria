import { AtriaDataType } from '../../api/api.client';
import { EnumOption } from '../modals/filter/filter-modal.types';

export const DATA_TYPE_CONFIG = {
    dataTypes: [
        { value: AtriaDataType.BlockWithTransactions, label: 'Block with Transactions' },
        { value: AtriaDataType.BlockWithLogs, label: 'Block with Logs' },
        { value: AtriaDataType.BlockWithTraces, label: 'Block with Traces' },
    ] as EnumOption[],

    dataTypeColors: {
        [AtriaDataType.BlockWithTransactions]: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/30 dark:text-indigo-300',
        [AtriaDataType.BlockWithLogs]: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300',
        [AtriaDataType.BlockWithTraces]: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-900/30 dark:text-cyan-300',
    } as { [key in AtriaDataType]: string },

    getDataTypes: (availableDatasets?: AtriaDataType[]): EnumOption[] => {
        if (!availableDatasets || availableDatasets.length === 0) {
            return DATA_TYPE_CONFIG.dataTypes;
        }
        return DATA_TYPE_CONFIG.dataTypes.filter(dt =>
            availableDatasets.includes(dt.value as AtriaDataType)
        );
    },

    getDataTypeLabel: (value: AtriaDataType): string => {
        const option = DATA_TYPE_CONFIG.dataTypes.find(o => o.value === value);
        return option?.label || 'Unknown';
    },

    getDataTypeColor: (value: AtriaDataType): string => {
        return DATA_TYPE_CONFIG.dataTypeColors[value] || 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-300';
    },
};