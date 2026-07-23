import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyAdminsSection } from './academy-admins-section';

describe('AcademyAdminsSection', () => {
  let component: AcademyAdminsSection;
  let fixture: ComponentFixture<AcademyAdminsSection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyAdminsSection],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyAdminsSection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
