# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
npm start          # Dev server at http://localhost:4200
npm run build      # Production build (output: dist/)
npm run watch      # Dev build in watch mode
npm test           # Unit tests via Vitest
```

No lint script is configured. TypeScript strict mode catches most type errors at build time.

## Architecture

This is an **Angular 21 standalone SSE chat frontend** that streams AI responses from a backend in real time.

**Request flow:**
1. User submits a message → added to `messages` signal in `AppComponent`
2. `ChatService.streamMessage()` POSTs `{ messages }` to `https://localhost:7033/api/chat/stream`
3. Response is parsed as SSE: `event: delta` / `data: <token>` chunks appended to the assistant message
4. Stream ends on `event: done` or `data: [DONE]`; user can cancel via `AbortController` at any time

**Key files:**
- [src/app/app.ts](src/app/app.ts) — root component; owns `messages`, `isStreaming`, `errorMsg` signals and all UI logic (send, stop, clear, scroll)
- [src/app/services/chat.service.ts](src/app/services/chat.service.ts) — SSE protocol parsing, `fetch` with `AbortController`, cancellation
- [src/app/models/chat.model.ts](src/app/models/chat.model.ts) — `ChatMessage` interface (`role: 'user' | 'assistant'`, `content: string`)
- [src/app/app.html](src/app/app.html) — chat UI template (message list, textarea, send/stop buttons)

**State management:** Angular Signals (`signal()`, `computed()`) — no NgRx or external store.

**Styling:** Tailwind CSS 4 (utilities) + component-scoped CSS in [src/app/app.css](src/app/app.css) for chat bubbles and the streaming cursor blink animation.

## Backend API Contract

The app expects a server at `https://localhost:7033` that accepts:

```
POST /api/chat/stream
Content-Type: application/json

{ "messages": [{ "role": "user"|"assistant", "content": "..." }] }
```

Response: SSE stream with:
- `event: delta\ndata: <token>\n\n` for each content chunk
- `event: done\n\n` or `data: [DONE]\n\n` to signal completion

## Conventions

- **Standalone components only** — no NgModules
- **Strict TypeScript** — `noImplicitAny`, `strictNullChecks` enforced via [tsconfig.json](tsconfig.json)
- **Prettier** for formatting (config in `package.json`); 2-space indent per [.editorconfig](.editorconfig)
- Tests use **Vitest** (not Karma/Jasmine) — configured in [angular.json](angular.json) under `test`
