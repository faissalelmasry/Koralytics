import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

declare var google: any;

@Injectable({
  providedIn: 'root'
})
export class GoogleAuthService {
  private isSdkLoaded = false;
  private clientId = environment.googleClientId;

  constructor() {}

  public loadGoogleSdk(): Promise<void> {
    if (this.isSdkLoaded) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => {
        this.isSdkLoaded = true;
        resolve();
      };
      script.onerror = () => {
        reject(new Error('Failed to load Google Identity Services SDK'));
      };
      document.head.appendChild(script);
    });
  }

  public async renderButton(element: HTMLElement, callback: (idToken: string) => void): Promise<void> {
    try {
      await this.loadGoogleSdk();

      google.accounts.id.initialize({
        client_id: this.clientId,
        callback: (response: any) => {
          if (response && response.credential) {
            callback(response.credential); // Return the idToken via callback
          }
        },
        auto_select: false,
        cancel_on_tap_outside: true,
      });

      google.accounts.id.renderButton(
        element,
        { theme: 'outline', size: 'large', text: 'continue_with', shape: 'pill', width: 400 }
      );
    } catch (err) {
      console.error('Failed to render Google button:', err);
    }
  }
}
