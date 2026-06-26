import { NotificationDto } from "./notification-dto";

export interface NotificationsPageDto {
  items: NotificationDto[];
  total: number;
  skip: number;
  take: number;
}