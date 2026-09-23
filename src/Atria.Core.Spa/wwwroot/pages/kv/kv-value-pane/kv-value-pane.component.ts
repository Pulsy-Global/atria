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
    inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FuseConfigService } from 'fuse/services/config';
import { MonacoEditorModule } from 'ngx-monaco-editor-v2';
import { Subject, takeUntil } from 'rxjs';
import { STRING_EMPTY } from '../../../shared/core/constants/common.constants';
import { kvByteLength, kvFormatSize, kvHexDump, kvPretty } from '../kv.util';

// How the stored value is rendered in the value pane. The JSON/Text views show
// the value as-is; Hex shows a byte dump of its UTF-8 encoding.
type KvViewMode = 'json' | 'text' | 'hex';

@Component({
    selector: 'kv-value-pane',
    standalone: true,
    templateUrl: './kv-value-pane.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        FormsModule,
        MatButtonModule,
        MatIconModule,
        MatMenuModule,
        MatTooltipModule,
        MonacoEditorModule,
    ],
})
export class KvValuePaneComponent implements OnInit, OnChanges, OnDestroy {
    private readonly _fuseConfigService = inject(FuseConfigService);
    private readonly _unsubscribeAll: Subject<void> = new Subject<void>();

    @Input() bucket = STRING_EMPTY;
    @Input() key = STRING_EMPTY;
    @Input() value = STRING_EMPTY;
    @Input() isJson = false;

    @Output() readonly close = new EventEmitter<void>();

    activeTab: 'value' | 'metadata' = 'value';
    view: KvViewMode = 'text';
    content = STRING_EMPTY;
    copied = false;

    editorOptions = {
        theme: 'vs-dark',
        language: 'plaintext',
        readOnly: true,
        automaticLayout: true,
        minimap: { enabled: false },
        fontSize: 14,
        lineNumbers: 'on',
        scrollBeyondLastLine: false,
        wordWrap: 'on',
        renderLineHighlight: 'none',
        scrollbar: {
            useShadows: false,
            verticalScrollbarSize: 8,
            horizontalScrollbarSize: 8,
            alwaysConsumeMouseWheel: false,
        },
    };

    ngOnInit(): void {
        this._fuseConfigService.config$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((config) => {
                let scheme = config?.scheme;

                if (scheme === 'auto') {
                    scheme = window.matchMedia('(prefers-color-scheme: dark)').matches
                        ? 'dark'
                        : 'light';
                }

                const theme = scheme === 'dark' ? 'vs-dark' : 'vs';

                if (this.editorOptions.theme !== theme) {
                    this.editorOptions = { ...this.editorOptions, theme };
                }
            });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['key'] || changes['value'] || changes['isJson']) {
            // Default to the most useful view for the value's type; Hex is always
            // opt-in via the menu.
            this.view = this.isJson ? 'json' : 'text';
            this.activeTab = 'value';
            this.copied = false;
            this._applyView();
        }
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }

    setView(view: KvViewMode): void {
        this.view = view;
        this._applyView();
    }

    get typeLabel(): string {
        return this.isJson ? 'JSON' : 'TEXT';
    }

    get viewLabel(): string {
        return this.view === 'json' ? 'JSON' : this.view === 'hex' ? 'Hex' : 'Plain text';
    }

    get size(): string {
        return kvFormatSize(kvByteLength(this.value));
    }

    get charCount(): number {
        return this.value?.length ?? 0;
    }

    copy(): void {
        navigator.clipboard
            ?.writeText(this.content)
            .then(() => {
                this.copied = true;
                setTimeout(() => (this.copied = false), 2000);
            })
            .catch(() => {
                /* clipboard unavailable: ignore */
            });
    }

    download(): void {
        const extension = this.view === 'json' ? 'json' : this.view === 'hex' ? 'hex.txt' : 'txt';
        const safeName = (this.key || 'value').replace(/[^a-z0-9._-]+/gi, '_');
        const blob = new Blob([this.content], { type: 'text/plain;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');

        anchor.href = url;
        anchor.download = `${safeName}.${extension}`;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        URL.revokeObjectURL(url);
    }

    onClose(): void {
        this.close.emit();
    }

    // Rebuilds the shown content + editor layout for the active view. Hex is
    // fixed-column text, so it must not word-wrap and has no use for line
    // numbers (the dump carries its own offsets); JSON still uses Monaco's
    // native grammar, Text/Hex fall back to plain text.
    private _applyView(): void {
        this.content = this.view === 'json'
            ? kvPretty(this.value)
            : this.view === 'hex'
                ? kvHexDump(this.value)
                : this.value;

        const isHex = this.view === 'hex';
        const language = this.view === 'json' ? 'json' : 'plaintext';

        this.editorOptions = {
            ...this.editorOptions,
            language,
            wordWrap: isHex ? 'off' : 'on',
            lineNumbers: isHex ? 'off' : 'on',
        };
    }
}
