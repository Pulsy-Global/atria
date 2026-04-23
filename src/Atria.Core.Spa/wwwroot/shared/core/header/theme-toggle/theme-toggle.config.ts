import { ThemeOption } from './theme-toggle.types';
import { Scheme } from 'fuse/services/config';
import { THEME_STORAGE_KEY } from 'shared/core/constants/common.constants';

export const THEME_CONFIG = {
    options: {
        light: {
            scheme: 'light',
            icon: 'light_mode',
            label: 'Light'
        },
        dark: {
            scheme: 'dark',
            icon: 'dark_mode',
            label: 'Dark'
        },
        auto: {
            scheme: 'auto',
            icon: 'brightness_auto',
            label: 'Auto'
        }
    } as Record<Scheme, ThemeOption>,

    getThemeOptions: (): ThemeOption[] => {
        return Object.values(THEME_CONFIG.options);
    },

    getThemeOption: (scheme: Scheme): ThemeOption => {
        return THEME_CONFIG.options[scheme];
    },

    getAvailableOptions: (currentScheme: Scheme): ThemeOption[] => {
        return Object.values(THEME_CONFIG.options)
            .filter(option => option.scheme !== currentScheme);
    },

    getThemeIcon: (scheme: Scheme): string => {
        return THEME_CONFIG.options[scheme]?.icon || THEME_CONFIG.options.auto.icon;
    },

    getThemeLabel: (scheme: Scheme): string => {
        return THEME_CONFIG.options[scheme]?.label || 'Unknown';
    },

    saveToStorage: (scheme: Scheme): void => {
        localStorage.setItem(THEME_STORAGE_KEY, scheme);
    },

    loadFromStorage: (): Scheme | null => {
        const saved = localStorage.getItem(THEME_STORAGE_KEY);

        return (saved === 'auto' || 
                saved === 'light' || 
                saved === 'dark') ? saved : null;
    }
};