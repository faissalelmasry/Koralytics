import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AcademyAdminDashboard } from './academy-admin-dashboard';

describe('AcademyAdminDashboard', () => {
  let component: AcademyAdminDashboard;
  let fixture: ComponentFixture<AcademyAdminDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcademyAdminDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(AcademyAdminDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
