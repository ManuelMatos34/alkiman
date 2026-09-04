export interface FinancialSummary {
  totalIncome: number
  totalExpenses: number
  netProfit: number
  averageRentalPrice: number
  totalContractedValue: number
}

export interface RentalsSummary {
  total: number
  active: number
  completed: number
  overdue: number
}

export interface AssetsSummary {
  total: number
  available: number
  rented: number
  maintenance: number
  utilizationRatePercent: number
  inventoryValue: number
}

export interface CategoryRevenue {
  categoryName: string
  revenue: number
  rentalsCount: number
}

export interface MonthlyFinancial {
  month: string
  income: number
  expenses: number
}

export interface OverdueRental {
  rentalId: string
  assetName: string
  customerName: string
  endDate: string
  daysOverdue: number
  totalPrice: number
}

export interface AssetRevenue {
  assetId: string
  assetName: string
  revenue: number
  rentalsCount: number
}

export interface CustomerRevenue {
  customerId: string
  customerName: string
  totalPaid: number
  rentalsCount: number
}

export interface ReportSummary {
  financial: FinancialSummary
  rentals: RentalsSummary
  assets: AssetsSummary
  revenueByCategory: CategoryRevenue[]
  revenueByMonth: MonthlyFinancial[]
  overdueRentals: OverdueRental[]
  topAssets: AssetRevenue[]
  topCustomers: CustomerRevenue[]
}
