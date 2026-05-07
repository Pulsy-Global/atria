import type { FeedErrorInfo } from '../../api/api.client';

export interface FeedErrorDisplayInfo {
    title: string;
    message: string;
}

export function getFeedErrorDisplayInfo(errorInfo: FeedErrorInfo | null | undefined): FeedErrorDisplayInfo | null {
    if (!errorInfo?.code) {
        return null;
    }

    switch (errorInfo.code) {
        case 'DeploymentFailed':
            return {
                title: 'Deployment failed',
                message: 'The feed could not be deployed.',
            };
        case 'RuntimeUnavailable':
            return {
                title: 'No runtime available',
                message: 'The feed stopped because no runtime became available for this deployment.',
            };
        case 'WebhookUnavailable':
            return {
                title: 'Webhook unavailable',
                message: 'The feed stopped because the webhook endpoint could not be reached.',
            };
        case 'ProcessingFailed':
            return {
                title: 'Processing failed',
                message: 'The feed stopped after repeated processing errors.',
            };
        case 'BlockDataUnavailable':
            return {
                title: 'Block data unavailable',
                message: 'The feed stopped because block data was unavailable for multiple consecutive blocks.',
            };
        case 'OperationFailed':
            return {
                title: 'Operation failed',
                message: 'The feed stopped because the requested operation could not be completed.',
            };
        default:
            return {
                title: 'Feed error',
                message: 'The feed stopped because of an unexpected error.',
            };
    }
}
