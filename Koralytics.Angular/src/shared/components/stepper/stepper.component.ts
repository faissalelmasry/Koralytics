import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stepper',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stepper.component.html',
  styleUrls: ['./stepper.component.css']
})
export class StepperComponent {
  @Input() steps: string[] = [];
  @Input() currentStep: number = 0; // 0-indexed
  @Output() stepClick = new EventEmitter<number>();

  onStepClick(index: number) {
    // Only allow clicking completed steps (going backwards)
    if (index < this.currentStep) {
      this.stepClick.emit(index);
    }
  }
}
