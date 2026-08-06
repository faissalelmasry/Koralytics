import { Pipe, PipeTransform, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';

@Pipe({
  name: 'localizedDate',
  standalone: true,
  pure: false
})
export class LocalizedDatePipe implements PipeTransform {
  private translate = inject(TranslateService);

  transform(value: any, format = 'mediumDate'): any {
    if (!value) return '';
    try {
      const langRaw: any = this.translate.currentLang;
      const lang = (typeof langRaw === 'function' ? langRaw() : langRaw) || 'en';
      // In Angular, Arabic locale is 'ar'
      const pipe = new DatePipe(lang === 'ar' ? 'ar-EG' : 'en-US'); 
      return pipe.transform(value, format);
    } catch (e) {
      // Fallback to english if locale not found
      const pipe = new DatePipe('en-US');
      return pipe.transform(value, format);
    }
  }
}
