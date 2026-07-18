import { Component, Input, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-button.html',
  styleUrls: ['./custom-button.css']
  })
export class CustomButtonComponent {
 @Input() type: 'button' | 'submit' = 'button';
  // El-Alwan el-gdeda monasba jdan lil-site dashboard style
  @Input() variant: 'accent' | 'coral' | 'cyan' | 'slate' = 'accent'; 
  @Input() loading: boolean = false;
  @Input() disabled: boolean = false;

  @ViewChild('btnElement') btnElement!: ElementRef<HTMLButtonElement>;

  // ---------- 3D Tilt Interaction ----------
  @HostListener('mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    if (this.disabled || this.loading) return;
    const rect = this.btnElement.nativeElement.getBoundingClientRect();
    const px = (e.clientX - rect.left) / rect.width;
    const py = (e.clientY - rect.top) / rect.height;
    
    const btn = this.btnElement.nativeElement;
    btn.style.setProperty('--mx', `${px * 100}%`);
    btn.style.setProperty('--my', `${py * 100}%`);
  }

  @HostListener('mouseleave')
  onMouseLeave() {
    // Reset properties if needed
  }

  // ---------- Dynamic Ripple Click ----------
  createRipple(e: MouseEvent) {
    if (this.disabled || this.loading) return;

    const btn = this.btnElement.nativeElement;
    const rect = btn.getBoundingClientRect();
    const ripple = document.createElement('span');
    const size = Math.max(rect.width, rect.height);
    
    ripple.className = 'ripple';
    ripple.style.width = ripple.style.height = `${size}px`;
    ripple.style.left = `${e.clientX - rect.left - size / 2}px`;
    ripple.style.top = `${e.clientY - rect.top - size / 2}px`;
    
    btn.appendChild(ripple);
    setTimeout(() => ripple.remove(), 650);
  }
}