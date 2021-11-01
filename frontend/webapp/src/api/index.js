import ky from 'ky'

const VUE_APP_BASE_API = process.env.VUE_APP_BASE_API || '/api';

const api = ky.create({ prefixUrl: VUE_APP_BASE_API, retry: { limit: 0 },timeout:false });

export function getQLTitle() {

  return api.get('Title').json()
}
export function getQLConfig(key) {

  return api.get('QLConfig?qlkey='+key).json()
}
export function  getConfigMain() {
    return api.get('Config').json()
}
export function SendSMS(body) {
  return api.post('SendSMS', { json: body }).json()
}
export function AutoCaptcha(body) {
  return api.post('AutoCaptcha', { json: body }).json()
}
export function VerifyCode(body) {
    return api.post('VerifyCode', { json: body }).json()
}
export function Upremarksapi(body) {
  return api.post('Upremarks', { json: body }).json()
}
export function getUserInfoAPI(qlid,qlkey) {
  const searchParams = new URLSearchParams()
  searchParams.set('qlid', qlid)
  searchParams.set('qlkey', qlkey)
  return api.get('User', { searchParams: searchParams }).json()
}
export function delAccountAPI(body) {
  return api.post('del', { json: body }).json()
}