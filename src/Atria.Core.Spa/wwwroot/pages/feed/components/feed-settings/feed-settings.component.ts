import { CommonModule } from '@angular/common';
import {
    Component,
    EventEmitter,
    Input,
    Output,
    ViewEncapsulation,
} from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EnumOption } from 'shared/modals/filter/filter-modal.types';
import { Environment, Network, Tag } from '../../../../api/api.client';
import { TagSelectorComponent } from '../../../../shared/components/tag-selector/tag-selector.component';
import {
    FeedSourcePickerModalComponent,
    FeedSourcePickerModalData,
} from './modals/feed-source-picker-modal.component';
import {
    FeedStreamPickerModalComponent,
    FeedStreamPickerModalData,
} from './modals/feed-stream-picker-modal.component';

type ErrorHandlingOption = EnumOption & {
    description: string;
    disabled: boolean;
};

@Component({
    selector: 'feed-settings',
    standalone: true,
    templateUrl: './feed-settings.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatSelectModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
        MatRadioModule,
        MatTooltipModule,
        TagSelectorComponent,
    ],
})
export class FeedSettingsComponent {
    @Input({ required: true }) feedForm!: FormGroup;
    @Input() isLoading: boolean = false;
    @Input() networks: Network[] = [];
    @Input() environments: Environment[] = [];
    @Input() dataTypeOptions: EnumOption[] = [];
    @Input() errorHandlingOptions: ErrorHandlingOption[] = [];
    @Input() tags: Tag[] = [];
    @Input() selectedTagIds: string[] = [];

    @Output() regenerateName = new EventEmitter<void>();
    @Output() networkChange = new EventEmitter<string>();
    @Output() tagsChanged = new EventEmitter<string[]>();
    @Output() createTag = new EventEmitter<{ name: string; color: string }>();

    showTagsEditor = false;

    private _sourcePickerRef?: MatDialogRef<FeedSourcePickerModalComponent>;
    private _streamPickerRef?: MatDialogRef<FeedStreamPickerModalComponent>;

    constructor(private readonly _dialog: MatDialog) {}

    toggleTagsEditor(): void {
        this.showTagsEditor = !this.showTagsEditor;
    }

    openSourcePicker(): void {
        const data: FeedSourcePickerModalData = {
            feedForm: this.feedForm,
            networks: this.networks,
            environments: this.environments,
            dataTypeOptions: this.dataTypeOptions,
            onNetworkChange: (value: string) => this.onNetworkChange(value),
        };

        this._sourcePickerRef = this._dialog.open(
            FeedSourcePickerModalComponent,
            {
                width: '520px',
                maxWidth: '92vw',
                data,
            }
        );
    }

    openStreamPicker(): void {
        const data: FeedStreamPickerModalData = {
            feedForm: this.feedForm,
        };

        this._streamPickerRef = this._dialog.open(
            FeedStreamPickerModalComponent,
            {
                width: '640px',
                maxWidth: '92vw',
                data,
            }
        );
    }

    onRegenerateName(): void {
        this.regenerateName.emit();
    }

    onNetworkChange(value: string): void {
        this.networkChange.emit(value);
    }

    onTagsChanged(tagIds: string[]): void {
        this.tagsChanged.emit(tagIds);
    }

    onCreateTag(tagData: { name: string; color: string }): void {
        this.createTag.emit(tagData);
    }

    removeTag(tagId: string): void {
        this.tagsChanged.emit(this.selectedTagIds.filter((id) => id !== tagId));
    }

    hasNetworkSelection(): boolean {
        return !!this.feedForm.get('network')?.value;
    }

    hasEnvironmentSelection(): boolean {
        return !!this.feedForm.get('environment')?.value;
    }

    hasDatasetSelection(): boolean {
        return !!this.feedForm.get('dataset')?.value;
    }

    hasTagsSelection(): boolean {
        return this.getSelectedTags().length > 0;
    }

    getSelectedNetworkTitle(): string {
        return this.feedForm.get('network')?.value || 'Network';
    }

    getSelectedEnvironmentTitle(): string {
        const environmentId = this.feedForm.get('environment')?.value;
        const environment = this.environments.find(
            (item) => item.id === environmentId
        );
        return environment?.title || 'Environment';
    }

    getSelectedDatasetLabel(): string {
        const datasetValue = this.feedForm.get('dataset')?.value;
        const option = this.dataTypeOptions.find(
            (item) => item.value === datasetValue
        );
        return option?.label || 'Dataset';
    }

    getStreamStartSummary(): string {
        const startMode = this.feedForm.get('streamStart')?.value;
        if (startMode === 'startAtSpecificBlock') {
            const block = this.feedForm.get('specificBlockNumber')?.value;
            return block ? `Block ${block}` : 'Specific block';
        }
        return 'Latest block';
    }

    getStreamEndSummary(): string {
        const endMode = this.feedForm.get('streamEnd')?.value;
        if (endMode === 'endAtSpecificBlock') {
            const block = this.feedForm.get('endBlockNumber')?.value;
            return block ? `Block ${block}` : 'Specific block';
        }
        return 'Continuous';
    }

    getErrorHandlingLabel(): string {
        const strategy = this.feedForm.get('errorHandlingStrategy')?.value;
        const option = this.errorHandlingOptions.find(
            (item) => item.value === strategy
        );
        return option?.label || 'Strategy';
    }

    getSelectedTags(): Tag[] {
        const selectedIds = new Set(this.selectedTagIds);
        return this.tags.filter((tag) => tag.id && selectedIds.has(tag.id));
    }

    getTagColor(tagId: string): string {
        return this.tags.find((tag) => tag.id === tagId)?.color || '#FECDD3';
    }

    getBlockDelaySummary(): string {
        const numericValue = Number(this.feedForm.get('blockDelay')?.value);
        if (!Number.isFinite(numericValue) || numericValue <= 0) {
            return 'Realtime';
        }

        return `${numericValue} blocks`;
    }
}
