import { Injectable } from '@angular/core';
import { QueryParams, FilterType, FilterOperator } from '../odata.types';
import { FilterElement, StringFilterValue, NumberFilterValue, EnumFilterValue, TagFilterValue } from '../../modals/filter/filter-modal.types';
import { Sort } from '@angular/material/sort';
import { PaginationState } from '../table.types';

@Injectable()
export class TableODataService {

    buildQuery(
        filters: FilterElement[],
        searchTerm: string,
        searchFields: string[],
        sort: Sort | null,
        pagination: PaginationState
    ): QueryParams {
        const query: QueryParams = {};

        if (searchTerm.trim()) {
            query.search = searchTerm.trim();
        }

        const filterQuery = this._buildFilterQuery(filters);
        const orderQuery = this._buildOrderQuery(sort);

        if (filterQuery) {
            query.filter = filterQuery;
        }

        if (orderQuery) {
            query.orderby = orderQuery;
        }

        query.skip = pagination.pageIndex * pagination.pageSize || 0;
        query.top = pagination.pageSize;

        return query;
    }

    private _buildFilterQuery(
        filters: FilterElement[]
    ): string | undefined {
        if (filters.length === 0) {
            return undefined;
        }

        const regularFilters = filters
            .map(filter => this._buildSingleFilter(filter))
            .filter(filter => filter !== null);

        return regularFilters.length > 0 ? regularFilters.join(' and ') : undefined;
    }

    private _buildSingleFilter(filter: FilterElement): string | null {
        switch (filter.type) {
            case FilterType.String:
                const stringValue = filter.value as StringFilterValue;

                if (stringValue.operator === FilterOperator.Contains) {
                    return `contains(${filter.field}, '${stringValue.value}')`;
                } else {
                    return `${filter.field} ${stringValue.operator} '${stringValue.value}'`;
                }

            case FilterType.Number:
                const numberValue = filter.value as NumberFilterValue;
                const numberParts: string[] = [];

                if (numberValue.from !== null) {
                    numberParts.push(`${filter.field} ${numberValue.fromOperator} '${numberValue.from}'`);
                }

                if (numberValue.to !== null) {
                    numberParts.push(`${filter.field} ${numberValue.toOperator} '${numberValue.to}'`);
                }

                return numberParts.length > 1
                    ? `(${numberParts.join(' and ')})`
                    : numberParts[0];

            case FilterType.Enum:
                const enumValue = filter.value as EnumFilterValue;

                const enumParts = enumValue.values.map(value => {
                    const formattedValue = typeof value === 'string'
                        ? `'${value}'`
                        : value;

                    return `${filter.field} eq ${formattedValue}`;
                });

                return `(${enumParts.join(' or ')})`;

            case FilterType.Tag:
                const tagValue = filter.value as TagFilterValue;

                const tagParts = tagValue.tagIds.map(tagId =>
                    `${filter.field}/any(t: t eq ${tagId})`
                );

                return `(${tagParts.join(' or ')})`;

            default:
                return null;
        }
    }

    private _buildOrderQuery(sort: Sort | null): string | null {
        return sort
            ? `${sort.active} ${sort.direction}`
            : null;
    }
}
