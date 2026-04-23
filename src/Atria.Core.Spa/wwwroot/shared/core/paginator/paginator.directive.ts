import {
    ElementRef,
    AfterViewInit,
    Directive,
    Renderer2
  } from "@angular/core";

@Directive({
    selector: '[atriaPagination]'
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

        const actionButtons = nativeElement.querySelectorAll(
            'button.mat-mdc-tooltip-trigger'
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

        /*
        actionButtons.forEach(function (button, i) 
        {
            if (i == 0 || i == 1) {
                this.ren.setStyle(button, 'margin-left', '5px');
            }
            if (i == 2 || i == 3) {
                this.ren.setStyle(button, 'margin-right', '5px');
            }
        });
        */
       
        this.ren.setStyle(actionsRangeLabel, 'font-size', '13px');
        this.ren.setStyle(actionsRangeLabel, 'font-weight', '500');
        this.ren.setStyle(actionsRangeLabel, 'margin-right', '32px');

        this.ren.setStyle(paginatorActions, 'margin', '8px');
        this.ren.setStyle(paginatorContainer, 'padding-bottom', '0px');

        this.ren.addClass(paginatorContainer, 'justify-center');
        this.ren.addClass(paginatorContainer, 'sm:justify-between');
        this.ren.addClass(paginatorOuterContainer, 'w-full');
        this.ren.addClass(itemsPerPage, 'max-sm:hidden');

        this.ren.setStyle(dropdownItemsPage, 'margin-top', '1px');
        this.ren.setStyle(dropdownItemsPage, 'padding-top', '4px');
        this.ren.setStyle(dropdownItemsPage, 'padding-bottom', '0px');
        
        this.ren.setStyle(dropdownText, 'width', '60px');
        this.ren.setStyle(dropdownText, 'height', '36px');
    }
}