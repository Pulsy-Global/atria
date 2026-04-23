import { CommonModule } from '@angular/common';
import { Component, Inject, ViewEncapsulation } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
    MAT_DIALOG_DATA,
    MatDialogModule,
    MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EnumOption } from 'shared/modals/filter/filter-modal.types';
import { Environment, Network } from '../../../../../api/api.client';
import { DATA_TYPE_CONFIG } from '../../../../../shared/config/data-type.config';

export interface FeedSourcePickerModalData {
    feedForm: FormGroup;
    networks: Network[];
    environments: Environment[];
    dataTypeOptions: EnumOption[];
    onNetworkChange?: (value: string) => void;
}

@Component({
    selector: 'feed-source-picker-modal',
    standalone: true,
    templateUrl: './feed-source-picker-modal.component.html',
    styleUrls: ['./feed-source-picker-modal.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatDialogModule,
        MatButtonModule,
        MatFormFieldModule,
        MatSelectModule,
        MatIconModule,
        MatTooltipModule,
    ],
})
export class FeedSourcePickerModalComponent {
    constructor(
        public dialogRef: MatDialogRef<FeedSourcePickerModalComponent>,
        @Inject(MAT_DIALOG_DATA) public data: FeedSourcePickerModalData
    ) {}

    onClose(): void {
        this.dialogRef.close();
    }

    onNetworkChange(value: string): void {
        const selectedNetwork = this.data.networks.find(
            (n) => n.title === value
        );
        this.data.environments = selectedNetwork?.environments || [];
        this.data.onNetworkChange?.(value);

        if (this.data.environments.length > 0) {
            const firstEnvironment = this.data.environments[0];
            this.data.feedForm.patchValue({ environment: firstEnvironment.id });
            this.updateDataTypeOptions(firstEnvironment);
        }
    }

    onEnvironmentChange(environmentId: string): void {
        const selectedEnvironment = this.data.environments.find(
            (e) => e.id === environmentId
        );
        if (selectedEnvironment) {
            this.updateDataTypeOptions(selectedEnvironment);
        }
    }

    private updateDataTypeOptions(environment: Environment): void {
        this.data.dataTypeOptions = DATA_TYPE_CONFIG.getDataTypes(environment.availableDatasets);

        const currentDataset = this.data.feedForm.get('dataset')?.value;
        const isAvailable = this.data.dataTypeOptions.some(
            (opt) => opt.value === currentDataset
        );

        if (!isAvailable && this.data.dataTypeOptions.length > 0) {
            this.data.feedForm.patchValue({
                dataset: this.data.dataTypeOptions[0].value,
            });
        }
    }
}
