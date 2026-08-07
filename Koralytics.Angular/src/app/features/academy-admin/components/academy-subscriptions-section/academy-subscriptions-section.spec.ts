import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademySubscriptionsSection } from './academy-subscriptions-section';

describe('AcademySubscriptionsSection', () => {
  let component: AcademySubscriptionsSection;
  let fixture: ComponentFixture<AcademySubscriptionsSection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademySubscriptionsSection],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademySubscriptionsSection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
