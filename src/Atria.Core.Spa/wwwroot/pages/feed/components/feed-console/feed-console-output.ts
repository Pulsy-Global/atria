export class FeedConsoleOutput {
    readonly lines: string[] = [];

    clear(): void {
        this.lines.length = 0;
    }

    addLine(message: string): void {
        this.lines.push(message);
    }

    addResult(label: string, rawResult?: string | null): void {
        if (!rawResult) {
            this.addLine(`${label}: (empty)`);
            return;
        }

        const formattedLines = this._formatJsonLines(rawResult);

        if (formattedLines.length === 1) {
            this.addLine(`${label}: ${formattedLines[0]}`);
            return;
        }

        this.addLine(`${label}:\n${formattedLines.join('\n')}`);
    }

    private _formatJsonLines(rawResult: string): string[] {
        const trimmed = rawResult.trim();

        if (!trimmed) {
            return ['(empty)'];
        }

        const looksLikeJson = trimmed.startsWith('{') || trimmed.startsWith('[');

        if (looksLikeJson) {
            try {
                const parsed = JSON.parse(trimmed);
                const pretty = JSON.stringify(parsed, null, 2);
                return pretty.split('\n');
            } catch {
                // Fall back to raw output if JSON parse fails.
            }
        }

        return trimmed.split('\n');
    }
}
