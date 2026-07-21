export type NotificationType =
    | 'AcademyAnnouncement'
    | 'PlayerMilestone'
    | 'ParentNotification'
    | 'SubscriptionGrace'
    | 'ScouterNotification'
    | string;
export interface CachedNotification {
    id: string;
    title: string;
    content: string;
    type: NotificationType;
    sentAt: Date;
    isRead: boolean;
    payload: any;
}