export function jwtDecode(token: string): Record<string, string> {
  const [, payload] = token.split('.')
  const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
  return JSON.parse(json) as Record<string, string>
}
