import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class FeedValidators {
    
    static version(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            const value = control.value;
            
            if (!value) {
                return null;
            }
            
            // major.minor.patch (1.0.0, 2.1.3, 1.0.0-alpha)
            const semanticVersionPattern = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$/;
            
            // v1.0, 1.0, 2.1 
            const simpleVersionPattern = /^v?(0|[1-9]\d*)\.(0|[1-9]\d*)(?:\.(0|[1-9]\d*))?$/;
            
            const isValidSemantic = semanticVersionPattern.test(value);
            const isValidSimple = simpleVersionPattern.test(value);
            
            if (!isValidSemantic && !isValidSimple) {
                return {
                    invalidVersion: {
                        actualValue: value,
                        message: 'Version must follow semantic versioning (e.g., 1.0.0, 2.1.3-alpha) or simple format (e.g., 1.0, v2.1)',
                        examples: ['1.0.0', '2.1.3', '1.0.0-alpha', '1.0', 'v2.1']
                    }
                };
            }
            
            return null;
        };
    }

    static blockNumber(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            const value = control.value;
            
            if (!value) {
                return null;
            }
            
            const numValue = parseInt(value, 10);
            
            if (isNaN(numValue) || numValue < 0) {
                return {
                    invalidBlockNumber: {
                        actualValue: value,
                        message: 'Block number must be a positive integer'
                    }
                };
            }
            
            return null;
        };
    }

    static blockRange(startBlockControlName: string, endBlockControlName: string): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            const formGroup = control.parent;
            
            if (!formGroup) {
                return null;
            }
            
            const startBlockControl = formGroup.get(startBlockControlName);
            const endBlockControl = formGroup.get(endBlockControlName);
            
            if (!startBlockControl || !endBlockControl) {
                return null;
            }
            
            const startBlock = parseInt(startBlockControl.value, 10);
            const endBlock = parseInt(endBlockControl.value, 10);
            
            if (isNaN(startBlock) || isNaN(endBlock)) {
                return null;
            }
            
            if (endBlock <= startBlock) {
                return {
                    invalidBlockRange: {
                        startBlock,
                        endBlock,
                        message: 'End block must be greater than start block'
                    }
                };
            }
            
            return null;
        };
    }
}