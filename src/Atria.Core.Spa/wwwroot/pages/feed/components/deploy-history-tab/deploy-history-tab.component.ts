import { Component, Input, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { Deploy, DeployStatus } from '../../../../api/api.client';
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
                return 'help';
        }
    }

    getStatusColor(status: DeployStatus | undefined): string {
        switch (status) {
            case DeployStatus.Deployed:
                return 'text-green-500';
            case DeployStatus.Failed:
                return 'text-red-500';
            case DeployStatus.Pending:
                return 'text-yellow-500';
            default:
                return 'text-gray-500';
        }
    }

    getStatusBorderColor(status: DeployStatus | undefined): string {
        switch (status) {
            case DeployStatus.Deployed:
                return 'border-green-500';
            case DeployStatus.Failed:
                return 'border-red-500';
            case DeployStatus.Pending:
                return 'border-yellow-500';
            default:
                return 'border-gray-500';
        }
    }
}