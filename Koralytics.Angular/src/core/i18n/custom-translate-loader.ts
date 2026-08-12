import { TranslateLoader } from '@ngx-translate/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export class CustomTranslateLoader implements TranslateLoader {
  constructor(private http: HttpClient, private prefix: string = '/i18n/') {}

  public getTranslation(lang: string): Observable<any> {
    // List the feature modules that have their own translation files here
    const modules = ['common', 'auth', 'system-admin', 'academy-admin', 'academy-profile', 'profile', 'player', 'drills', 'coach' ,'match', 'parent', 'static','academy','scouter','notification'];

    const requests = modules.map((module) => {
      const path = `${this.prefix}${lang}/${module}.json`;
      return this.http.get(path).pipe(
        catchError(() => {
          console.warn(`Could not load translations for module: ${module} in language: ${lang}`);
          return of({}); // Return empty object if file not found
        })
      );
    });

    return forkJoin(requests).pipe(
      map((responses: any[]) => {
        // Merge all module translations into a single object
        return responses.reduce((acc, curr) => ({ ...acc, ...curr }), {});
      })
    );
  }
}
