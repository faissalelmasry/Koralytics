import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FootballPitch } from './football-pitch';

describe('FootballPitch', () => {
  let component: FootballPitch;
  let fixture: ComponentFixture<FootballPitch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FootballPitch],
    }).compileComponents();

    fixture = TestBed.createComponent(FootballPitch);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
