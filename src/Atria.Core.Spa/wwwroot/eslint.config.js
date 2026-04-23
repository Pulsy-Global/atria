import js from '@eslint/js';
import tseslint from '@typescript-eslint/eslint-plugin';
import tsparser from '@typescript-eslint/parser';
import angular from '@angular-eslint/eslint-plugin';
import angularTemplate from '@angular-eslint/eslint-plugin-template';
import angularTemplateParser from '@angular-eslint/template-parser';
import prettier from 'eslint-config-prettier';

export default [
    {
        ignores: [
            'projects/**/*',
            'misc/**/*',
            'node_modules/**/*',
            '.angular/**/*',
            'public/**/*',
            '*.tmp',
            '*.temp'
        ]
    },
    
    // TypeScript files
    {
        files: ['**/*.ts'],
        languageOptions: {
            parser: tsparser,
            parserOptions: {
                ecmaVersion: 2022,
                sourceType: 'module',
                project: './tsconfig.json'
            }
        },
        plugins: {
            '@typescript-eslint': tseslint,
            '@angular-eslint': angular
        },
        rules: {
            ...js.configs.recommended.rules,
            ...tseslint.configs.recommended.rules,
            ...angular.configs.recommended.rules,
            '@angular-eslint/directive-selector': [
                'error',
                {
                    type: 'attribute',
                    prefix: 'app',
                    style: 'camelCase'
                }
            ],
            '@angular-eslint/component-selector': [
                'error',
                {
                    type: 'element',
                    prefix: 'app',
                    style: 'kebab-case'
                }
            ],
            '@typescript-eslint/no-unused-vars': [
                'error',
                {
                    argsIgnorePattern: '^_'
                }
            ],
            '@typescript-eslint/explicit-function-return-type': 'warn',
            '@typescript-eslint/no-explicit-any': 'warn',
            '@typescript-eslint/no-empty-function': 'warn',
            '@typescript-eslint/naming-convention': 'off',
            'prefer-const': 'error',
            'no-var': 'error'
        }
    },

    // HTML template files
    {
        files: ['**/*.html'],
        languageOptions: {
            parser: angularTemplateParser
        },
        plugins: {
            '@angular-eslint/template': angularTemplate
        },
        rules: {
            ...angularTemplate.configs.recommended.rules,
            ...angularTemplate.configs.accessibility.rules,
            '@angular-eslint/template/click-events-have-key-events': 'warn',
            '@angular-eslint/template/mouse-events-have-key-events': 'warn'
        }
    },

    // Prettier
    prettier
];