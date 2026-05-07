import { Component, Input, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { Deploy, DeployStatus } from '../../../../api/api.client';
import { getFeedErrorDisplayInfo, type FeedErrorDisplayInfo } from '../../../../shared/config/feed-error-display.config';
import { FilterPipe } from '../../../../shared/pipes/filter.pipe';
import { CultureAgnosticDatePipe } from '../../../../shared/pipes/date.pipe';

@Component({
    selector: 'deploy-history-tab',
    standalone: true,
    templateUrl: './deploy-history-tab.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule,
        MatButtonModule,
        MatCardModule,
        MatChipsModule,
        FilterPipe,
        CultureAgnosticDatePipe
    ]
})
export class DeployHistoryTabComponent {
    
    @Input() deployHistory: Deploy[] = [];

    readonly DeployStatus = DeployStatus;

    getStatusIcon(status: DeployStatus | undefined): string {
        switch (status) {
            case DeployStatus.Deployed:
                return 'check_circle';
            case DeployStatus.Failed:
                return 'error';
            case DeployStatus.Pending:
                return 'schedule';
            default:
                return 'history';
        }
    }

    getStatusLabel(status: DeployStatus | undefined): string {
        switch (status) {
            case DeployStatus.Deployed:
                return 'Active';
            case DeployStatus.Failed:
                return 'Failed';
            case DeployStatus.Pending:
                return 'Pending';
            default:
                return 'Inactive';
        }
    }

    getStatusBadgeClasses(status: DeployStatus | undefined): string {
        switch (status) {
            case DeployStatus.Deployed:
                return 'border-green-200 bg-green-50 text-green-700 dark:border-green-400/40 dark:bg-green-500/10 dark:text-green-300';
            case DeployStatus.Failed:
                return 'border-red-200 bg-red-50 text-red-700 dark:border-red-400/40 dark:bg-red-500/10 dark:text-red-300';
            case DeployStatus.Pending:
                return 'border-yellow-200 bg-yellow-50 text-yellow-700 dark:border-yellow-400/40 dark:bg-yellow-500/10 dark:text-yellow-300';
            default:
                return 'border-neutral-200 bg-neutral-100 text-neutral-600 dark:border-neutral-500/30 dark:bg-neutral-700/30 dark:text-neutral-300';
        }
    }

    getStatusIconClasses(status: DeployStatus | undefined): string {
        switch (status) {
            case DeployStatus.Deployed:
                return 'text-green-600 dark:text-green-300';
            case DeployStatus.Failed:
                return 'text-red-600 dark:text-red-300';
            case DeployStatus.Pending:
                return 'text-yellow-600 dark:text-yellow-300';
            default:
                return 'text-neutral-500 dark:text-neutral-300';
        }
    }

    getDeployErrorDisplayInfo(deploy: Deploy): FeedErrorDisplayInfo | null {
        return getFeedErrorDisplayInfo(deploy.errorInfo);
    }

    getDeployDetailsTitle(deploy: Deploy): string {
        const errorDisplayInfo = this.getDeployErrorDisplayInfo(deploy);

        if (errorDisplayInfo) {
            return errorDisplayInfo.title;
        }

        switch (deploy.status) {
            case DeployStatus.Deployed:
                return 'Runtime confirmed';
            case DeployStatus.Pending:
                return 'Waiting for runtime';
            case DeployStatus.Failed:
                return 'Deployment failed';
            default:
                return 'No longer active';
        }
    }

    getDeployDetailsMessage(deploy: Deploy): string | null {
        const errorDisplayInfo = this.getDeployErrorDisplayInfo(deploy);

        if (errorDisplayInfo) {
            return errorDisplayInfo.message;
        }

        switch (deploy.status) {
            case DeployStatus.Deployed:
                return 'This deployment is currently active.';
            case DeployStatus.Pending:
                return 'The runtime has not confirmed this deployment yet.';
            case DeployStatus.Failed:
                return null;
            default:
                return 'A newer deployment or stop action replaced this deployment.';
        }
    }
}
