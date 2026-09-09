export const vehicleQueryKeys = {
  makes: () => ["vehicles", "makes"] as const,
  models: (makeId: number) => ["vehicles", "models", makeId] as const,
}
