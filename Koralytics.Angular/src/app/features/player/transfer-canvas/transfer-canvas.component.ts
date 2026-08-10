import { Component, Input, OnChanges, SimpleChanges, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-transfer-canvas',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './transfer-canvas.component.html',
  styleUrls: ['./transfer-canvas.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransferCanvasComponent implements OnInit, OnChanges {

  @Input() overallTrainingAvg: number = 0;
  @Input() overallTournamentAvg: number = 0;
  @Input() transferClassification: string = '';

  private readonly positionMap: Record<string, { left: number; top: number }> = {
    Elite:        { left: 25, top: 25 },
    Natural:      { left: 75, top: 25 },
    Expert:       { left: 75, top: 25 },
    Trainable:    { left: 75, top: 75 },
    NeedsWork:    { left: 25, top: 75 },
    Developing:   { left: 25, top: 75 },
  };

  displayClassification = '';
  displayClassificationKey = 'PLAYER.PLAYER';
  nodeLeft = '50%';
  nodeTop = '50%';
  nodeColor = '#6b7280';
  classificationGlow = 'rgba(107,114,128,0.3)';
  drillIndex = 0;
  matchIndex = 0;
  transferGap = 0;
  transferEfficiency = '0%';
  efficiencyColor = '#6b7280';

  ngOnInit() {
    this.computeState();
  }

  ngOnChanges(changes: SimpleChanges) {
    this.computeState();
  }

  private computeState() {
    if (!this.transferClassification) {
      this.displayClassification = '';
      this.displayClassificationKey = 'PLAYER.PLAYER';
    } else if (this.transferClassification === 'Natural') {
      this.displayClassification = 'Expert';
      this.displayClassificationKey = 'PLAYER.EXPERT';
    } else {
      this.displayClassification = this.transferClassification;
      if (this.transferClassification === 'NeedsWork') {
        this.displayClassificationKey = 'PLAYER.NEEDS_WORK';
      } else {
        this.displayClassificationKey = 'PLAYER.' + this.transferClassification.toUpperCase();
      }
    }

    const pos = this.positionMap[this.transferClassification] || this.positionMap[this.displayClassification];
    this.nodeLeft = pos ? `${pos.left}%` : '50%';
    this.nodeTop = pos ? `${pos.top}%` : '50%';

    switch (this.transferClassification) {
      case 'Elite':
        this.nodeColor = '#38bdf8';
        this.classificationGlow = 'rgba(56,189,248,0.5)';
        break;
      case 'Natural':
      case 'Expert':
        this.nodeColor = '#a3e635';
        this.classificationGlow = 'rgba(163,230,53,0.5)';
        break;
      case 'Trainable':
        this.nodeColor = '#facc15';
        this.classificationGlow = 'rgba(250,204,21,0.5)';
        break;
      case 'NeedsWork':
      case 'Developing':
        this.nodeColor = '#f87171';
        this.classificationGlow = 'rgba(248,113,113,0.4)';
        break;
      default:
        this.nodeColor = '#6b7280';
        this.classificationGlow = 'rgba(107,114,128,0.3)';
        break;
    }

    this.drillIndex = Math.round(this.overallTrainingAvg);
    this.matchIndex = Math.round(this.overallTournamentAvg);
    this.transferGap = Math.round(this.overallTournamentAvg - this.overallTrainingAvg);

    const gap = this.transferGap;
    if (gap > 0) this.transferEfficiency = `+${gap}%`;
    else this.transferEfficiency = `${gap}%`;

    if (gap > 10) this.efficiencyColor = '#a3e635';
    else if (gap > 0) this.efficiencyColor = '#38bdf8';
    else if (gap >= -10) this.efficiencyColor = '#facc15';
    else this.efficiencyColor = '#f87171';
  }
}
