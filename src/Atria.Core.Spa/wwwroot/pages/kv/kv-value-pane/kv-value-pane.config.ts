export interface KvEditorOptions {
    theme: string;
    language: string;
    readOnly: boolean;
    automaticLayout: boolean;
    minimap: { enabled: boolean };
    fontSize: number;
    lineNumbers: 'on' | 'off';
    scrollBeyondLastLine: boolean;
    wordWrap: 'on' | 'off';
    renderLineHighlight: string;
    scrollbar: {
        useShadows: boolean;
        verticalScrollbarSize: number;
        horizontalScrollbarSize: number;
        alwaysConsumeMouseWheel: boolean;
    };
}

export const KV_EDITOR_OPTIONS: KvEditorOptions = {
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
