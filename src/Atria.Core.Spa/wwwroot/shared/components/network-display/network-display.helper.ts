import {
    NetworkDisplayIconSource,
    resolveNetworkIconSource,
} from './network-display-icons';

export interface NetworkDisplayEnvironment {
    id?: string;
    title?: string;
}

export interface NetworkDisplayNetwork {
    title?: string;
    iconUrl?: string;
    environments?: NetworkDisplayEnvironment[];
}

export interface NetworkDisplayInfo {
    networkTitle: string;
    environmentTitle: string;
    iconSource?: NetworkDisplayIconSource;
    isKnown: boolean;
}

export interface NetworkOption {
    value: string;
    label: string;
    iconSource?: NetworkDisplayIconSource;
}

export function getNetworkDisplayInfo(
    networks: readonly NetworkDisplayNetwork[] | null | undefined,
    networkId: string | null | undefined
): NetworkDisplayInfo {
    if (!networks || !networkId) {
        return getUnknownNetworkDisplayInfo();
    }

    for (const network of networks) {
        const environment = network.environments?.find(
            (env) => env.id === networkId
        );

        if (environment) {
            return {
                networkTitle: network.title || 'Unknown',
                environmentTitle: environment.title || 'Unknown',
                iconSource: resolveNetworkIconSource(network.iconUrl),
                isKnown: true,
            };
        }
    }

    return getUnknownNetworkDisplayInfo();
}

export function getNetworkOptions(
    networks: readonly NetworkDisplayNetwork[] | null | undefined
): NetworkOption[] {
    if (!networks) {
        return [];
    }

    return networks.flatMap((network) => {
        const iconSource = resolveNetworkIconSource(network.iconUrl);

        return (network.environments || [])
            .filter((environment) => !!environment.id)
            .map((environment) => ({
                value: environment.id!,
                label: `${network.title || 'Unknown'} - ${environment.title || 'Unknown'}`,
                iconSource,
            }));
    });
}

function getUnknownNetworkDisplayInfo(): NetworkDisplayInfo {
    return {
        networkTitle: 'Unknown',
        environmentTitle: 'Unknown',
        iconSource: undefined,
        isKnown: false,
    };
}
