import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyProfileLocations } from './academy-profile-locations';

describe('AcademyProfileLocations', () => {
  let component: AcademyProfileLocations;
  let fixture: ComponentFixture<AcademyProfileLocations>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyProfileLocations],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyProfileLocations);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
