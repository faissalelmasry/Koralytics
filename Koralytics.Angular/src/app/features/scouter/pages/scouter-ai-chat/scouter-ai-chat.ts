import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';

import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ScouterService } from '../../../../../core/services/Scouter/scouter.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { cleanAiBotResponse, formatAiMarkdown } from '../../../../../core/utils/ai-chat.util';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { TranslatePipe } from '@ngx-translate/core';

export interface ChatMessage {
  id: string;
  sender: 'user' | 'assistant';
  text: string;
  timestamp: Date;
}

@Component({
  selector: 'app-scouter-ai-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NavbarComponent,
    Footer,
    ScrollRevealDirective,
    LoadingSpinnerComponent,
    TranslatePipe
  ],
  templateUrl: './scouter-ai-chat.html',
  styleUrls: ['./scouter-ai-chat.css']
})
export class ScouterAiChatComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly scouterService = inject(ScouterService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);

  public currentScouterId = signal<number>(0);
  public currentSessionId = signal<string>('');
  public nlQuery = signal<string>('');
  public chatMessages = signal<ChatMessage[]>([]);
  public isAiLoading = signal<boolean>(false);

  public suggestedPrompts: string[] = [
    'Find left-footed wingers under 20 years old',
    'Recommend top CMs with rating over 80 OVR',
    'Search for promising strikers in Egyptian academies',
    'Analyze defensive midfielders with high ratings',
    'Compare top rated players in the system'
  ];

  ngOnInit(): void {
    this.currentSessionId.set(crypto.randomUUID());
    const paramId = this.route.snapshot.paramMap.get('scouterId');
    if (paramId) {
      this.currentScouterId.set(Number(paramId));
    } else {
      const token = this.tokenStorage.getAccessToken();
      const decoded = token ? this.decodeTokenPayload(token) : null;
      if (decoded) {
        this.currentScouterId.set(decoded.userId);
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

    this.scouterService.aiChatBot(query, this.currentSessionId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reply: string) => {
          const cleanedText = cleanAiBotResponse(reply);
          const botMsg: ChatMessage = {
            id: (Date.now() + 1).toString(),
            sender: 'assistant',
            text: cleanedText || 'No response returned from Scouting AI Assistant.',
            timestamp: new Date()
          };
          this.chatMessages.update(msgs => [...msgs, botMsg]);
          this.isAiLoading.set(false);
          this.scrollChatToBottom();
        },
        error: (err: HttpErrorResponse) => {
          console.error('AI ChatBot query failed', err);
          const fallbackMsg = 'عذراً، حدث خطأ أثناء التواصل مع المساعد الذكي. يرجى المحاولة مرة أخرى لاحقاً.';
          const botMsg: ChatMessage = {
            id: (Date.now() + 1).toString(),
            sender: 'assistant',
            text: `⚠️ ${fallbackMsg}`,
            timestamp: new Date()
          };
          this.chatMessages.update(msgs => [...msgs, botMsg]);
          this.toastService.show(fallbackMsg, 'error');
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

  public navigateToSearch(): void {
    const id = this.currentScouterId();
    if (id) {
      this.router.navigate(['/search', id]);
    } else {
      this.router.navigate(['/search']);
    }
  }

  private scrollChatToBottom(): void {
    setTimeout(() => {
      const feed = document.getElementById('scouterAiChatFeed');
      if (feed) {
        feed.scrollTop = feed.scrollHeight;
      }
    }, 80);
  }

  private decodeTokenPayload(token: string): { userId: number; roles: string[] } | null {
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
      if (!userId) return null;

      const rawRoles = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const roles: string[] = Array.isArray(rawRoles) ? rawRoles : rawRoles ? [rawRoles] : [];

      return { userId, roles };
    } catch {
      return null;
    }
  }

  public formatMessageText(text: string): SafeHtml {
    if (!text) return '';
    const formatted = formatAiMarkdown(text);
    return this.sanitizer.bypassSecurityTrustHtml(formatted);
  }
}
