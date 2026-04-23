import { CommonModule } from '@angular/common';
import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
    Output,
    SimpleChanges,
    ViewEncapsulation,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FuseConfigService } from 'fuse/services/config';
import { MonacoEditorModule } from 'ngx-monaco-editor-v2';
import { Subject, takeUntil } from 'rxjs';
import { AtriaDataType } from '../../../../api/api.client';

@Component({
    selector: 'code-editor-tab',
    standalone: true,
    templateUrl: './code-editor-tab.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        FormsModule,
        MatButtonModule,
        MatIconModule,
        MatTooltipModule,
        MonacoEditorModule,
    ],
})
export class CodeEditorTabComponent implements OnInit, OnChanges, OnDestroy {
    @Input() title: string = '';
    @Input() description: string = '';
    @Input() templates: Record<AtriaDataType, string> = {} as Record<AtriaDataType, string>;
    @Input() initialCode: string = '';
    @Input() dataType: AtriaDataType = AtriaDataType.BlockWithTransactions;

    @Output() codeChanged = new EventEmitter<string>();

    private _unsubscribeAll: Subject<void> = new Subject<void>();

    editorOptions = {
        theme: 'vs-dark',
        language: 'javascript',
        automaticLayout: true,
        minimap: { enabled: false },
        fontSize: 14,
        lineNumbers: 'on',
        scrollBeyondLastLine: false,
        wordWrap: 'on',
        scrollbar: { useShadows: false, verticalScrollbarSize: 8, horizontalScrollbarSize: 8, alwaysConsumeMouseWheel: false },
    };

    code: string = '';

    constructor(private _fuseConfigService: FuseConfigService) {}

    ngOnInit(): void {
        this._fuseConfigService.config$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((config) => {
                let scheme = config.scheme;

                if (scheme === 'auto') {
                    scheme = this._getSystemScheme();
                }

                const newTheme = scheme === 'dark' ? 'vs-dark' : 'vs';
                if (this.editorOptions.theme !== newTheme) {
                    this.editorOptions = {
                        ...this.editorOptions,
                        theme: newTheme,
                    };
                }
            });

        if (this.initialCode) {
            this.code = this.initialCode;
        } else {
            this.code =
                this.templates[this.dataType] ||
                this.templates[AtriaDataType.BlockWithTransactions] ||
                '';
        }
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['initialCode'] && !changes['initialCode'].firstChange) {
            const newCode = changes['initialCode'].currentValue;
            if (newCode && newCode !== this.code) {
                this.code = newCode;
            }
        }
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }

    private _getSystemScheme(): 'light' | 'dark' {
        if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    onCodeChange(code: string): void {
        this.code = code;
        this.codeChanged.emit(code);
    }

    onFileUpload(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            const file = input.files[0];
            const reader = new FileReader();

            reader.onload = (e) => {
                const content = e.target?.result as string;
                this.code = content;
                this.codeChanged.emit(content);
            };

            reader.readAsText(file);
            input.value = '';
        }
    }
}
