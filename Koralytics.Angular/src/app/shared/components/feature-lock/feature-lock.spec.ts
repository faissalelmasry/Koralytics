import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FeatureLock } from './feature-lock';

describe('FeatureLock', () => {
  let component: FeatureLock;
  let fixture: ComponentFixture<FeatureLock>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeatureLock],
    }).compileComponents();

    fixture = TestBed.createComponent(FeatureLock);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
