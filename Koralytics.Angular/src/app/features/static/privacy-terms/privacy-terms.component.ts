import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-privacy-terms',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './privacy-terms.component.html',
  styleUrls: ['./privacy-terms.component.css']
})
export class PrivacyTermsComponent {
  activeSection: 'minor' | 'ownership' | 'anonymization' | 'terms' = 'minor';

  setSection(section: 'minor' | 'ownership' | 'anonymization' | 'terms') {
    this.activeSection = section;
  }
}
