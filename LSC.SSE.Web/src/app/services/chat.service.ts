import { Injectable } from '@angular/core';
import { ChatMessage } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly apiUrl = 'https://localhost:7033/api/chat/stream-ollama';
  //private readonly apiUrl = 'https://localhost:7033/api/chat/stream';

  /**
   * Opens a POST-based SSE connection to the backend.
   * Returns a cancel() function the caller can invoke to abort mid-stream.
   */
  streamMessage(
    messages: ChatMessage[],
    onChunk: (text: string) => void,
    onDone:  () => void,
    onError: (err: unknown) => void
  ): () => void {
    const controller = new AbortController();

    fetch(this.apiUrl, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify({ messages }),
      signal:  controller.signal,
    })
    .then(async (response) => {
      if (!response.ok) {
        throw new Error(`Server error: HTTP ${response.status}`);
      }

      const reader  = response.body!.getReader();
      const decoder = new TextDecoder();
      let   buffer  = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        // Accumulate raw bytes into a text buffer
        buffer += decoder.decode(value, { stream: true });

        // SSE events are separated by blank lines (\n\n).
        // Split on that boundary and process each complete event.
        const events = buffer.split('\n\n');
        // Keep the last (possibly incomplete) chunk in the buffer
        buffer = events.pop() ?? '';

        for (const event of events) {
          // An SSE event block looks like:
          //   event: delta
          //   data: Hello
          // We parse each line and extract the event type and data.
          const lines     = event.split('\n');
          let   eventType = '';
          let   data      = '';

          for (const line of lines) {
            if (line.startsWith('event: ')) {
              eventType = line.slice(7).trim();
            } else if (line.startsWith('data: ')) {
              data = line.slice(6);  // preserve whitespace/newlines in token
            }
          }

          if (eventType === 'done' || data === '[DONE]') {
            onDone();
            return;
          }

          if (eventType === 'delta' && data) {
            onChunk(data);
          }
        }
      }
      onDone();
    })
    .catch((err: unknown) => {
      if (err instanceof Error && err.name === 'AbortError') return;
      onError(err);
    });

    return () => controller.abort();
  }
}