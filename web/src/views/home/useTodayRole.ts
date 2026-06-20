import { ref, computed } from 'vue'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'

export type TodayRole = 'decision' | 'power' | 'light'
const OVERRIDE_KEY = 'today_home_role'

export const ROLE_LABELS: Record<TodayRole, string> = {
  decision: '经营视角',
  power: '处理视角',
  light: '快捷视角',
}

export function useTodayRole() {
  const userStore = useUserStore()
  const permissionStore = usePermissionStore()
  const override = ref<TodayRole | null>((localStorage.getItem(OVERRIDE_KEY) as TodayRole) || null)

  const detected = computed<TodayRole>(() => {
    const isBoss =
      userStore.roles.includes('admin') ||
      userStore.roles.includes('Admin') ||
      permissionStore.hasAdminAccess
    if (isBoss) return 'decision'
    const isPower =
      permissionStore.hasModuleAccess('finance', userStore.permissions) ||
      permissionStore.hasModuleAccess('cardflow', userStore.permissions) ||
      permissionStore.hasModuleAccess('quality', userStore.permissions)
    return isPower ? 'power' : 'light'
  })

  const role = computed<TodayRole>(() => override.value ?? detected.value)

  function setRole(r: TodayRole | null) {
    override.value = r
    if (r) localStorage.setItem(OVERRIDE_KEY, r)
    else localStorage.removeItem(OVERRIDE_KEY)
  }

  return { role, detected, override, setRole }
}
