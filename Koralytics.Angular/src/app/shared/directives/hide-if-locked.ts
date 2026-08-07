import { Directive, Input, TemplateRef, ViewContainerRef, inject, OnInit } from '@angular/core';
import { TokenStorageService } from '../../../core/services/auth/token-storage.service';

@Directive({
  selector: '[hideIfLocked]',
  standalone: true
})
export class HideIfLockedDirective implements OnInit {
  private templateRef = inject(TemplateRef<any>);
  private viewContainer = inject(ViewContainerRef);
  private tokenStorage = inject(TokenStorageService);

  @Input('hideIfLocked') requiredFeature: string = '';

  ngOnInit() {
    this.updateView();
  }

  private updateView() {
    const tier = this.tokenStorage.getTier();
    let hasAccess = false;

    // Based on Tier Limits
    if (tier === 'Elite') {
      hasAccess = true;
    } else if (tier === 'Pro') {
      const proFeatures = [
        'ProgressionAnalytics',
        'SquadWeakness'
      ];
      hasAccess = proFeatures.includes(this.requiredFeature);
    } else { // Starter
      hasAccess = false;
    }

    if (hasAccess) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    } else {
      this.viewContainer.clear();
    }
  }
}
