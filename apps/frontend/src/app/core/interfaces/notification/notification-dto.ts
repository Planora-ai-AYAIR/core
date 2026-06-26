import { NotificationType } from "./notification-type";

export interface NotificationDto {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  link: string | null;
  data: string | null;
  createdAt: string;
  isRead: boolean;
}
