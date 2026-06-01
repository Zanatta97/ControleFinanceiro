import api from './client'

export const getStatus = () => api.get('/Logs/status')
export const ativar = () => api.post('/Logs/ativar')
export const desativar = () => api.post('/Logs/desativar')
