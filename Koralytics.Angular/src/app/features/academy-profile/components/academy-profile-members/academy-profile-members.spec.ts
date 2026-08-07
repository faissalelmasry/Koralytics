import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyProfileMembers } from './academy-profile-members';

describe('AcademyProfileMembers', () => {
  let component: AcademyProfileMembers;
  let fixture: ComponentFixture<AcademyProfileMembers>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyProfileMembers],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyProfileMembers);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
