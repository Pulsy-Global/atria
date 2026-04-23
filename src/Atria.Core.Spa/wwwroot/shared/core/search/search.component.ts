import {Component, ViewEncapsulation, Output, EventEmitter, ViewChild, ElementRef, Input, OnInit, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { STRING_EMPTY } from '../constants/common.constants';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

@Component({
    selector: 'search',
    standalone: true,
    templateUrl: './search.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        ReactiveFormsModule,
        MatIconModule
    ]
})
export class SearchBarComponent implements OnInit, OnDestroy {
    @Input() searchTerm: string = STRING_EMPTY;
    @Output() searchBarFilterEvent: EventEmitter<string>;
    @ViewChild('inputField') inputElement!: ElementRef;

    prevValue: string = STRING_EMPTY;
    private _unsubscribeAll: Subject<void> = new Subject<void>();

    form = new FormGroup({
        searchBarText: new FormControl<string>(STRING_EMPTY),
    });

    constructor() {
        this.searchBarFilterEvent = new EventEmitter<string>();
    }

    ngOnInit(): void {
        if (this.searchTerm) {
            this.setSearchBarText(this.searchTerm);
            this.prevValue = this.searchTerm;
        }

        this.searchBarText.valueChanges
            .pipe(
                debounceTime(300),
                distinctUntilChanged(),
                takeUntil(this._unsubscribeAll)
            )
            .subscribe((value: string | null) => {
                const currentValue = value?.trim() || STRING_EMPTY;

                if (currentValue !== this.prevValue) {
                    this.prevValue = currentValue;
                    this.searchBarFilterEvent.emit(currentValue);
                }
            });
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }

    get searchBarText(): FormControl<string> {
        return this.form.controls.searchBarText;
    }

    public setSearchBarText(newValue: string) {
        this.searchBarText.setValue(newValue);
    }

    public searchBarFilterChanged() {
        const currentValue = this.searchBarText.value?.trim() || STRING_EMPTY;

        if (currentValue !== this.prevValue)
        {
            this.prevValue = currentValue;
            this.searchBarFilterEvent.emit(currentValue);
        }
    }

    public clearSearchBarText() {
        this.setSearchBarText(STRING_EMPTY);
    }

    onEnter() {
        this.searchBarFilterChanged();
        this.inputElement.nativeElement.blur();
    }
}
