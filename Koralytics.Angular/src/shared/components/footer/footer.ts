import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ScrollRevealDirective } from '../../directives/scroll-reveal.directive';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterModule, ScrollRevealDirective],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {}
