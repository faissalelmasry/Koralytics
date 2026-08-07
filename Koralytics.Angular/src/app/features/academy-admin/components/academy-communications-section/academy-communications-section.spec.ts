import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyCommunicationsSection } from './academy-communications-section';

describe('AcademyCommunicationsSection', () => {
  let component: AcademyCommunicationsSection;
  let fixture: ComponentFixture<AcademyCommunicationsSection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyCommunicationsSection],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyCommunicationsSection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
