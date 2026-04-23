import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'filter',
    standalone: true,
    pure: false
})
export class FilterPipe implements PipeTransform {
    transform(items: any[], property: string, value: any): any[] {
        if (!items || !property) {
            return items;
        }
        
        return items.filter(item => item[property] === value);
    }
}