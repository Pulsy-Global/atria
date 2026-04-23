import { OutputType } from '../../api/api.client';
import { OutputTypeConfig } from './output-config.types';

export const OUTPUT_TYPE_CONFIG = {
    outputTypes: [
        { 
            value: OutputType.Webhook, 
            label: 'Webhook', 
            heroIcon: 'heroicons_outline:link',
            color: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300',
            visible: true,
            disabled: false
        },
        { 
            value: OutputType.Postgres, 
            label: 'PostgreSQL', 
            heroIcon: 'heroicons_outline:circle-stack',
            color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
            visible: true,
            disabled: true
        },
        { 
            value: OutputType.S3, 
            label: 'S3', 
            heroIcon: 'heroicons_outline:cloud-arrow-up',
            color: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300',
            visible: true,
            disabled: true
        },
        { 
            value: OutputType.Telegram, 
            label: 'Telegram', 
            heroIcon: 'heroicons_outline:chat-bubble-left-right',
            color: 'bg-sky-100 text-sky-800 dark:bg-sky-900/30 dark:text-sky-300',
            visible: false,
            disabled: true
        },
        { 
            value: OutputType.Discord, 
            label: 'Discord', 
            heroIcon: 'heroicons_outline:chat-bubble-left-ellipsis',
            color: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/30 dark:text-indigo-300',
            visible: false,
            disabled: true
        },
        { 
            value: OutputType.Email, 
            label: 'Email', 
            heroIcon: 'heroicons_outline:envelope',
            color: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300',
            visible: false,
            disabled: true
        }
    ] as OutputTypeConfig[],

    getTypes: (): OutputTypeConfig[] => {
        return OUTPUT_TYPE_CONFIG.outputTypes.filter(type => type.visible);
    },

    getAllTypes: (): OutputTypeConfig[] => {
        return OUTPUT_TYPE_CONFIG.outputTypes;
    },

    getTypeLabel: (type: OutputType): string => {
        return OUTPUT_TYPE_CONFIG.outputTypes.find(o => o.value === type)!.label;
    },

    getTypeIcon: (type: OutputType): string => {
        return OUTPUT_TYPE_CONFIG.outputTypes.find(o => o.value === type)!.heroIcon;
    },

    getTypeColor: (type: OutputType): string => {
        return OUTPUT_TYPE_CONFIG.outputTypes.find(o => o.value === type)!.color;
    }
};