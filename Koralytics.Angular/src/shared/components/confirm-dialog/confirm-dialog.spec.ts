import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ConfirmDialogComponent } from './confirm-dialog';

describe('ConfirmDialogComponent', () => {
  let component: ConfirmDialogComponent;
  let fixture: ComponentFixture<ConfirmDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmDialogComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display when isOpen is true', () => {
    component.isOpen = true;
    fixture.detectChanges();

    const dialogEl = fixture.debugElement.query(By.css('.confirm-dialog'));
    expect(dialogEl).toBeTruthy();
  });

  it('should not display when isOpen is false', () => {
    component.isOpen = false;
    fixture.detectChanges();

    const dialogEl = fixture.debugElement.query(By.css('.confirm-dialog'));
    expect(dialogEl).toBeNull();
  });

  it('should emit confirm on confirm click if matches confirmation text', () => {
    spyOn(component.confirm, 'emit');
    component.isOpen = true;
    component.requiresConfirmation = true;
    component.confirmationText = 'DELETE';
    component.confirmationInput = 'DELETE';
    fixture.detectChanges();

    component.onConfirm();
    expect(component.confirm.emit).toHaveBeenCalled();
  });

  it('should disable confirm button if confirmation text does not match', () => {
    component.isOpen = true;
    component.requiresConfirmation = true;
    component.confirmationText = 'DELETE';
    component.confirmationInput = 'WRONG';
    fixture.detectChanges();

    expect(component.isConfirmDisabled).toBeTrue();
  });
});
