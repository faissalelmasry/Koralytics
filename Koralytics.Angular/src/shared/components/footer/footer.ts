import { Component } from '@angular/core';
import { ScrollRevealDirective } from '../../directives/scroll-reveal.directive';

@Component({
  selector: 'app-footer',
  imports: [ScrollRevealDirective],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {}
