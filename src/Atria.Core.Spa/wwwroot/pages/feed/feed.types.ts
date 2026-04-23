export enum TabType {
    Settings = 'settings',
    Filter = 'filter',
    Function = 'function',
    Output = 'output',
    Result = 'result',
    DeployHistory = 'deployHistory'
}

export enum FeedOperation {
    Create = 'create',
    Update = 'update'
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
