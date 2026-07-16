import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomSelect } from './custom-select';

describe('CustomSelect', () => {
  let component: CustomSelect;
  let fixture: ComponentFixture<CustomSelect>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomSelect],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomSelect);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
