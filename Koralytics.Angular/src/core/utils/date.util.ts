/**
 * Utility functions for handling local date-time strings without UTC timezone shifts.
 */

/**
 * Formats a Date object or date-time string as a local ISO string (YYYY-MM-DDTHH:mm:ss)
 * preserving the local year, month, date, hours, minutes, and seconds.
 * Avoids using `.toISOString()` which converts local time to UTC time.
 */
export function formatToLocalISO(dateInput: Date | string | null | undefined): string {
  if (!dateInput) return '';

  if (typeof dateInput === 'string') {
    const trimmed = dateInput.trim();
    // If it's already a local ISO string like "2026-08-10T16:00" or "2026-08-10T16:00:00"
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2})?$/.test(trimmed)) {
      return trimmed.length === 16 ? `${trimmed}:00` : trimmed;
    }
    dateInput = new Date(trimmed);
  }

  if (isNaN(dateInput.getTime())) return '';

  const pad = (n: number) => (n < 10 ? '0' + n : n);
  const year = dateInput.getFullYear();
  const month = pad(dateInput.getMonth() + 1);
  const day = pad(dateInput.getDate());
  const hours = pad(dateInput.getHours());
  const minutes = pad(dateInput.getMinutes());
  const seconds = pad(dateInput.getSeconds());

  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
}
