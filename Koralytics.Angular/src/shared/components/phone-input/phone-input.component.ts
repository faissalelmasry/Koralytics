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
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
  imports: [CommonModule, TranslatePipe],
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
    { code: 'EG', name: 'COMMON.COUNTRIES.EG', flag: '🇪🇬', dialCode: '+20', length: 10, placeholder: '1012345678' },
    { code: 'SA', name: 'COMMON.COUNTRIES.SA', flag: '🇸🇦', dialCode: '+966', length: 9, placeholder: '501234567' },
    { code: 'AE', name: 'COMMON.COUNTRIES.AE', flag: '🇦🇪', dialCode: '+971', length: 9, placeholder: '501234567' },
    { code: 'QA', name: 'COMMON.COUNTRIES.QA', flag: '🇶🇦', dialCode: '+974', length: 8, placeholder: '33123456' },
    { code: 'KW', name: 'COMMON.COUNTRIES.KW', flag: '🇰🇼', dialCode: '+965', length: 8, placeholder: '50123456' },
    { code: 'BH', name: 'COMMON.COUNTRIES.BH', flag: '🇧🇭', dialCode: '+973', length: 8, placeholder: '39123456' },
    { code: 'OM', name: 'COMMON.COUNTRIES.OM', flag: '🇴🇲', dialCode: '+968', length: 8, placeholder: '91234567' },
    { code: 'JO', name: 'COMMON.COUNTRIES.JO', flag: '🇯🇴', dialCode: '+962', length: 9, placeholder: '791234567' },
    { code: 'MA', name: 'COMMON.COUNTRIES.MA', flag: '🇲🇦', dialCode: '+212', length: 9, placeholder: '612345678' },
    { code: 'GB', name: 'COMMON.COUNTRIES.GB', flag: '🇬🇧', dialCode: '+44', length: 10, placeholder: '7911123456' },
    { code: 'US', name: 'COMMON.COUNTRIES.US', flag: '🇺🇸', dialCode: '+1', length: 10, placeholder: '2015550123' },
    { code: 'DE', name: 'COMMON.COUNTRIES.DE', flag: '🇩🇪', dialCode: '+49', length: [10, 11], placeholder: '15112345678' },
    { code: 'FR', name: 'COMMON.COUNTRIES.FR', flag: '🇫🇷', dialCode: '+33', length: 9, placeholder: '612345678' },
    { code: 'ES', name: 'COMMON.COUNTRIES.ES', flag: '🇪🇸', dialCode: '+34', length: 9, placeholder: '612345678' },
    { code: 'IT', name: 'COMMON.COUNTRIES.IT', flag: '🇮🇹', dialCode: '+39', length: 10, placeholder: '3123456789' }
  ];

  selectedCountry: CountryOption = this.countries[0]; // Default: Egypt (+20)
  digits: string = '';
  isDropdownOpen: boolean = false;
  isFocused: boolean = false;
  isTouched: boolean = false;

  onChange = (_: any) => {};
  onTouched = () => {};
  onValidatorChange = () => {};

  constructor(private eRef: ElementRef, private translate: TranslateService) {}

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

  private normalizeDigits(val: string): string {
    const arabicNumbers = ['٠','١','٢','٣','٤','٥','٦','٧','٨','٩'];
    let res = '';
    for (let char of val) {
      const idx = arabicNumbers.indexOf(char);
      res += idx !== -1 ? idx : char;
    }
    return res.replace(/\D/g, '');
  }

  onDigitsInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let raw = this.normalizeDigits(input.value);

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
        const info = err['invalidPhone'];
        const countryName = this.translate.instant(info.country);
        return this.translate.instant('COMMON.ERRORS.INVALID_PHONE_LENGTH', {
          country: countryName,
          expected: Array.isArray(info.expectedLength) ? info.expectedLength.join(' or ') : info.expectedLength,
          dialCode: this.selectedCountry.dialCode
        });
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
        this.digits = this.normalizeDigits(clean.substring(matchedNoPlus.dialCode.length - 1));
        return;
      }
      this.digits = this.normalizeDigits(clean);
      return;
    }

    // Sort countries by longest dial code first (e.g. +971 before +97)
    const matched = this.countries
      .slice()
      .sort((a, b) => b.dialCode.length - a.dialCode.length)
      .find(c => clean.startsWith(c.dialCode));

    if (matched) {
      this.selectedCountry = matched;
      this.digits = this.normalizeDigits(clean.substring(matched.dialCode.length));
    } else {
      this.digits = this.normalizeDigits(clean);
    }
  }
}
