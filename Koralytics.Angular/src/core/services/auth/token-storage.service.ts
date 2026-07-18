import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {
  private readonly ACCESS_TOKEN_KEY = 'koralytics_access_token';
  private readonly REFRESH_TOKEN_KEY = 'koralytics_refresh_token';
  private readonly USER_KEY = 'koralytics_user';

  constructor() {}

  // "Remember me" toggles whether we use localStorage (persistent) or sessionStorage (transient)
  // By default, we'll try to determine the storage based on where the token is currently saved.
  
  private getStorage(): Storage {
    // If we already have something in localStorage, keep using it, else use sessionStorage
    if (localStorage.getItem(this.ACCESS_TOKEN_KEY)) {
      return localStorage;
    }
    return sessionStorage;
  }

  public isRememberMe(): boolean {
    return !!localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  public saveTokens(accessToken: string, refreshToken: string, rememberMe: boolean = false): void {
    const storage = rememberMe ? localStorage : sessionStorage;
    
    // Clear the other storage just in case
    const otherStorage = rememberMe ? sessionStorage : localStorage;
    otherStorage.removeItem(this.ACCESS_TOKEN_KEY);
    otherStorage.removeItem(this.REFRESH_TOKEN_KEY);
    
    storage.setItem(this.ACCESS_TOKEN_KEY, accessToken);
    storage.setItem(this.REFRESH_TOKEN_KEY, refreshToken);
  }

  public getAccessToken(): string | null {
    return this.getStorage().getItem(this.ACCESS_TOKEN_KEY);
  }

  public getRefreshToken(): string | null {
    return this.getStorage().getItem(this.REFRESH_TOKEN_KEY);
  }

  public saveUser(user: any, rememberMe: boolean = false): void {
    const storage = rememberMe ? localStorage : sessionStorage;
    storage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  public getUser(): any | null {
    const userStr = this.getStorage().getItem(this.USER_KEY);
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch (e) {
        return null;
      }
    }
    return null;
  }

  public clear(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    
    sessionStorage.removeItem(this.ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(this.REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(this.USER_KEY);
  }
}
