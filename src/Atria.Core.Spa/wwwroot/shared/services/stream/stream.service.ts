import { Inject, Injectable, OnDestroy, Optional } from '@angular/core';
import { Observable } from 'rxjs';
import { STREAM_AUTH_HEADERS } from '../stream-auth.token';

@Injectable({
    providedIn: 'root'
})
export class StreamService implements OnDestroy {
    private readonly RECONNECT_DELAY_MS = 2000;
    private activeControllers = new Set<AbortController>();

    constructor(
        @Optional()
        @Inject(STREAM_AUTH_HEADERS)
        private getStreamHeaders: (() => Promise<Record<string, string>>) | null,
    ) {}

    ngOnDestroy(): void {
        this.disconnectAll();
    }

    connect<T>(url: string): Observable<T> {
        return new Observable<T>(subscriber => {
            const abortController = new AbortController();
            this.activeControllers.add(abortController);

            const stream = async () => {
                if (abortController.signal.aborted) return;

                try {
                    const headers: Record<string, string> = {};
                    if (this.getStreamHeaders) {
                        Object.assign(headers, await this.getStreamHeaders());
                    }

                    const response = await fetch(url, {
                        headers,
                        signal: abortController.signal,
                    });

                    if (!response.ok || !response.body) {
                        throw new Error(`Stream response error: ${response.status}`);
                    }

                    const reader = response.body.getReader();
                    const decoder = new TextDecoder();
                    let buffer = '';

                    while (true) {
                        const { done, value } = await reader.read();
                        if (done) break;

                        buffer += decoder.decode(value, { stream: true });

                        const lines = buffer.split('\n');
                        buffer = lines.pop() ?? '';

                        for (const line of lines) {
                            if (!line.startsWith('data: ')) continue;
                            try {
                                subscriber.next(JSON.parse(line.slice(6)) as T);
                            } catch {
                                // Skip malformed SSE data lines
                            }
                        }
                    }
                } catch (err) {
                    if (abortController.signal.aborted) return;
                    console.error('SSE stream error:', err);
                }

                if (!abortController.signal.aborted) {
                    setTimeout(() => stream(), this.RECONNECT_DELAY_MS);
                }
            };

            stream();

            return () => {
                abortController.abort();
                this.activeControllers.delete(abortController);
            };
        });
    }

    disconnectAll(): void {
        this.activeControllers.forEach(c => c.abort());
        this.activeControllers.clear();
    }
}
