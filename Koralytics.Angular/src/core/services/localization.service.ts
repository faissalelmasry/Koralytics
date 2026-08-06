import { Injectable, signal, effect } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class LocalizationService {
  private readonly LANG_KEY = 'koralytics_lang';
  public currentLang = signal<string>('en');

  constructor(private translate: TranslateService) {
    this.initLang();
  }

  private initLang(): void {
    const savedLang = localStorage.getItem(this.LANG_KEY);
    const defaultLang = savedLang || 'en';
    
    this.translate.setFallbackLang('en');
    this.setLanguage(defaultLang);
  }

  public setLanguage(lang: string): void {
    this.translate.use(lang).subscribe(() => {
      this.currentLang.set(lang);
      localStorage.setItem(this.LANG_KEY, lang);
      this.updateDirection(lang);
    });
  }

  public toggleLanguage(): void {
    const newLang = this.currentLang() === 'en' ? 'ar' : 'en';
    this.setLanguage(newLang);
  }

  private updateDirection(lang: string): void {
    const htmlTag = document.documentElement;
    if (lang === 'ar') {
      htmlTag.setAttribute('dir', 'rtl');
      htmlTag.setAttribute('lang', 'ar');
    } else {
      htmlTag.setAttribute('dir', 'ltr');
      htmlTag.setAttribute('lang', 'en');
    }
  }
}
