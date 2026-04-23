import { ErrorHandlingStrategy } from '../../api/api.client';
import { EnumOption } from '../modals/filter/filter-modal.types';

export const ERROR_HANDLING_CONFIG = {
    strategies: [
        { value: ErrorHandlingStrategy.StopOnError, label: 'Stop on Error', description: 'Stop processing if block data is missing', disabled: false },
        { value: ErrorHandlingStrategy.ContinueOnError, label: 'Continue on Error', description: 'Skip missing blocks and continue processing', disabled: false },
    ] as Array<EnumOption & { description: string; disabled: boolean }>,

    strategyColors: {
        [ErrorHandlingStrategy.StopOnError]: 'bg-rose-100 text-rose-800 dark:bg-rose-900/30 dark:text-rose-300',
        [ErrorHandlingStrategy.ContinueOnError]: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300',
    } as { [key in ErrorHandlingStrategy]: string },

    getStrategies: (): Array<EnumOption & { description: string; disabled: boolean }> => {
        return ERROR_HANDLING_CONFIG.strategies;
    },

    getStrategyLabel: (strategy: ErrorHandlingStrategy): string => {
        return ERROR_HANDLING_CONFIG.strategies.find(o => o.value === strategy)!.label;
    },

    getStrategyColor: (strategy: ErrorHandlingStrategy): string => {
        return ERROR_HANDLING_CONFIG.strategyColors[strategy];
    },

    getStrategyDescription: (strategy: ErrorHandlingStrategy): string => {
        return ERROR_HANDLING_CONFIG.strategies.find(o => o.value === strategy)!.description;
    }
};