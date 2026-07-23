import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { CardComponent } from './card';

describe('CardComponent', () => {
  let component: CardComponent;
  let fixture: ComponentFixture<CardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(CardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render title and subtitle', () => {
    component.title = 'Test Card Title';
    component.subtitle = 'Test Subtitle';
    fixture.detectChanges();

    const titleEl = fixture.debugElement.query(By.css('.card-title')).nativeElement;
    const subtitleEl = fixture.debugElement.query(By.css('.card-subtitle')).nativeElement;

    expect(titleEl.textContent).toContain('Test Card Title');
    expect(subtitleEl.textContent).toContain('Test Subtitle');
  });

  it('should emit cardClicked on click if clickable is true', () => {
    spyOn(component.cardClicked, 'emit');
    component.clickable = true;
    fixture.detectChanges();

    const cardEl = fixture.debugElement.query(By.css('.card'));
    cardEl.triggerEventHandler('click', null);

    expect(component.cardClicked.emit).toHaveBeenCalled();
  });

  it('should not emit cardClicked on click if clickable is false', () => {
    spyOn(component.cardClicked, 'emit');
    component.clickable = false;
    fixture.detectChanges();

    const cardEl = fixture.debugElement.query(By.css('.card'));
    cardEl.triggerEventHandler('click', null);

    expect(component.cardClicked.emit).not.toHaveBeenCalled();
  });
});
