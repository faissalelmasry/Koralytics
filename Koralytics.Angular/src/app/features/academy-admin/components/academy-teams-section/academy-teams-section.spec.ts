import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyTeamsSection } from './academy-teams-section';

describe('AcademyTeamsSection', () => {
  let component: AcademyTeamsSection;
  let fixture: ComponentFixture<AcademyTeamsSection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyTeamsSection],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyTeamsSection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
