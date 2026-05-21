import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { InputComponent } from './input.component';
import { ComponentRef } from '@angular/core';

describe('InputComponent', () => {
  let component: InputComponent;
  let fixture: ComponentFixture<InputComponent>;
  let componentRef: ComponentRef<InputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InputComponent, ReactiveFormsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(InputComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should update value on input', () => {
    const inputElement = fixture.nativeElement.querySelector('input');
    const testValue = 'test value';

    inputElement.value = testValue;
    inputElement.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.value()).toBe(testValue);
  });

  it('should mark as touched on blur', () => {
    const inputElement = fixture.nativeElement.querySelector('input');

    inputElement.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(component.isTouched()).toBe(true);
  });

  it('should set focused state on focus', () => {
    const inputElement = fixture.nativeElement.querySelector('input');

    inputElement.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(component.isFocused()).toBe(true);
  });

  it('should work with reactive forms', () => {
    const formControl = new FormControl('initial value');
    component.writeValue('initial value');

    expect(component.value()).toBe('initial value');
  });

  it('should disable input when setDisabledState is called', () => {
    component.setDisabledState(true);
    fixture.detectChanges();

    const inputElement = fixture.nativeElement.querySelector('input');
    expect(inputElement.disabled).toBe(true);
  });

  it('should display label when provided', () => {
    componentRef.setInput('label', 'Test Label');
    fixture.detectChanges();

    const label = fixture.nativeElement.querySelector('label');
    expect(label).toBeTruthy();
    expect(label.textContent.trim()).toBe('Test Label');
  });

  it('should display error message when showError is true', () => {
    componentRef.setInput('errorMessage', 'Error message');
    componentRef.setInput('showError', true);
    component.isTouched.set(true);
    fixture.detectChanges();

    const errorSpan = fixture.nativeElement.querySelector('[role="alert"]');
    expect(errorSpan).toBeTruthy();
    expect(errorSpan.textContent.trim()).toBe('Error message');
  });

  it('should display helper text when no error', () => {
    componentRef.setInput('helperText', 'Helper text');
    componentRef.setInput('showError', false);
    fixture.detectChanges();

    const helperSpan = fixture.nativeElement.querySelector('[id$="-helper"]');
    expect(helperSpan).toBeTruthy();
    expect(helperSpan.textContent.trim()).toBe('Helper text');
  });

  it('should display suffix icon when provided', () => {
    componentRef.setInput('suffixIcon', 'pi pi-eye');
    fixture.detectChanges();

    const suffixButton = fixture.nativeElement.querySelector('button');
    expect(suffixButton).toBeTruthy();

    const icon = suffixButton.querySelector('i');
    expect(icon.classList.contains('pi-eye')).toBe(true);
  });
});
