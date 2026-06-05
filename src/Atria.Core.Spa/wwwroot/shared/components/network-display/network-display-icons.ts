export interface NetworkDisplayIconSource {
    url: string;
}

export function resolveNetworkIconSource(
    icon: string | undefined
): NetworkDisplayIconSource | undefined {
    const url = icon?.trim();

    if (!url) {
        return undefined;
    }

    return {
        url,
    };
}
