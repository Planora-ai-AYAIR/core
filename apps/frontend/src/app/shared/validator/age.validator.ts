import { AbstractControl, ValidatorFn, ValidationErrors } from '@angular/forms';

export function ageValidator(minAge: number, maxAge?: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null; 

    const dateValue = control.value; 
    const birthDate = new Date(dateValue);
    if (isNaN(birthDate.getTime())) return { invalidDate: true };

    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }

    if (age < minAge) return { minAge: { required: minAge, actual: age } };
    if (maxAge !== undefined && age > maxAge) return { maxAge: { required: maxAge, actual: age } };
    return null;
  };
}
