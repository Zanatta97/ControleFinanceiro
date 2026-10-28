import api from './client'

/** Se o log completo de requisições está ligado (`dados.logAllRequests`). */
export const getStatus = () => api.get('/Logs/status')
/** Liga o log completo: toda requisição passa a ser gravada com corpo. Vale até a API reiniciar. */
export const ativar = () => api.post('/Logs/ativar')
/** Desliga o log completo: volta a gravar só erros (status ≥ 400), sem o corpo da requisição. */
export const desativar = () => api.post('/Logs/desativar')
