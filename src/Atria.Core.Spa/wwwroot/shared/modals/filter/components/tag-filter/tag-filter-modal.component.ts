import { Component, Input, Output, ViewEncapsulation, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';
import { 
    FilterModalData, 
    FilterModalResult, 
    TagFilterValue
} from '../../filter-modal.types';
import { STRING_EMPTY } from '../../../../core/constants/common.constants';
import { Tag } from '../../../../../api/api.client';

@Component({
    selector: 'tag-filter-modal',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatChipsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatTooltipModule,
        FormsModule
    ],
    encapsulation: ViewEncapsulation.None,
    templateUrl: './tag-filter-modal.component.html',
    styleUrls: ['./tag-filter-modal.component.scss']
})
export class TagFilterModalComponent implements OnInit {
    @Input() data!: FilterModalData;
    @Output() result = new EventEmitter<FilterModalResult | null>();

    selectedTags: string[] = [];
    filteredTags: Tag[] = [];
    searchTerm = STRING_EMPTY;

    ngOnInit(): void {
        if (this.data.currentFilter) {
            const tagValue = this.data.currentFilter.value as TagFilterValue;
            this.selectedTags = [...tagValue.tagIds];
        }

        this.filteredTags = [...(this.data.tagOptions || [])];
    }

    get canConfirm(): boolean {
        return this.selectedTags.length > 0;
    }

    filterTags(): void {
        const searchLower = this.searchTerm.toLowerCase();
        
        this.filteredTags = (this.data.tagOptions || []).filter(tag =>
            tag.name!.toLowerCase().includes(searchLower)
        );
    }

    isSelected(tagId: string): boolean {
        return this.selectedTags.includes(tagId);
    }

    toggleTag(tagId: string): void {
        const index = this.selectedTags.indexOf(tagId);
        
        if (index > -1) {
            this.selectedTags.splice(index, 1);
        } else {
            this.selectedTags.push(tagId);
        }
    }

    removeTag(tagId: string): void {
        const index = this.selectedTags.indexOf(tagId);
        if (index > -1) {
            this.selectedTags.splice(index, 1);
        }
    }

    getTag(tagId: string): Tag | undefined {
        return this.data.tagOptions?.find(t => t.id === tagId);
    }

    getTagLabel(tagId: string): string {
        const tag = this.getTag(tagId);
        return tag?.name || STRING_EMPTY;
    }

    getTagColor(tagId: string): string {
        const tag = this.getTag(tagId);
        return tag?.color || '#3B82F6';
    }

    onClear(): void {
        this.selectedTags = [];
    }

    onFilter(): void {
        if (!this.canConfirm) {
            return;
        }

        const tagValue: TagFilterValue = {
            tagIds: [...this.selectedTags]
        };

        const modalResult: FilterModalResult = {
            columnConfig: this.data.columnConfig,
            value: tagValue
        };

        this.result.emit(modalResult);
    }

    onCancel(): void {
        this.result.emit(null);
    }
}