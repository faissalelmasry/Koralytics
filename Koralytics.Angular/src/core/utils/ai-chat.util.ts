/**
 * Strips internal metadata tags (such as ##PLAYER_CONTEXT:4## or ##TAG:VALUE##) from AI chatbot responses
 * so they are never displayed in the frontend UI.
 */
export function cleanAiBotResponse(text: string): string {
  if (!text) return '';
  return text
    .replace(/##PLAYER_CONTEXT:[^#]*##/gi, '')
    .replace(/##[A-Z_]+(?::[^#]*)?##/gi, '')
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}
