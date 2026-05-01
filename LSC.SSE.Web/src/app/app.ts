import { Component, ElementRef, signal, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ChatMessage } from './models/chat.model';
import { ChatService } from './services/chat.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('lsc.sse.web');
  @ViewChild('messagesEnd') messagesEnd!: ElementRef;

  messages    = signal<ChatMessage[]>([]);
  userInput   = '';
  isStreaming = signal(false);
  errorMsg    = signal('');

  private cancelStream?: () => void;

  constructor(private chatService: ChatService) {}

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  sendMessage(): void {
    const text = this.userInput.trim();
    if (!text || this.isStreaming()) return;

    this.errorMsg.set('');

    // 1. Append user message
    this.messages.update(msgs => [
      ...msgs,
      { role: 'user', content: text },
    ]);
    this.userInput = '';

    // 2. Add an empty placeholder for the assistant reply
    this.messages.update(msgs => [
      ...msgs,
      { role: 'assistant', content: '', streaming: true },
    ]);

    this.isStreaming.set(true);

    // 3. Send the full conversation history (minus the empty placeholder)
    const history = this.messages()
      .slice(0, -1)
      .map(({ role, content }) => ({ role, content }));

    this.cancelStream = this.chatService.streamMessage(
      history,

      // onChunk — append each arriving token to the last message
      (chunk) => {
        this.messages.update(msgs => {
          const updated = [...msgs];
          const last    = updated.at(-1)!;
          updated[updated.length - 1] = {
            ...last,
            content: last.content + chunk,
          };
          return updated;
        });
      },

      // onDone — mark streaming complete
      () => {
        this.messages.update(msgs => {
          const updated = [...msgs];
          updated[updated.length - 1] = {
            ...updated.at(-1)!,
            streaming: false,
          };
          return updated;
        });
        this.isStreaming.set(false);
      },

      // onError
      (err) => {
        console.error('Stream error:', err);
        this.errorMsg.set('Something went wrong. Please try again.');
        this.isStreaming.set(false);
      }
    );
  }

  stopStreaming(): void {
    this.cancelStream?.();
    this.messages.update(msgs => {
      const updated = [...msgs];
      updated[updated.length - 1] = {
        ...updated.at(-1)!,
        streaming: false,
      };
      return updated;
    });
    this.isStreaming.set(false);
  }

  clearChat(): void {
    if (this.isStreaming()) this.stopStreaming();
    this.messages.set([]);
    this.errorMsg.set('');
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    try {
      this.messagesEnd.nativeElement.scrollIntoView({ behavior: 'smooth' });
    } catch { /* ignore */ }
  }
}
