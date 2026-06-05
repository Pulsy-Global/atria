import { AfterViewInit, Directive, ElementRef, Renderer2 } from '@angular/core';

@Directive({
    selector: '[atriaPagination]',
})
export class AtriaPaginationDirective implements AfterViewInit {
    constructor(
        private elementRef: ElementRef,
        private ren: Renderer2
    ) {}

    ngAfterViewInit(): void {
        this._styleDefaultPagination();
    }

    private _styleDefaultPagination() {
        const nativeElement = this.elementRef.nativeElement;

        const dropdownItemsPage = nativeElement.querySelector(
            '.mat-mdc-form-field-infix'
        );

        const actionsRangeLabel = nativeElement.querySelector(
            '.mat-mdc-paginator-range-label'
        );

        const dropdownText = nativeElement.querySelector(
            '.mat-mdc-text-field-wrapper'
        );

        const itemsPerPage = nativeElement.querySelector(
            '.mat-mdc-paginator-page-size'
        );

        const paginatorContainer = nativeElement.querySelector(
            '.mat-mdc-paginator-container'
        );

        const paginatorOuterContainer = nativeElement.querySelector(
            '.mat-mdc-paginator-outer-container'
        );

        const paginatorActions = nativeElement.querySelector(
            '.mat-mdc-paginator-range-actions'
        );

        this._setStyle(actionsRangeLabel, 'font-size', '13px');
        this._setStyle(actionsRangeLabel, 'font-weight', '500');
        this._setStyle(actionsRangeLabel, 'margin-right', '32px');

        this._setStyle(paginatorActions, 'margin', '8px');
        this._setStyle(paginatorContainer, 'padding-bottom', '0px');

        this._addClass(paginatorContainer, 'justify-center');
        this._addClass(paginatorContainer, 'sm:justify-between');
        this._addClass(paginatorOuterContainer, 'w-full');
        this._addClass(itemsPerPage, 'max-sm:hidden');

        this._setStyle(dropdownItemsPage, 'margin-top', '1px');
        this._setStyle(dropdownItemsPage, 'padding-top', '4px');
        this._setStyle(dropdownItemsPage, 'padding-bottom', '0px');

        this._setStyle(dropdownText, 'width', '60px');
        this._setStyle(dropdownText, 'height', '36px');
    }

    private _setStyle(
        element: Element | null,
        style: string,
        value: string
    ): void {
        if (!element) {
            return;
        }

        this.ren.setStyle(element, style, value);
    }

    private _addClass(element: Element | null, name: string): void {
        if (!element) {
            return;
        }

        this.ren.addClass(element, name);
    }
}
