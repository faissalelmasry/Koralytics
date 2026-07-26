import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyBadgesSection } from './academy-badges-section';

describe('AcademyBadgesSection', () => {
  let component: AcademyBadgesSection;
  let fixture: ComponentFixture<AcademyBadgesSection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyBadgesSection],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyBadgesSection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
