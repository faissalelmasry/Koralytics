import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChartComponent } from './chart';

describe('ChartComponent', () => {
  let component: ChartComponent;
  let fixture: ComponentFixture<ChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChartComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ChartComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize chart after view init', () => {
    component.type = 'line';
    component.data = {
      labels: ['January', 'February', 'March'],
      datasets: [{ data: [10, 20, 30] }]
    };
    fixture.detectChanges();
    expect(component.chart).not.toBeNull();
  });
});
