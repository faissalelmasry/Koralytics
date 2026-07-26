import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RatingDisplayComponent } from './rating-display';

describe('RatingDisplayComponent', () => {
  let component: RatingDisplayComponent;
  let fixture: ComponentFixture<RatingDisplayComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RatingDisplayComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(RatingDisplayComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display stars for star rating', () => {
    component.value = 8;
    component.max = 10;
    component.type = 'star';
    fixture.detectChanges();
    
    // 8/10 = 80% which maps to 4 filled stars out of 5
    const filledStars = fixture.debugElement.queryAll(By.css('.star.filled'));
    expect(filledStars.length).toBe(4);
  });

  it('should calculate correct percentage', () => {
    component.value = 5;
    component.max = 10;
    expect(component.percentage).toBe(50);
  });

  it('should return correct color based on percentage', () => {
    component.value = 9;
    component.max = 10;
    expect(component.color).toBe('success');  // 90% >= 80%
  });
});
