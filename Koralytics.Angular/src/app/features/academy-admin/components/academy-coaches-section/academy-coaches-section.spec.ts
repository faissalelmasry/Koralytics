import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyCoachesSection } from './academy-coaches-section';

describe('AcademyCoachesSection', () => {
  let component: AcademyCoachesSection;
  let fixture: ComponentFixture<AcademyCoachesSection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyCoachesSection],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyCoachesSection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
