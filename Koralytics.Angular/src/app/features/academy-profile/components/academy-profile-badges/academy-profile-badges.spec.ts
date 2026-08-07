import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyProfileBadges } from './academy-profile-badges';

describe('AcademyProfileBadges', () => {
  let component: AcademyProfileBadges;
  let fixture: ComponentFixture<AcademyProfileBadges>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyProfileBadges],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyProfileBadges);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
