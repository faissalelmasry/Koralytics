import { Component, OnInit, signal, computed, effect, inject, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';
import { cleanAiBotResponse } from '../../../../core/utils/ai-chat.util';

import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import {
  ParentAiService,
  ParentChatRequest,
  ParentChatResponse,
  ParentChatStreamChunk
} from '@core/services/parent/ParentAiService';
import { TokenStorageService } from '@core/services/auth/token-storage.service';
import { ParentService } from '../../../../core/services/parent/parent.service';
import { FeatureLockComponent } from '../../../shared/components/feature-lock/feature-lock';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  meta?: {
    usedRAG: boolean;
    usedSQL: boolean;
    responseTime: number;
  };
}

interface SuggestedPrompt {
  icon: 'trend' | 'target' | 'book' | 'shield';
  label: string;
  prompt: string;
}

const SESSION_STORAGE_KEY = 'koralytics_parent_chat_session_id';

@Component({
  selector: 'app-parent-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NavbarComponent,
    Footer,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    StatusChipComponent,
    FeatureLockComponent,
    TranslatePipe
  ],
  templateUrl: './parent-chat.component.html',
  styleUrls: ['./parent-chat.component.css']
})
export class ParentChatComponent implements OnInit, AfterViewChecked {
  private readonly aiService = inject(ParentAiService);
  private readonly parentService = inject(ParentService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly translate = inject(TranslateService);

  isLocked = signal<boolean>(false);

  ngOnInit(): void {
    this.parentService.getMyChildren().subscribe({
      next: (res: any) => {
        const children = res?.data || res || [];
        const hasEliteChild = children.some((c: any) => c.isEliteTier || c.academyTier === 'Elite');
        this.isLocked.set(!hasEliteChild);
      },
      error: () => {
        this.isLocked.set(true);
      }
    });

    this.messages.set([{
      role: 'assistant',
      text: this.translate.instant('PARENT.CHAT.WELCOME_MESSAGE')
    }]);
  }

  @ViewChild('chatScrollContainer') private scrollContainer!: ElementRef;
  @ViewChild('chatTextarea') private textareaRef?: ElementRef<HTMLTextAreaElement>;

  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // FIX #7: session ID is now persisted across reloads instead of being
  // regenerated every time the component initializes, so ChatSummary /
  // last-3-turns memory in the Langflow flow stays tied to the same
  // conversation for a returning parent.
  sessionId = signal<string>(this.loadOrCreateSessionId());

  currentInput = signal<string>('');

  messages = signal<ChatMessage[]>([]);

  // ── UI-only additions below (empty-state prompts, textarea auto-grow) --
  // none of this touches how messages are sent; sendMessage/sendStreaming/
  // sendNonStreaming are unchanged from the original file.

  get suggestedPrompts(): SuggestedPrompt[] {
    return [
      { icon: 'trend', label: this.translate.instant('PARENT.CHAT.PROMPT_1_LABEL'), prompt: "How has my child's performance trended over the last month?" },
      { icon: 'target', label: this.translate.instant('PARENT.CHAT.PROMPT_2_LABEL'), prompt: 'What position best suits my child based on their stats?' },
      { icon: 'book', label: this.translate.instant('PARENT.CHAT.PROMPT_3_LABEL'), prompt: 'What can we work on at home to improve their game?' },
      { icon: 'shield', label: this.translate.instant('PARENT.CHAT.PROMPT_4_LABEL'), prompt: 'Can you explain the offside rule simply?' },
    ];
  }

  // Only the seeded welcome message present (and nothing in flight yet) =
  // show the centered empty/start state with suggested prompts instead of
  // the transcript view.
  hasStartedConversation = computed<boolean>(() => this.messages().length > 1 || this.isLoading());

  constructor() {
    // Keeps the textarea sized to its content, including the reset back to
    // one line after sendMessage() clears currentInput() post-send -- this
    // only observes the signal sendMessage() already sets, it doesn't
    // change what sendMessage() does.
    effect(() => {
      this.currentInput();
      queueMicrotask(() => this.autoResizeTextarea());
    });
  }

  private loadOrCreateSessionId(): string {

    const currentUser = this.tokenStorage.getUser();
    if (currentUser && currentUser.userId) {
      return `parent_${currentUser.userId}`;
    }


    return 'parent_guest';
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  private scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    } catch (err) { }
  }

  private autoResizeTextarea(): void {
    const el = this.textareaRef?.nativeElement;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 160) + 'px';
  }

  /** Fills the input with a suggested prompt and sends it via the existing,
   *  unmodified sendMessage() flow -- this is a UI convenience, not a
   *  different send path. */
  useSuggestedPrompt(prompt: string): void {
    this.currentInput.set(prompt);
    this.sendMessage();
  }

  sendMessage(): void {
    const text = this.currentInput().trim();
    if (!text || this.isLoading()) return;

    this.messages.update(msgs => [...msgs, { role: 'user', text }]);
    this.currentInput.set('');
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const request: ParentChatRequest = {
      sessionId: this.sessionId(),
      message: text
    };

    //this.sendStreaming(request);
    this.sendNonStreaming(request);
  }

  /**
   * FIX #6: consumes the real streaming endpoint, appending tokens to the
   * assistant bubble as they arrive instead of waiting for the full answer.
   */
  private sendStreaming(request: ParentChatRequest): void {
    // Placeholder assistant message that gets filled in as tokens stream in.
    this.messages.update(msgs => [...msgs, { role: 'assistant', text: '' }]);
    const assistantIndex = this.messages().length - 1;

    this.aiService.streamMessage(request).subscribe({
      next: (chunk: ParentChatStreamChunk) => {
        if (chunk.eventType === 'token' && chunk.text) {
          this.messages.update(msgs => {
            const updated = [...msgs];
            updated[assistantIndex] = {
              ...updated[assistantIndex],
              text: updated[assistantIndex].text + chunk.text
            };
            return updated;
          });
        } else if (chunk.eventType === 'meta' && chunk.meta) {
          this.messages.update(msgs => {
            const updated = [...msgs];
            updated[assistantIndex] = {
              ...updated[assistantIndex],
              text: cleanAiBotResponse(chunk.meta!.answer),
              meta: {
                usedRAG: chunk.meta!.usedRAG,
                usedSQL: chunk.meta!.usedSQL,
                responseTime: chunk.meta!.responseTime
              }
            };
            return updated;
          });
          this.isLoading.set(false);
        } else if (chunk.eventType === 'error') {
          this.messages.update(msgs => {
            const updated = [...msgs];
            updated[assistantIndex] = {
              ...updated[assistantIndex],
              text: chunk.text ?? this.translate.instant('PARENT.CHAT.DEFAULT_ERROR')
            };
            return updated;
          });
          this.isLoading.set(false);
        }
      },
      error: (err) => {
        this.errorMessage.set(extractErrorMessage(err, this.translate.instant('PARENT.CHAT.CONNECT_ERROR')));
        this.isLoading.set(false);
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  /**
   * Non-streaming fallback, kept for cases where SSE isn't available
   * (e.g. behind a proxy that strips it) — swap sendStreaming for this
   * in sendMessage() if needed.
   */
  private sendNonStreaming(request: ParentChatRequest): void {
    this.aiService.sendMessage(request).subscribe({
      next: (res: ParentChatResponse) => {
        this.messages.update(msgs => [...msgs, {
          role: 'assistant',
          text: cleanAiBotResponse(res.answer),
          meta: {
            usedRAG: res.usedRAG,
            usedSQL: res.usedSQL,
            responseTime: res.responseTime
          }
        }]);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(extractErrorMessage(err, this.translate.instant('PARENT.CHAT.CONNECT_ERROR')));
        this.isLoading.set(false);
      }
    });
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}