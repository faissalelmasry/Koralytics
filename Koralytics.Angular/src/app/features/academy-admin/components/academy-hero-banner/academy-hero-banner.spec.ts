import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyHeroBanner } from './academy-hero-banner';

describe('AcademyHeroBanner', () => {
  let component: AcademyHeroBanner;
  let fixture: ComponentFixture<AcademyHeroBanner>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyHeroBanner],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyHeroBanner);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
