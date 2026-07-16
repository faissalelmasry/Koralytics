import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ModalContainer } from './modal-container';

describe('ModalContainer', () => {
  let component: ModalContainer;
  let fixture: ComponentFixture<ModalContainer>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModalContainer],
    }).compileComponents();

    fixture = TestBed.createComponent(ModalContainer);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
