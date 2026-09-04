export type ReminderStatus = "Pending" | "Completed" | "Cancelled"

export interface Reminder {
  id: string
  title: string
  message: string | null
  remindAt: string
  customerId: string | null
  customerName: string | null
  rentalId: string | null
  status: ReminderStatus
  createdAt: string
}

export interface CreateReminderRequest {
  title: string
  message?: string | null
  remindAt: string
  customerId?: string | null
  rentalId?: string | null
}

export interface UpdateReminderRequest extends CreateReminderRequest {
  status: ReminderStatus
}
