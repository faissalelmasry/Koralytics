import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

interface FaqItem {
  questionKey: string;
  answerKey: string;
  category: 'general' | 'privacy' | 'portals';
  open?: boolean;
}

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, TranslatePipe],
  templateUrl: './faq.component.html',
  styleUrls: ['./faq.component.css']
})
export class FaqComponent {
  searchQuery = '';
  selectedCategory: 'all' | 'general' | 'privacy' | 'portals' = 'all';

  constructor(private translate: TranslateService) {}

  faqs: FaqItem[] = [
    {
      questionKey: 'STATIC.FAQ.Q1_Q',
      answerKey: 'STATIC.FAQ.Q1_A',
      category: 'privacy',
      open: true
    },
    {
      questionKey: 'STATIC.FAQ.Q2_Q',
      answerKey: 'STATIC.FAQ.Q2_A',
      category: 'portals',
      open: true
    },
    {
      questionKey: 'STATIC.FAQ.Q3_Q',
      answerKey: 'STATIC.FAQ.Q3_A',
      category: 'privacy',
      open: true
    },
    {
      questionKey: 'STATIC.FAQ.Q4_Q',
      answerKey: 'STATIC.FAQ.Q4_A',
      category: 'general',
      open: false
    },
    {
      questionKey: 'STATIC.FAQ.Q5_Q',
      answerKey: 'STATIC.FAQ.Q5_A',
      category: 'general',
      open: false
    },
    {
      questionKey: 'STATIC.FAQ.Q6_Q',
      answerKey: 'STATIC.FAQ.Q6_A',
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
      const qText = this.translate.instant(faq.questionKey).toLowerCase();
      const aText = this.translate.instant(faq.answerKey).toLowerCase();
      const query = this.searchQuery.toLowerCase();
      const matchesQuery = !this.searchQuery || 
        qText.includes(query) || 
        aText.includes(query);
      return matchesCategory && matchesQuery;
    });
  }

  setCategory(cat: 'all' | 'general' | 'privacy' | 'portals') {
    this.selectedCategory = cat;
  }
}
