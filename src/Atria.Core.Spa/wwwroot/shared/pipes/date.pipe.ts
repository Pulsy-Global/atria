import { Pipe, PipeTransform } from '@angular/core';
import { STRING_EMPTY } from 'shared/core/constants/common.constants';

@Pipe({
    name: 'cultureAgnosticDate',
    standalone: true
})
export class CultureAgnosticDatePipe implements PipeTransform {
    
    transform(value: Date | string | null | undefined, format: 'short' | 'medium' | 'long' | 'full' | 'shortDate' | 'mediumDate' | 'longDate' | 'fullDate' | 'shortTime' | 'mediumTime' | 'longTime' | 'fullTime' = 'medium'): string {
        if (!value) {
            return 'Not specified';
        }

        const date = value instanceof Date ? value : new Date(value);
        
        if (isNaN(date.getTime())) {
            return STRING_EMPTY;
        }

        const options: Intl.DateTimeFormatOptions = this.getFormatOptions(format);
        
        return new Intl.DateTimeFormat(undefined, options).format(date);
    }

    private getFormatOptions(format: string): Intl.DateTimeFormatOptions {
        switch (format) {
            case 'short':
                return {
                    year: '2-digit',
                    month: 'numeric',
                    day: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit'
                };
            case 'medium':
                return {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    second: '2-digit'
                };
            case 'long':
                return {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    second: '2-digit',
                    timeZoneName: 'short'
                };
            case 'full':
                return {
                    weekday: 'long',
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    second: '2-digit',
                    timeZoneName: 'short'
                };
            case 'shortDate':
                return {
                    year: '2-digit',
                    month: 'numeric',
                    day: 'numeric'
                };
            case 'mediumDate':
                return {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric'
                };
            case 'longDate':
                return {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                };
            case 'fullDate':
                return {
                    weekday: 'long',
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                };
            case 'shortTime':
                return {
                    hour: 'numeric',
                    minute: '2-digit'
                };
            case 'mediumTime':
                return {
                    hour: 'numeric',
                    minute: '2-digit',
                    second: '2-digit'
                };
            case 'longTime':
                return {
                    hour: 'numeric',
                    minute: '2-digit',
                    second: '2-digit',
                    timeZoneName: 'short'
                };
            case 'fullTime':
                return {
                    hour: 'numeric',
                    minute: '2-digit',
                    second: '2-digit',
                    timeZoneName: 'long'
                };
            default:
                return {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    second: '2-digit'
                };
        }
    }
}