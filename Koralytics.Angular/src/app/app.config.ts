import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors, HttpClient } from '@angular/common/http';
import { provideTranslateService, TranslateLoader } from '@ngx-translate/core';
import { authInterceptor } from '../core/interceptors/auth.interceptor';
import { tokenRefreshInterceptor } from '../core/interceptors/token-refresh.interceptor';
import { routes } from './app.routes';
import { CustomTranslateLoader } from '../core/i18n/custom-translate-loader';
import { registerLocaleData } from '@angular/common';
import localeAr from '@angular/common/locales/ar-EG';
import { LocalizationService } from '../core/services/localization.service';

registerLocaleData(localeAr, 'ar-EG');

export function HttpLoaderFactory(http: HttpClient) {
  return new CustomTranslateLoader(http);
}

export function initializeLang(localizationService: LocalizationService) {
  return () => localizationService.initLang();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, tokenRefreshInterceptor])),
    provideTranslateService({
      fallbackLang: 'en',
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      }
    }),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeLang,
      deps: [LocalizationService],
      multi: true
    }
  ]
};
