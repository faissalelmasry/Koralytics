import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { extractErrorMessage } from '../../../../../core/utils/http-error.util';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

export interface ChatMessage {
  id: string;
  sender: 'user' | 'assistant';
  text: string;
  timestamp: Date;
}

@Component({
  selector: 'app-academy-ai-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NavbarComponent,
    Footer,
    ScrollRevealDirective,
    LoadingSpinnerComponent
  ],
  templateUrl: './academy-ai-chat.html',
  styleUrls: ['./academy-ai-chat.css']
})
export class AcademyAiChatComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly academyService = inject(AcademyService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);

  public currentAcademyId = signal<number>(0);
  public currentSessionId = signal<string>('');
  public nlQuery = signal<string>('');
  public chatMessages = signal<ChatMessage[]>([]);
  public isAiLoading = signal<boolean>(false);

  public suggestedPrompts: string[] = [
    'What about his passing performance?',
    'Show top performing players in our U20 squad',
    'Analyze team stamina and readiness for next match',
    'Summarize recent drill performance for midfielders',
    'Which players need tactical improvement?'
  ];

  ngOnInit(): void {
    this.currentSessionId.set(crypto.randomUUID());
    const paramId = this.route.snapshot.paramMap.get('academyId');
    if (paramId) {
      this.currentAcademyId.set(Number(paramId));
    } else {
      const token = this.tokenStorage.getAccessToken();
      const decoded = token ? this.decodeTokenPayload(token) : null;
      if (decoded && decoded.academyId) {
        this.currentAcademyId.set(decoded.academyId);
      }
    }
  }

  public onSendQuery(queryText?: string): void {
    const query = (queryText ?? this.nlQuery()).trim();
    if (!query || this.isAiLoading()) return;

    const userMsg: ChatMessage = {
      id: Date.now().toString(),
      sender: 'user',
      text: query,
      timestamp: new Date()
    };

    this.chatMessages.update(msgs => [...msgs, userMsg]);
    this.nlQuery.set('');
    this.isAiLoading.set(true);
    this.scrollChatToBottom();

    this.academyService.academySearchAiChatBot(query, this.currentSessionId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reply: string) => {
          const botMsg: ChatMessage = {
            id: (Date.now() + 1).toString(),
            sender: 'assistant',
            text: reply || 'No response returned from Academy AI Assistant.',
            timestamp: new Date()
          };
          this.chatMessages.update(msgs => [...msgs, botMsg]);
          this.isAiLoading.set(false);
          this.scrollChatToBottom();
        },
        error: (err: HttpErrorResponse) => {
          console.error('Academy AI ChatBot query failed', err);
          const errorMsg = extractErrorMessage(err, 'Failed to connect to Academy AI Assistant.');
          const botMsg: ChatMessage = {
            id: (Date.now() + 1).toString(),
            sender: 'assistant',
            text: `⚠️ ${errorMsg}`,
            timestamp: new Date()
          };
          this.chatMessages.update(msgs => [...msgs, botMsg]);
          this.toastService.show(errorMsg, 'error');
          this.isAiLoading.set(false);
          this.scrollChatToBottom();
        }
      });
  }

  public useSuggestedPrompt(prompt: string): void {
    this.onSendQuery(prompt);
  }

  public onNewChat(): void {
    this.currentSessionId.set(crypto.randomUUID());
    this.chatMessages.set([]);
  }

  public clearChat(): void {
    this.onNewChat();
  }

  public navigateToDashboard(): void {
    this.router.navigate(['/']);
  }

  private scrollChatToBottom(): void {
    setTimeout(() => {
      const feed = document.getElementById('academyAiChatFeed');
      if (feed) {
        feed.scrollTop = feed.scrollHeight;
      }
    }, 80);
  }

  private decodeTokenPayload(token: string): { userId: number; academyId?: number; roles: string[] } | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      let payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      while (payload.length % 4) payload += '=';
      const decoded = JSON.parse(atob(payload));

      const userId = parseInt(
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? '0',
        10
      );

      const academyIdStr = decoded['AcademyId'] ?? decoded['academyId'];
      const academyId = academyIdStr ? parseInt(academyIdStr, 10) : undefined;

      const rawRoles = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const roles: string[] = Array.isArray(rawRoles) ? rawRoles : rawRoles ? [rawRoles] : [];

      return { userId, academyId, roles };
    } catch {
      return null;
    }
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
          return `<p class="ai-paragraph">${trimmed.replace(/\n/g, '<br/>')}</p>`;
        })
        .join('');
    } else {
      formatted = formatted.replace(/\n/g, '<br/>');
    }

    return this.sanitizer.bypassSecurityTrustHtml(formatted);
  }
}
