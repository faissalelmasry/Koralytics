import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationService } from '../../../core/services/localization.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './language-switcher.component.html',
  styleUrls: ['./language-switcher.component.css']
})
export class LanguageSwitcherComponent {
  constructor(public localizationService: LocalizationService) {}
}
