/**
 * Decodifica o payload de um JWT (a parte do meio) sem validar a assinatura. Serve só para o frontend
 * ler os claims; a validação é do backend.
 * @throws se o token não tiver o formato header.payload.assinatura ou o payload não for JSON.
 */
export function jwtDecode(token: string): Record<string, string> {
  const [, payload] = token.split('.')
  // O payload vem em Base64URL; troca "-" e "_" pelos caracteres do Base64 comum, que é o que atob entende.
  // Valores não ASCII (ex.: nome com acento) saem com a codificação errada: atob devolve bytes, não UTF-8.
  const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
  return JSON.parse(json) as Record<string, string>
}
