import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AcademyResponseDto } from '../../../../../core/interfaces/academy.models';

@Component({
  selector: 'app-academy-hero-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './academy-hero-banner.html',
  styleUrls: ['./academy-hero-banner.css']
})
export class AcademyHeroBannerComponent implements OnInit, OnChanges {
  @Input() academy!: AcademyResponseDto;
  @Input() membersCount: number = 0;
  @Input() coachesCount: number = 0;
  
  initials: string = '';

  ngOnInit() {
    this.updateInitials();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academy']) {
      this.updateInitials();
    }
  }

  updateInitials() {
    if (this.academy?.name) {
      const words = this.academy.name.split(' ');
      if (words.length > 1) {
        this.initials = (words[0].charAt(0) + words[1].charAt(0)).toUpperCase();
      } else {
        this.initials = this.academy.name.substring(0, 2).toUpperCase();
      }
    }
  }
}
