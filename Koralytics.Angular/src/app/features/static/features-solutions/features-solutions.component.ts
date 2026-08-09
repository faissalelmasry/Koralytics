import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-features-solutions',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './features-solutions.component.html',
  styleUrls: ['./features-solutions.component.css']
})
export class FeaturesSolutionsComponent {
  activeTab: 'management' | 'coaches' | 'players' | 'benchmarks' = 'management';

  setTab(tab: 'management' | 'coaches' | 'players' | 'benchmarks') {
    this.activeTab = tab;
  }
}
