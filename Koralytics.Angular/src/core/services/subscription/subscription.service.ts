import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PlayerSubscriptionDto, CreateSubscriptionDto, PaymentIntentResponse } from '@core/models/subscription/subscription.model';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class SubscriptionService {
    private readonly apiUrl = `${environment.apiUrl}/api/Subscription`;

    constructor(private http: HttpClient) { }

    /**
     * GET /api/Subscription/my-children
     * Fetches subscriptions for the logged-in parent's children.
     */
    getMyChildrenSubscriptions(): Observable<PlayerSubscriptionDto[]> {
        return this.http.get<any>(`${this.apiUrl}/my-children`, {
            params: { t: new Date().getTime().toString() }
        }).pipe(
            map(res => (res?.data !== undefined ? res.data : res) || [])
        );
    }

    /**
     * GET /api/Subscription/academy/{academyId}
     * Fetches all subscriptions for a specific academy.
     */
    getAcademySubscriptions(academyId: number = 2): Observable<PlayerSubscriptionDto[]> {
        return this.http.get<any>(`${this.apiUrl}/academy/${academyId}`).pipe(
            map(res => (res?.data !== undefined ? res.data : res) || [])
        );
    }

    /**
     * GET /api/Subscription/children/{playerId}/history
     * Fetches complete subscription and billing history for a specific player.
     */
    getPlayerSubscriptionHistory(playerId: number): Observable<PlayerSubscriptionDto[]> {
        return this.http.get<any>(`${this.apiUrl}/children/${playerId}/history`).pipe(
            map(res => (res?.data !== undefined ? res.data : res) || [])
        );
    }

    /**
     * POST /api/Subscription
     * Creates or overrides a subscription invoice.
     */
    createSubscription(dto: CreateSubscriptionDto): Observable<PlayerSubscriptionDto> {
        return this.http.post<any>(this.apiUrl, dto).pipe(
            map(res => res?.data || res)
        );
    }

    /**
     * POST /api/Subscription/{id}/pay
     * Settles a subscription payment (Online / Card path).
     */
    paySubscription(subscriptionId: number): Observable<{ message: string }> {
        return this.http.post<any>(`${this.apiUrl}/${subscriptionId}/pay`, {}).pipe(
            map(res => res?.data || res)
        );
    }

    /**
     * POST /api/Subscription/{id}/mark-paid-cash
     * Academy Admin confirms cash receipt at the desk — marks subscription as Paid.
     */
    markAsPaidByCash(subscriptionId: number): Observable<{ message: string }> {
        return this.http.post<any>(`${this.apiUrl}/${subscriptionId}/mark-paid-cash`, {}).pipe(
            map(res => res?.data || res)
        );
    }

    /**
     * POST /api/Subscription/{id}/create-payment-intent
     * Generates Stripe PaymentIntent Client Secret for card checkout.
     */
    createPaymentIntent(subscriptionId: number): Observable<PaymentIntentResponse> {
        return this.http.post<any>(`${this.apiUrl}/${subscriptionId}/create-payment-intent`, {}).pipe(
            map(res => res?.data || res)
        );
    }
}