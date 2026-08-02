import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatchService } from '../../../../../core/services/match/match.service';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

export interface ParsedReport {
  summary: string;
  goals: string;
  motm: string;
  individual: string;
}

export interface PlayerEvaluation {
  name: string;
  initials: string;
  desc: string;
}

@Component({
  selector: 'app-match-report',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent
  ],
  templateUrl: './match-report.component.html',
  styleUrls: ['./match-report.component.css']
})
export class MatchReportComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private matchService = inject(MatchService);
  private cdr = inject(ChangeDetectorRef);

  matchId!: number;
  isLoading = true;
  isGenerating = false;
  error = '';

  reportData: any = null;
  parsedReport: ParsedReport = {
    summary: '',
    goals: '',
    motm: '',
    individual: ''
  };

  motmName = '---';
  motmInitials = '--';
  motmRating = '7.50';
  playersList: PlayerEvaluation[] = [];

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.matchId = Number(idParam);
      this.loadReport();
    } else {
      this.error = 'معرّف المباراة غير صحيح';
      this.isLoading = false;
    }
  }

  loadReport(): void {
    this.isLoading = true;
    this.error = '';

    this.matchService.getMatchReport(this.matchId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.reportData = res.data;
          this.processReportText(res.data.reportText || '');
        } else {
          this.error = res.message || 'فشل في تحميل التقرير';
        }
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error fetching match report:', err);
        // If not found, show user friendly prompt with option to generate
        this.error = 'لم يتم العثور على تقرير لهذه المباراة بعد.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  generateReport(): void {
    this.isGenerating = true;
    this.error = '';

    this.matchService.generateMatchReport(this.matchId).subscribe({
      next: (res) => {
        this.isGenerating = false;
        this.loadReport();
      },
      error: (err) => {
        console.error('Error generating match report:', err);
        this.isGenerating = false;
        this.error = 'فشل في إنشاء التقرير عبر الذكاء الاصطناعي. يُرجى المحاولة لاحقًا.';
        this.cdr.markForCheck();
      }
    });
  }

  goBackToMatch(): void {
    this.router.navigate(['/match', this.matchId]);
  }

  private processReportText(rawText: string): void {
    if (!rawText) return;

    // Find section positions accurately
    const sec2Match = rawText.match(/(?:⚽|\u26BD|(?:\*\*|\#)?\s*2[\.\)]\s*(?:سير|الأهداف)|سير اللقاء والأهداف|سير اللقاء)/i);
    const sec3Match = rawText.match(/(?:👑|🌟|\uD83C\uDF1F|(?:\*\*|\#)?\s*3[\.\)]\s*(?:نجم|رجل|MOTM)|نجم اللقاء|رجل المباراة|MOTM|MAN OF THE MATCH)/i);
    const sec4Match = rawText.match(/(?:🧠|\uD83E\uDDE0|(?:\*\*|\#)?\s*4[\.\)]\s*(?:التقييم|العناصر)|التقييم الفردي)/i);

    const pos2 = sec2Match && sec2Match.index !== undefined ? sec2Match.index : -1;
    const pos3 = sec3Match && sec3Match.index !== undefined ? sec3Match.index : -1;
    const pos4 = sec4Match && sec4Match.index !== undefined ? sec4Match.index : -1;

    let summaryRaw = '';
    let goalsRaw = '';
    let motmRaw = '';
    let individualRaw = '';

    if (pos2 > -1) {
      summaryRaw = rawText.substring(0, pos2);
      if (pos3 > pos2) {
        goalsRaw = rawText.substring(pos2, pos3);
        if (pos4 > pos3) {
          motmRaw = rawText.substring(pos3, pos4);
          individualRaw = rawText.substring(pos4);
        } else {
          motmRaw = rawText.substring(pos3);
        }
      } else if (pos4 > pos2) {
        goalsRaw = rawText.substring(pos2, pos4);
        individualRaw = rawText.substring(pos4);
      } else {
        goalsRaw = rawText.substring(pos2);
      }
    } else if (pos3 > -1) {
      summaryRaw = rawText.substring(0, pos3);
      if (pos4 > pos3) {
        motmRaw = rawText.substring(pos3, pos4);
        individualRaw = rawText.substring(pos4);
      } else {
        motmRaw = rawText.substring(pos3);
      }
    } else {
      summaryRaw = rawText;
    }

    this.parsedReport = {
      summary: this.cleanSectionText(summaryRaw),
      goals: this.cleanSectionText(goalsRaw),
      motm: this.cleanSectionText(motmRaw),
      individual: this.cleanSectionText(individualRaw)
    };

    // MOTM Extraction
    if (this.parsedReport.motm) {
      const nameMatch = this.parsedReport.motm.match(/^(.*?) كان رجل المباراة/) ||
        this.parsedReport.motm.match(/([A-Za-z\u0600-\u06FF\s]+) (?:كان|هو) رجل/);
      if (nameMatch) {
        this.motmName = nameMatch[1].trim();
      } else {
        const words = this.parsedReport.motm.split(' ');
        this.motmName = words.slice(0, 2).join(' ');
      }

      this.motmInitials = this.getInitials(this.motmName);

      const ratingMatch = this.parsedReport.motm.match(/(?:تقييمه|التقييم)[\s\w\u0600-\u06FF]*?(\d+(?:\.\d+)?)/);
      if (ratingMatch) {
        this.motmRating = ratingMatch[1];
      }
    }

    // Individual Players Extraction
    if (this.parsedReport.individual) {
      const lines = this.parsedReport.individual
        .split('\n')
        .filter(l => l.trim().startsWith('-') || l.trim().startsWith('*') || l.trim().startsWith('•'));

      this.playersList = lines.map(line => {
        const clean = line.replace(/^[\-\*\•]\s*/, '').trim();
        const parts = clean.split(':');
        const name = parts[0]?.replace(/\*\*/g, '').trim() || 'لاعب';
        const desc = this.cleanSectionText(parts.slice(1).join(':').trim());
        return {
          name: this.cleanMarkdownText(name),
          initials: this.getInitials(name),
          desc
        };
      });
    }
  }

  private cleanSectionText(text: string): string {
    if (!text) return '';

    let cleaned = text.trim();

    // 1. Strip top-level section header title/numbers like "👑 **3. نجم اللقاء - (MOTM)**:" or "🏰 1. بطاقة الماتش والملخص العام:"
    cleaned = cleaned.replace(/^(?:[\uD83C-\uDBFF\uDC00-\uDFFF\u2600-\u26FF\u2300-\u27BF\s]|[\*\#\-\_])*(?:\d+[\.\)\-]?\s*)?(?:بطاقة الماتش والملخص العام|ملخص المباراة|بطاقة الماتش|الملخص العام|ملخص|سير اللقاء والأهداف|سير اللقاء|الأهداف|نجم اللقاء|رجل المباراة|MOTM|MAN OF THE MATCH|التقييم الفردي وتفاصيل العناصر الفنية|التقييم الفردي|تفاصيل العناصر الفنية)?(?:\s*\(MOTM\))?(?:\s*[\-\:]\s*|\:\s*|\s+)/i, '');

    // 2. Strip any remaining leading numbers like "1.", "2.", "3.", "4." at the beginning of section
    cleaned = cleaned.replace(/^(?:[\*\#\-\_]|\s)*?\d+[\.\)\-]\s*/, '');

    // 3. Remove markdown bolding **text** -> text
    cleaned = cleaned.replace(/\*\*(.*?)\*\*/g, '$1');

    // 4. Remove all emojis
    cleaned = cleaned.replace(/[\uD83C-\uDBFF\uDC00-\uDFFF\u2600-\u26FF\u2300-\u27BF]/g, '');

    // 5. Remove leading colons or hyphens
    cleaned = cleaned.replace(/^[\:\-\–\—\s]+/, '');

    return cleaned.trim();
  }

  private cleanMarkdownText(text: string): string {
    if (!text) return '';
    return text
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .replace(/[\uD83C-\uDBFF\uDC00-\uDFFF\u2600-\u26FF\u2300-\u27BF]/g, '')
      .trim();
  }

  private getInitials(name: string): string {
    if (!name || name === '---') return '--';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + ' ' + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }
}
