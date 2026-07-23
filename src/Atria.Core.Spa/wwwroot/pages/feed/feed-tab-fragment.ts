import { TabType } from './feed.types';

const TAB_FRAGMENT_BY_TYPE: Record<TabType, string> = {
    [TabType.Settings]: 'settings',
    [TabType.Filter]: 'filter',
    [TabType.Function]: 'function',
    [TabType.Output]: 'output',
    [TabType.Result]: 'live-preview',
    [TabType.DeployHistory]: 'deploy-history',
    [TabType.Metrics]: 'metrics',
};

const TAB_TYPE_BY_FRAGMENT = new Map<string, TabType>([
    ...Object.entries(TAB_FRAGMENT_BY_TYPE).map(
        ([type, fragment]) => [fragment, type as TabType] as const
    ),
    ['result', TabType.Result],
    ['deployhistory', TabType.DeployHistory],
]);

export function getFeedTabFragment(tabType: TabType): string {
    return TAB_FRAGMENT_BY_TYPE[tabType];
}

export function getFeedTabTypeFromFragment(
    fragment: string | null
): TabType | null {
    if (!fragment) {
        return null;
    }

    return TAB_TYPE_BY_FRAGMENT.get(fragment.trim().toLowerCase()) ?? null;
}
