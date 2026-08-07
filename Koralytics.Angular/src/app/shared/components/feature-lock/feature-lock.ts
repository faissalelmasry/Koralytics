import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { Router } from '@angular/router';

@Component({
  selector: 'app-feature-lock',
  standalone: true,
  imports: [CommonModule, CustomButtonComponent],
  templateUrl: './feature-lock.html',
  styleUrl: './feature-lock.css'
})
export class FeatureLockComponent implements OnInit {
  private tokenStorage = inject(TokenStorageService);
  private router = inject(Router);

  @Input() feature: string = '';
  @Input() featureName: string = '';
  @Input() mode: 'card' | 'button' = 'card';

  isLocked: boolean = false;
  requiredTier: string = 'Pro';

  ngOnInit() {
    this.checkAccess();
  }

  private checkAccess() {
    const tier = this.tokenStorage.getTier();
    
    const eliteFeatures = ['FullAnalyticsSuite', 'AIInsights'];
    const proFeatures = ['ProgressionAnalytics', 'SquadWeakness', 'Archetype', 'AcademyComparison', 'TransferRate'];

    if (tier === 'Elite') {
      this.isLocked = false;
    } else if (tier === 'Pro') {
      if (eliteFeatures.includes(this.feature)) {
        this.isLocked = true;
        this.requiredTier = 'Elite';
      } else {
        this.isLocked = false;
      }
    } else {
      this.isLocked = true;
      if (eliteFeatures.includes(this.feature)) {
        this.requiredTier = 'Elite';
      } else {
        this.requiredTier = 'Pro';
      }
    }
  }

  upgrade() {
    // Could route to pricing/upgrade page if available
    this.router.navigate(['/']);
  }
}
