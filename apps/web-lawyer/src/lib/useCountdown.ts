import { useEffect, useState } from "react";

/** Seconds left until a code may be resent, and a way to start it over. Starts running at
 * once unless `startAt` says otherwise (0 = available immediately). */
export function useCountdown(seconds: number, startAt = seconds): [number, () => void] {
  const [left, setLeft] = useState(startAt);
  useEffect(() => {
    if (left <= 0) return;
    const timer = setTimeout(() => setLeft((s) => s - 1), 1000);
    return () => clearTimeout(timer);
  }, [left]);
  return [left, () => setLeft(seconds)];
}
