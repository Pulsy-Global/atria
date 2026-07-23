export enum TabType {
    Settings = 'settings',
    Filter = 'filter',
    Function = 'function',
    Output = 'output',
    Result = 'result',
    DeployHistory = 'deployHistory',
    Metrics = 'metrics',
}

const DRAFT_HIDDEN_TABS = new Set([TabType.Metrics, TabType.Result]);

export function isFeedTabAvailable(
    tabType: TabType,
    isDraft: boolean
): boolean {
    return !isDraft || !DRAFT_HIDDEN_TABS.has(tabType);
}

export enum FeedOperation {
    Create = 'create',
    Update = 'update',
}

export interface TabConfig {
    label: string;
    type: TabType;
    closable: boolean;
    requiresConfirmation: boolean;
    confirmationMessage?: string;
    icon: string;
}

export interface DeployFeedRequest {
    deployFeed: any;
    deployFeedId?: string;
    filterFile?: File;
    functionFile?: File;
    operation: FeedOperation;
}
