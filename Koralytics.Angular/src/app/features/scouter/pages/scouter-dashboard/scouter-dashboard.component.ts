import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';

@Component({
  selector: 'app-scouter-dashboard',
  standalone: true,
  imports: [CommonModule, NavbarComponent, Footer],
  template: `
    <app-navbar></app-navbar>
    <main style="min-height: 80vh; padding: 120px 24px 48px; text-align: center; color: #f2f3f5; background: #0a0c0f;">
      <h1 style="font-family: 'Bebas Neue', sans-serif; font-size: 48px; letter-spacing: 2px; color: #c8ff4d;">SCOUTER DASHBOARD</h1>
      <p style="color: #8b909a; font-size: 18px; margin-top: 12px;">This feature is currently under active development.</p>
    </main>
    <app-footer></app-footer>
  `
})
export class ScouterDashboardComponent {}
