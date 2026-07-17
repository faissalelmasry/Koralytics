import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomToggle } from './custom-toggle';

describe('CustomToggle', () => {
  let component: CustomToggle;
  let fixture: ComponentFixture<CustomToggle>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomToggle],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomToggle);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
