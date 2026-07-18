import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-password-strength',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './password-strength.component.html',
  styleUrls: ['./password-strength.component.css']
})
export class PasswordStrengthComponent implements OnChanges {
  @Input() password = '';

  strength = 0;
  label = '';
  color = 'var(--bg-input)'; // Default

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['password']) {
      this.calculateStrength(this.password || '');
    }
  }

  private calculateStrength(p: string) {
    let score = 0;
    if (p.length > 0) {
      if (p.length >= 8) score++;
      if (/[A-Z]/.test(p)) score++;
      if (/[0-9]/.test(p)) score++;
      if (/[^A-Za-z0-9]/.test(p)) score++;
    }
    
    if (p.length > 0 && score === 0) score = 1;

    this.strength = score;

    switch (score) {
      case 0:
        this.label = '';
        this.color = 'var(--bg-input)';
        break;
      case 1:
        this.label = 'Weak';
        this.color = 'var(--error-color)';
        break;
      case 2:
        this.label = 'Fair';
        this.color = '#ffb04f'; // Yellow/Orange
        break;
      case 3:
        this.label = 'Good';
        this.color = 'var(--accent-cyan, #00f0ff)';
        break;
      case 4:
        this.label = 'Strong';
        this.color = 'var(--accent-lime)';
        break;
    }
  }
}
