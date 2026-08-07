import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyProfileTeams } from './academy-profile-teams';

describe('AcademyProfileTeams', () => {
  let component: AcademyProfileTeams;
  let fixture: ComponentFixture<AcademyProfileTeams>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyProfileTeams],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyProfileTeams);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
