import { Component, Input, OnInit, AfterViewInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, registerables, ChartConfiguration } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chart.html',
  styleUrls: ['./chart.css']
})
export class ChartComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;
  
  @Input() type: 'line' | 'bar' | 'pie' | 'radar' | 'doughnut' = 'line';
  @Input() title: string = '';
  @Input() data: any = {};
  @Input() options: any = {};
  
  chart: Chart | null = null;

  ngOnInit() {}

  ngAfterViewInit() {
    this.initChart();
  }

  private initChart() {
    if (!this.chartCanvas) return;

    const config: ChartConfiguration = {
      type: this.type as any,
      data: this.data,
      options: this.getMergedOptions()
    };

    this.chart = new Chart(this.chartCanvas.nativeElement, config);
  }

  private getMergedOptions(): any {
    const defaultOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true,
          labels: {
            color: '#9ca3af',
            font: {
              family: "'Inter', sans-serif",
              size: 12,
              weight: '500'
            }
          }
        },
        title: { 
          display: !!this.title,
          text: this.title,
          color: '#f2f3f5',
          font: {
            family: "'Inter', sans-serif",
            size: 15,
            weight: '700'
          }
        }
      },
      scales: this.type === 'pie' || this.type === 'doughnut' || this.type === 'radar' ? undefined : {
        x: {
          grid: {
            color: 'rgba(255, 255, 255, 0.05)',
            drawBorder: false
          },
          ticks: {
            color: '#9ca3af',
            font: {
              family: "'Inter', sans-serif"
            }
          }
        },
        y: {
          grid: {
            color: 'rgba(255, 255, 255, 0.05)',
            drawBorder: false
          },
          ticks: {
            color: '#9ca3af',
            font: {
              family: "'Inter', sans-serif"
            }
          }
        }
      }
    };

    return { ...defaultOptions, ...this.options };
  }

  ngOnDestroy() {
    if (this.chart) {
      this.chart.destroy();
    }
  }
}
