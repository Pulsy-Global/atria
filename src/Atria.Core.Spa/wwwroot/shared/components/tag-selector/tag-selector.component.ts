import { Component, Input, Output, EventEmitter, OnInit, OnChanges, ViewEncapsulation, SimpleChanges, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocomplete, MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { Observable, combineLatest } from 'rxjs';
import { map, startWith } from 'rxjs/operators';
import { Tag } from '../../../api/api.client';
import { STRING_EMPTY } from '../../core/constants/common.constants';
import { TagColorPickerModalComponent } from './tag-color-picker-modal/tag-color-picker-modal.component';

@Component({
    selector: 'tag-selector',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatAutocompleteModule,
        MatIconModule,
        MatInputModule,
        MatTooltipModule
    ],
    encapsulation: ViewEncapsulation.None,
    templateUrl: './tag-selector.component.html',
    styleUrls: ['./tag-selector.component.scss']
})
export class TagSelectorComponent implements OnInit, OnChanges {
    @Input() availableTags: Tag[] = [];
    @Input() selectedTagIds: string[] = [];
    @Input() showLabel = true;

    @Output() tagsChanged = new EventEmitter<string[]>();
    @Output() createTag = new EventEmitter<{ name: string; color: string }>();

    @ViewChild('auto', { read: MatAutocomplete }) autocomplete!: MatAutocomplete;
    @ViewChild('auto', { read: ElementRef }) autocompleteElement!: ElementRef;

    searchControl = new FormControl<string | null>(STRING_EMPTY);
    filteredTags$!: Observable<Tag[]>;
    tagNameMaxWidth: string = '300px';

    constructor(private readonly dialog: MatDialog) {}

    onAutocompleteOpened(): void {
        setTimeout(() => {
            this._calculateTagNameMaxWidth();
        }, 0);
    }

    ngOnInit(): void {
        this._initializeFilteredTags();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['availableTags'] || changes['selectedTagIds']) {
            if (this.searchControl) {
                this.searchControl.setValue(this.searchControl.value);
            }
        }
    }

    get selectedTags(): Tag[] {
        return this.availableTags.filter(
            tag => this.selectedTagIds.includes(tag.id!));
    }

    get searchValue(): string {
        const value = this.searchControl.value;

        if (typeof value === 'string') {
            return value.trim();
        }

        return STRING_EMPTY;
    }

    get canCreateTag(): boolean {
        return !!this.searchValue && !this.tagExists();
    }

    selectTag(tag: Tag): void {
        if (!this.selectedTagIds.includes(tag.id!)) {
            this.tagsChanged.emit([
                ...this.selectedTagIds, tag.id
            ]);
        }

        this.searchControl.setValue(STRING_EMPTY);
    }

    removeTag(tagId: string): void {
        this.tagsChanged.emit(this.selectedTagIds.filter(id => id !== tagId));
    }

    openCreateTagModal(): void {
        if (!this.canCreateTag) return;

        const dialogRef = this.dialog.open(TagColorPickerModalComponent, {
            width: '400px',
            data: { tagName: this.searchValue },
            autoFocus: false,
            restoreFocus: false
        });

        dialogRef.afterClosed().subscribe(result => {
            this.createTag.emit({
                name: result.name,
                color: result.color
            });

            this.searchControl.setValue(STRING_EMPTY);
        });
    }

    getTagColor(tagId: string): string {
        return (
            this.availableTags.find((t) => t.id === tagId)?.color || '#FECDD3'
        );
    }

    private _calculateTagNameMaxWidth(): void {
        const panel = this.autocomplete?.panel?.nativeElement as HTMLElement;
        const element = this.autocompleteElement?.nativeElement as HTMLElement;

        const panelElement = panel || element;

        if (!panelElement || panelElement.offsetWidth === 0)
            return;

        this.tagNameMaxWidth = `${panelElement.offsetWidth - 64}px`;
    }

    private _initializeFilteredTags(): void {
        this.filteredTags$ = this.searchControl.valueChanges.pipe(
            startWith(STRING_EMPTY),
            map(() => this._filterTags())
        );
    }

    private _filterTags(): Tag[] {
        const filterValue = this.searchValue.toLowerCase();

        const available = this.availableTags.filter(
            tag => !this.selectedTagIds.includes(tag.id!));

        if (!filterValue) return available;

        return available.filter(
            tag => tag.name!.toLowerCase().includes(filterValue));
    }

    private tagExists(): boolean {
        return this.availableTags.some(
            tag => tag.name!.toLowerCase() === this.searchValue.toLowerCase());
    }
}
