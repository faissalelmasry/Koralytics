import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

interface FaqItem {
  question: string;
  answer: string;
  category: 'general' | 'privacy' | 'portals';
  open?: boolean;
}

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './faq.component.html',
  styleUrls: ['./faq.component.css']
})
export class FaqComponent {
  searchQuery = '';
  selectedCategory: 'all' | 'general' | 'privacy' | 'portals' = 'all';

  faqs: FaqItem[] = [
    {
      question: 'How does Koralytics use football statistics across academies?',
      answer: 'We aggregate anonymized performance data (e.g., average pass accuracy, sprint speeds by age group) to create competitive benchmarks. No personally identifiable information (PII) is ever exposed.',
      category: 'privacy',
      open: true
    },
    {
      question: 'Can parents and players log in directly?',
      answer: 'Yes, Koralytics provides dedicated portals for players and parents to track individual growth curves, match stats, attendance records, and schedules transparently.',
      category: 'portals',
      open: true
    },
    {
      question: 'Is data isolated between different academies?',
      answer: 'Absolutely. Every academy\'s private operational, financial, and personal data is completely isolated within tenant boundaries. Only anonymized, aggregated football metrics contribute to overall ecosystem analytics.',
      category: 'privacy',
      open: true
    },
    {
      question: 'How quickly can our academy onboard?',
      answer: 'Onboarding takes less than 24 hours. Once your academy account is initialized, branch managers can invite coaches, register squads, and begin tracking telemetry immediately.',
      category: 'general',
      open: false
    },
    {
      question: 'What hardware or sensors are required for telemetry?',
      answer: 'Koralytics works out of the box with standard mobile tablets and web browsers for manual drill & match input. It also integrates seamlessly with GPS player vests and optical camera video systems.',
      category: 'general',
      open: false
    },
    {
      question: 'How are minor athletes\' data protected?',
      answer: 'We enforce strict minor data protection protocols. Scouters and external users cannot view minor contact details or personal IDs; only sports telemetry radar metrics are visible under parental consent.',
      category: 'privacy',
      open: false
    }
  ];

  toggleFaq(faq: FaqItem) {
    faq.open = !faq.open;
  }

  get filteredFaqs(): FaqItem[] {
    return this.faqs.filter(faq => {
      const matchesCategory = this.selectedCategory === 'all' || faq.category === this.selectedCategory;
      const matchesQuery = !this.searchQuery || 
        faq.question.toLowerCase().includes(this.searchQuery.toLowerCase()) || 
        faq.answer.toLowerCase().includes(this.searchQuery.toLowerCase());
      return matchesCategory && matchesQuery;
    });
  }

  setCategory(cat: 'all' | 'general' | 'privacy' | 'portals') {
    this.selectedCategory = cat;
  }
}
