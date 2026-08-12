import { Component, OnInit, signal, inject, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';
import { cleanAiBotResponse } from '../../../../core/utils/ai-chat.util';

import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import {
  ParentAiService,
  ParentChatRequest,
  ParentChatResponse
} from '@core/services/parent/ParentAiService';
import { TokenStorageService } from '@core/services/auth/token-storage.service';
import { ParentService } from '../../../../core/services/parent/parent.service';
import { FeatureLockComponent } from '../../../shared/components/feature-lock/feature-lock';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

export interface ChatMessage {
  id: string;
  sender: 'user' | 'assistant';
  text: string;
  timestamp: Date;
  meta?: {
    usedRAG: boolean;
    usedSQL: boolean;
    responseTime: number;
  };
}

@Component({
  selector: 'app-parent-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    StatusChipComponent,
    FeatureLockComponent,
    ScrollRevealDirective,
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
  private readonly router = inject(Router);
  private readonly sanitizer = inject(DomSanitizer);

  @ViewChild('chatScrollContainer') private scrollContainer!: ElementRef;

  isLocked = signal<boolean>(false);
  isLoading = signal<boolean>(false);
  isAiLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  sessionId = signal<string>(this.loadOrCreateSessionId());
  nlQuery = signal<string>('');
  chatMessages = signal<ChatMessage[]>([]);

  suggestedPrompts: string[] = [
    "How has my child's performance trended over the last month?",
    "What position best suits my child based on their stats?",
    "What can we work on at home to improve their game?",
    "Can you explain tactical offside rules and positioning?"
  ];

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
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  private loadOrCreateSessionId(): string {
    const currentUser = this.tokenStorage.getUser();
    if (currentUser && currentUser.userId) {
      return `parent_${currentUser.userId}_${Date.now()}`;
    }
    return `parent_session_${Date.now()}`;
  }

  private scrollToBottom(): void {
    try {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch (err) { }
  }

  public navigateToDashboard(): void {
    this.router.navigate(['/parent/dashboard']);
  }

  public onNewChat(): void {
    this.chatMessages.set([]);
    this.nlQuery.set('');
    this.errorMessage.set(null);
    this.sessionId.set(this.loadOrCreateSessionId());
  }

  public useSuggestedPrompt(prompt: string): void {
    this.nlQuery.set(prompt);
    this.onSendQuery();
  }

  public onSendQuery(queryText?: string): void {
    const query = (queryText ?? this.nlQuery()).trim();
    if (!query || this.isAiLoading()) return;

    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      sender: 'user',
      text: query,
      timestamp: new Date()
    };

    this.chatMessages.update(msgs => [...msgs, userMsg]);
    this.nlQuery.set('');
    this.isAiLoading.set(true);
    this.errorMessage.set(null);

    const request: ParentChatRequest = {
      sessionId: this.sessionId(),
      message: query
    };

    this.aiService.sendMessage(request).subscribe({
      next: (res: ParentChatResponse) => {
        const botMsg: ChatMessage = {
          id: crypto.randomUUID(),
          sender: 'assistant',
          text: cleanAiBotResponse(res.answer),
          timestamp: new Date(),
          meta: {
            usedRAG: res.usedRAG,
            usedSQL: res.usedSQL,
            responseTime: res.responseTime
          }
        };
        this.chatMessages.update(msgs => [...msgs, botMsg]);
        this.isAiLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(extractErrorMessage(err, this.translate.instant('PARENT.CHAT.CONNECT_ERROR')));
        this.isAiLoading.set(false);
      }
    });
  }

  public formatMessageText(text: string): SafeHtml {
    if (!text) return '';

    let formatted = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong class="ai-bold-highlight">$1</strong>');
    formatted = formatted.replace(/\*(.*?)\*/g, '<em>$1</em>');

    const paragraphs = formatted.split(/\n\s*\n/);
    if (paragraphs.length > 1) {
      formatted = paragraphs
        .map(p => {
          const trimmed = p.trim();
          if (!trimmed) return '';
          const withBreaks = trimmed.replace(/\n/g, '<br>');
          return `<div class="ai-msg-block">${withBreaks}</div>`;
        })
        .join('');
    } else {
      formatted = formatted.replace(/\n/g, '<br>');
    }

    return this.sanitizer.bypassSecurityTrustHtml(formatted);
  }
}