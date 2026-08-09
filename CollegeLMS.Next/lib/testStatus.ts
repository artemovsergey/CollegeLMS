import type { MyTestResultDto } from "@/types"

export type TestStatus = "passed" | "failed" | "none"

export function testStatusFor(
  testId: string | null,
  results: Map<string, MyTestResultDto>,
): TestStatus {
  if (!testId) return "none"
  const r = results.get(testId)
  if (!r) return "none"
  return r.passed ? "passed" : "failed"
}

export const TEST_STATUS_LABELS: Record<TestStatus, string> = {
  passed: "Тест: пройден",
  failed: "Тест: не пройден",
  none: "Тест",
}

export const TEST_STATUS_VARIANTS: Record<TestStatus, "default" | "secondary" | "outline" | "destructive"> = {
  passed: "default",
  failed: "secondary",
  none: "outline",
}
