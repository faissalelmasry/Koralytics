import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyProfileOverview } from './academy-profile-overview';

describe('AcademyProfileOverview', () => {
  let component: AcademyProfileOverview;
  let fixture: ComponentFixture<AcademyProfileOverview>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyProfileOverview],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyProfileOverview);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
