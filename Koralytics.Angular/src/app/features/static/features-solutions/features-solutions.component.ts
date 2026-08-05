import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-features-solutions',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './features-solutions.component.html',
  styleUrls: ['./features-solutions.component.css']
})
export class FeaturesSolutionsComponent {
  activeTab: 'management' | 'coaches' | 'players' | 'benchmarks' = 'management';

  setTab(tab: 'management' | 'coaches' | 'players' | 'benchmarks') {
    this.activeTab = tab;
  }
}
