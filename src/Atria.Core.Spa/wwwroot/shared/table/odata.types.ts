export enum FilterType {
    None,
    Boolean,
    String,
    Number,
    Date,
    Enum,
    Tag
}

export enum FilterOperator {
    Equals = 'eq',
    Contains = 'contains',
    Less = 'lt',
    Greater = 'gt',
    LessOrEqual = 'le',
    GreaterOrEqual = 'ge',
    NotEquals = 'ne'
}

export enum SortDirection {
    Asc = "asc",
    Desc = "desc"
}

export interface PaginationParams {
    skip: number;
    top: number;
}

export interface QueryParams {
    search?: string;
    filter?: string;
    orderby?: string;
    skip?: number;
    top?: number;
}