import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-transfer-canvas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './transfer-canvas.component.html',
  styleUrls: ['./transfer-canvas.component.css']
})
export class TransferCanvasComponent {

  @Input() overallTrainingAvg: number = 0;
  @Input() overallTournamentAvg: number = 0;
  @Input() transferClassification: string = '';

  private readonly positionMap: Record<string, { left: number; top: number }> = {
    Elite:       { left: 75, top: 25 },
    Natural:     { left: 25, top: 25 },
    Trainable:   { left: 75, top: 75 },
    NeedsWork:   { left: 25, top: 75 },
    Developing:  { left: 25, top: 75 },
  };

  get nodeLeft(): string {
    const pos = this.positionMap[this.transferClassification];
    return pos ? `${pos.left}%` : '50%';
  }

  get nodeTop(): string {
    const pos = this.positionMap[this.transferClassification];
    return pos ? `${pos.top}%` : '50%';
  }

  get nodeColor(): string {
    switch (this.transferClassification) {
      case 'Elite': return '#a3e635';
      case 'Trainable': return '#38bdf8';
      case 'Natural': return '#facc15';
      case 'NeedsWork': return '#f87171';
      case 'Developing': return '#f87171';
      default: return '#6b7280';
    }
  }

  get classificationGlow(): string {
    switch (this.transferClassification) {
      case 'Elite': return 'rgba(163,230,53,0.5)';
      case 'Trainable': return 'rgba(56,189,248,0.5)';
      case 'Natural': return 'rgba(250,204,21,0.35)';
      case 'NeedsWork': return 'rgba(248,113,113,0.4)';
      case 'Developing': return 'rgba(248,113,113,0.4)';
      default: return 'rgba(107,114,128,0.3)';
    }
  }

  get drillIndex(): number {
    return Math.round(this.overallTrainingAvg);
  }

  get matchIndex(): number {
    return Math.round(this.overallTournamentAvg);
  }

  get transferGap(): number {
    return Math.round(this.overallTrainingAvg - this.overallTournamentAvg);
  }

  get transferEfficiency(): string {
    const gap = this.transferGap;
    if (gap > 0) return `+${gap}%`;
    return `${gap}%`;
  }

  get efficiencyColor(): string {
    const gap = this.transferGap;
    if (gap > 10) return '#a3e635';
    if (gap > 0) return '#38bdf8';
    if (gap >= -10) return '#facc15';
    return '#f87171';
  }
}
