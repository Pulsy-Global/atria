import { Component, OnInit, OnDestroy, ViewEncapsulation } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatRadioModule } from '@angular/material/radio';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseCardComponent } from 'fuse/components/card';
import { OutputService } from './output.service';
import { OutputState } from './output.state';
import { OutputRequest, OutputOperation } from './output.types';
import { NotificationService } from '../../shared/services/notification/notification.service';
import { TagSelectorComponent } from '../../shared/components/tag-selector/tag-selector.component';
import { Subject, takeUntil, timer } from 'rxjs';
import { STRING_EMPTY } from '../../shared/core/constants/common.constants';
import { CreateOutput, UpdateOutput, Output, OutputType, Tag } from '../../api/api.client';
import { OutputConfig, OutputConfigMode } from '../../shared/output/output-config.types';
import { OUTPUT_TYPE_CONFIG } from '../../shared/output/output-config.config';
import { WebhookConfigComponent } from '../../shared/output/webhook/webhook-config.component';
import { TelegramConfigComponent } from '../../shared/output/telegram/telegram-config.component';
import { PostgresConfigComponent } from '../../shared/output/postgres/postgres-config.component';
import { EmailConfigComponent } from '../../shared/output/email/email-config.component';
import { DiscordConfigComponent } from '../../shared/output/discord/discord-config.component';
import { S3ConfigComponent } from '../../shared/output/s3/s3-config.component';

@Component({
    selector: 'output',
    standalone: true,
    templateUrl: './output.component.html',
    styleUrls: ['./output.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        RouterModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatSelectModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
        MatCardModule,
        MatRadioModule,
        MatTooltipModule,
        MatProgressSpinnerModule,
        FuseCardComponent,
        WebhookConfigComponent,
        TelegramConfigComponent,
        PostgresConfigComponent,
        EmailConfigComponent,
        DiscordConfigComponent,
        S3ConfigComponent,
        TagSelectorComponent
    ]
})
export class OutputComponent implements OnInit, OnDestroy {

    private _unsubscribeAll: Subject<any> = new Subject<any>();

    outputForm: FormGroup;
    outputId: string | null = null;
    tags: Tag[] = [];
    selectedTagIds: string[] = [];

    outputTypeOptions = OUTPUT_TYPE_CONFIG.getTypes();

    currentConfig: OutputConfig | null = null;
    currentOutputData: CreateOutput | UpdateOutput;

    isSaving = false;
    isLoading = false;

    readonly OutputType = OutputType;
    readonly OutputConfigMode = OutputConfigMode;

    constructor(
        private readonly _activatedRoute: ActivatedRoute,
        private readonly _formBuilder: FormBuilder,
        private readonly _outputService: OutputService,
        private readonly _outputState: OutputState,
        private readonly _notificationService: NotificationService,
        private readonly _router: Router,
        public readonly location: Location,
    ) {}

    ngOnInit(): void {
        const resolverData = this._activatedRoute.snapshot
            .data['data'].outputId as string;

        this._applyOutputState(resolverData);
        this._initializeForm();
        this._setupSubscriptions();
        this._setupFormSubscriptions();

        if (this.isEditMode()) {
            this._loadOutputData(this.outputId!);
        }
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
        this._outputService.clearState();
        this._outputState.clearAllConfigs();
    }

    onNavigateBack(): void {
        this._router.navigate(['/outputs']);
    }

    onTypeChange(type: OutputType): void {
        this._updateConfig(
            this._outputState.getConfig(type));

        this.outputForm.patchValue({ type });
    }

    onConfigChange(config: OutputConfig): void {
        this._updateConfig(config);
        this._updateOutputDataModel();
    }

    onTagsChanged(tagIds: string[]): void {
        this.selectedTagIds = tagIds;
        this._updateOutputDataModel();
    }

    onCreateTag(tagData: { name: string; color: string }): void {
        this._outputService.createTag(tagData.name, tagData.color).subscribe({
            next: (tag: Tag) => {
                timer(500)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this.selectedTagIds = [...this.selectedTagIds, tag.id!];
                        this._updateOutputDataModel();
                    });
            },
            error: (error) => {
                this._notificationService.showErrorAlert(
                    'Failed to Create Tag',
                    error.title || 'Failed to create tag'
                );
            }
        });
    }

    isEditMode(): boolean {
        return !!this.outputId;
    }

    isSaveDisabled(): boolean {
        return !this.outputForm.valid || !this.currentConfig;
    }

    getTypeIcon(type: OutputType): string {
        return OUTPUT_TYPE_CONFIG.getTypeIcon(type);
    }

    getTypeLabel(type: OutputType): string {
        return OUTPUT_TYPE_CONFIG.getTypeLabel(type);
    }

    saveOutput(): void {
        const request: OutputRequest = {
            outputData: this.currentOutputData,
            outputId: this.outputId,
            operation: this.isEditMode()
                ? OutputOperation.Update
                : OutputOperation.Create
        };

        this.isSaving = true;

        this._outputService.saveOutput(request).subscribe({
            next: (output) => {
                timer(300)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this._handleSaveSuccess(output);
                    });
            },
            error: (error) => this._handleSaveError(error)
        });
    }

    private _loadOutputData(outputId: string): void {
        this.isLoading = true;

        this._outputService.getOutput(outputId).subscribe({
            next: () => {
                timer(300)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this.isLoading = false;
                    });
            },
            error: () => {
                this._notificationService.showErrorAlert(
                    'Error Loading Output',
                    'Failed to load output data'
                );

                this.isLoading = false;
            }
        });
    }

    private _handleSaveSuccess(output: Output): void {
        if (!this.isEditMode()) {
            this.location.replaceState(`/output/${output.id}`);
        }

        this._notificationService.showSuccessAlert(
            'Output Saved',
            `Output "${output.name}" has been saved successfully!`
        );

        this._applyOutputState(output.id);
        this._loadOutputData(output.id);

        this.isSaving = false;
    }

    private _handleSaveError(error: any): void {
        this._notificationService.showErrorAlert(
            'Save Failed',
            error.title || 'Failed to save output'
        );

        this.isSaving = false;
    }

    private _setupSubscriptions(): void {
        this._outputService.output$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((output: Output | null) => {
                if (output) {
                    this._initializeForm(output);
                    this._updateConfig(output.config);
                    this._setupFormSubscriptions();
                }
            });

        this._outputService.tags$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((tags: Tag[]) => {
                if (tags) {
                    this.tags = tags;
                }
            });
    }

    private _applyOutputState(outputId?: string | null): void {
        this.outputId = outputId;
    }

    private _updateConfig(config: unknown): void {
        this.currentConfig = config as any;

        const currentType = this.outputForm?.get('type')?.value;

        if (currentType && this.currentConfig) {
            this._outputState.saveConfig(currentType, this.currentConfig);
        }
    }

    private _initializeForm(outputData?: Output | null): void {
        this.outputForm = this._formBuilder.group({
            name: [outputData?.name || STRING_EMPTY, [
                Validators.required
            ]],
            description: [outputData?.description || STRING_EMPTY],
            type: [outputData?.type || OutputType.Webhook, [
                Validators.required
            ]]
        });

        if (outputData?.tagIds) {
            this.selectedTagIds = [...outputData.tagIds];
        }

        this._updateOutputDataModel();
    }

    private _setupFormSubscriptions(): void {
        this.outputForm.valueChanges
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(() => this._updateOutputDataModel());

        this.outputForm.get('type')?.valueChanges
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((type: OutputType) => {
                if (type !== this.outputForm.get('type')?.value) {
                    this.onTypeChange(type);
                }
            });
    }

    private _updateOutputDataModel(): void {
        const formValue = this.outputForm.value;

        this.currentOutputData = this.isEditMode()
            ? new UpdateOutput()
            : new CreateOutput();

        Object.assign(this.currentOutputData, {
            name: formValue.name || STRING_EMPTY,
            description: formValue.description || STRING_EMPTY,
            type: formValue.type,
            config: this.currentConfig,
            tagIds: this.selectedTagIds
        });
    }
}
