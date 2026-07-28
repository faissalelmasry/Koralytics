import {
  Component,
  Input,
  Output,
  EventEmitter,
  forwardRef,
  ElementRef,
  HostListener,
  HostBinding
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ControlValueAccessor,
  NG_VALUE_ACCESSOR,
  NG_VALIDATORS,
  Validator,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';

export interface CountryOption {
  code: string;       // ISO code e.g. 'EG'
  name: string;       // Country name e.g. 'Egypt'
  flag: string;       // Emoji flag e.g. '🇪🇬'
  dialCode: string;   // Dial code e.g. '+20'
  length: number | number[]; // Required digits length after dial code
  placeholder: string;
}

@Component({
  selector: 'app-phone-input',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './phone-input.component.html',
  styleUrls: ['./phone-input.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true
    }
  ]
})
export class PhoneInputComponent implements ControlValueAccessor, Validator {
  @Input() label: string = 'Phone';
  @Input() placeholder: string = '';
  @Input() errorMessage: string = '';
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;

  @HostBinding('class.is-open') get isOpenClass(): boolean {
    return this.isDropdownOpen;
  }

  private _value: string = '';

  @Input()
  set value(val: string | null | undefined) {
    const clean = val || '';
    if (this._value !== clean) {
      this._value = clean;
      this.parseValue(clean);
    }
  }

  get value(): string {
    return this._value;
  }

  @Output() valueChange = new EventEmitter<string>();

  countries: CountryOption[] = [
    { code: 'EG', name: 'Egypt', flag: '🇪🇬', dialCode: '+20', length: 10, placeholder: '1012345678' },
    { code: 'SA', name: 'Saudi Arabia', flag: '🇸🇦', dialCode: '+966', length: 9, placeholder: '501234567' },
    { code: 'AE', name: 'United Arab Emirates', flag: '🇦🇪', dialCode: '+971', length: 9, placeholder: '501234567' },
    { code: 'QA', name: 'Qatar', flag: '🇶🇦', dialCode: '+974', length: 8, placeholder: '33123456' },
    { code: 'KW', name: 'Kuwait', flag: '🇰🇼', dialCode: '+965', length: 8, placeholder: '50123456' },
    { code: 'BH', name: 'Bahrain', flag: '🇧🇭', dialCode: '+973', length: 8, placeholder: '39123456' },
    { code: 'OM', name: 'Oman', flag: '🇴🇲', dialCode: '+968', length: 8, placeholder: '91234567' },
    { code: 'JO', name: 'Jordan', flag: '🇯🇴', dialCode: '+962', length: 9, placeholder: '791234567' },
    { code: 'MA', name: 'Morocco', flag: '🇲🇦', dialCode: '+212', length: 9, placeholder: '612345678' },
    { code: 'GB', name: 'United Kingdom', flag: '🇬🇧', dialCode: '+44', length: 10, placeholder: '7911123456' },
    { code: 'US', name: 'United States', flag: '🇺🇸', dialCode: '+1', length: 10, placeholder: '2015550123' },
    { code: 'DE', name: 'Germany', flag: '🇩🇪', dialCode: '+49', length: [10, 11], placeholder: '15112345678' },
    { code: 'FR', name: 'France', flag: '🇫🇷', dialCode: '+33', length: 9, placeholder: '612345678' },
    { code: 'ES', name: 'Spain', flag: '🇪🇸', dialCode: '+34', length: 9, placeholder: '612345678' },
    { code: 'IT', name: 'Italy', flag: '🇮🇹', dialCode: '+39', length: 10, placeholder: '3123456789' }
  ];

  selectedCountry: CountryOption = this.countries[0]; // Default: Egypt (+20)
  digits: string = '';
  isDropdownOpen: boolean = false;
  isFocused: boolean = false;
  isTouched: boolean = false;

  onChange = (_: any) => {};
  onTouched = () => {};
  onValidatorChange = () => {};

  constructor(private eRef: ElementRef) {}

  @HostListener('document:click', ['$event'])
  clickout(event: Event) {
    if (!this.eRef.nativeElement.contains(event.target)) {
      this.isDropdownOpen = false;
    }
  }

  toggleDropdown(event: MouseEvent): void {
    if (this.disabled) return;
    event.stopPropagation();
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  get maxDigitsLength(): number {
    if (Array.isArray(this.selectedCountry.length)) {
      return Math.max(...this.selectedCountry.length);
    }
    return this.selectedCountry.length;
  }

  selectCountry(country: CountryOption, event?: MouseEvent): void {
    if (event) event.stopPropagation();
    this.selectedCountry = country;
    this.isDropdownOpen = false;
    if (this.digits.length > this.maxDigitsLength) {
      this.digits = this.digits.substring(0, this.maxDigitsLength);
    }
    this.emitChange();
  }

  onDigitsInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let raw = input.value.replace(/\D/g, '');

    // Strip leading zero when entering local style number (e.g. 01012345678 in Egypt)
    if (this.selectedCountry.code === 'EG' && raw.startsWith('0') && raw.length > 1) {
      raw = raw.substring(1);
    }

    if (raw.length > this.maxDigitsLength) {
      raw = raw.substring(0, this.maxDigitsLength);
    }

    this.digits = raw;
    input.value = this.digits;
    this.emitChange();
  }

  onBlur(): void {
    this.isFocused = false;
    this.isTouched = true;
    this.onTouched();
  }

  private emitChange(): void {
    const fullVal = this.digits ? `${this.selectedCountry.dialCode}${this.digits}` : '';
    this._value = fullVal;
    this.valueChange.emit(fullVal);
    this.onChange(fullVal);
    this.onValidatorChange();
  }

  // ControlValueAccessor methods
  writeValue(val: string | null | undefined): void {
    const clean = val || '';
    if (this._value !== clean) {
      this._value = clean;
      this.parseValue(clean);
    } else if (!clean) {
      this.digits = '';
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  // NG_VALIDATORS implementation
  validate(control: AbstractControl): ValidationErrors | null {
    if (!this.digits || this.digits.trim() === '') {
      return null; // Let required validator handle empty fields
    }

    const expected = this.selectedCountry.length;
    let isValidLength = false;

    if (Array.isArray(expected)) {
      isValidLength = expected.includes(this.digits.length);
    } else {
      isValidLength = this.digits.length === expected;
    }

    if (!isValidLength) {
      return {
        invalidPhone: {
          country: this.selectedCountry.name,
          expectedLength: expected,
          actualLength: this.digits.length,
          message: `${this.selectedCountry.name} numbers must be ${Array.isArray(expected) ? expected.join(' or ') : expected} digits after ${this.selectedCountry.dialCode}`
        }
      };
    }

    return null;
  }

  get currentErrorMessage(): string {
    if (this.errorMessage) return this.errorMessage;

    if (this.isTouched && this.digits && this.digits.trim() !== '') {
      const err = this.validate(null as any);
      if (err && err['invalidPhone']) {
        return err['invalidPhone'].message;
      }
    }
    return '';
  }

  private parseValue(val: string): void {
    if (!val || val.trim() === '') {
      this.digits = '';
      return;
    }
    let clean = val.trim();
    if (!clean.startsWith('+')) {
      // If no '+' prefix, check if it starts with any dialCode without +
      const matchedNoPlus = this.countries
        .slice()
        .sort((a, b) => b.dialCode.length - a.dialCode.length)
        .find(c => clean.startsWith(c.dialCode.substring(1)));
      if (matchedNoPlus) {
        this.selectedCountry = matchedNoPlus;
        this.digits = clean.substring(matchedNoPlus.dialCode.length - 1);
        return;
      }
      this.digits = clean.replace(/\D/g, '');
      return;
    }

    // Sort countries by longest dial code first (e.g. +971 before +97)
    const matched = this.countries
      .slice()
      .sort((a, b) => b.dialCode.length - a.dialCode.length)
      .find(c => clean.startsWith(c.dialCode));

    if (matched) {
      this.selectedCountry = matched;
      this.digits = clean.substring(matched.dialCode.length).replace(/\D/g, '');
    } else {
      this.digits = clean.replace(/\D/g, '');
    }
  }
}
