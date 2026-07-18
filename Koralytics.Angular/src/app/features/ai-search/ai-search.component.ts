import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AnalyticsService } from '../../../core/services/analytics/analytics.service';

interface ChatMessage {
  role: 'user' | 'ai';
  text: string;
  timestamp: Date;
}

@Component({
  selector: 'app-ai-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-search.component.html',
  styleUrls: ['./ai-search.component.css']
})
export class AiSearchComponent {
  private analyticsService = inject(AnalyticsService);

  query = '';
  isLoading = false;
  messages: ChatMessage[] = [];
  errorMessage = '';

  readonly exampleQueries = [
    'Show me left-footed strikers with a rating above 7',
    'Which players have the most Man of the Match awards?',
    'List all injured players in the academy',
    'Top 5 players by drill scores in the Advanced category',
    'Show me players who played more than 60 minutes on average'
  ];

  onSubmit(): void {
    const trimmedQuery = this.query.trim();
    if (!trimmedQuery || this.isLoading) return;

    this.errorMessage = '';
    this.messages.push({
      role: 'user',
      text: trimmedQuery,
      timestamp: new Date()
    });

    const currentQuery = trimmedQuery;
    this.query = '';
    this.isLoading = true;

    this.analyticsService.aiPlayerSearch(currentQuery).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.isSuccess && response.data) {
          this.messages.push({
            role: 'ai',
            text: response.data.answer,
            timestamp: new Date()
          });
        } else {
          this.errorMessage = response.message || 'Unexpected response from AI service.';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message
          || 'Failed to connect to the AI search service. Please try again.';
      }
    });
  }

  useExample(query: string): void {
    this.query = query;
  }

  clearChat(): void {
    this.messages = [];
    this.errorMessage = '';
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSubmit();
    }
  }
}
