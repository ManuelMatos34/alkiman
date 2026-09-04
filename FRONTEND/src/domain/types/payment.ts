export type PaymentType = "Income" | "Expense"

export interface Payment {
  id: string
  rentalId: string | null
  amount: number
  type: PaymentType
  paymentDate: string
  stripeTransactionId: string | null
  createdAt: string
}

