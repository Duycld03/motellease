import { describe, it, expect } from 'vitest'
import fs from 'node:fs'
import path from 'node:path'

function getNestedKeys(obj: any, prefix = ''): string[] {
  let keys: string[] = []
  for (const k in obj) {
    const full = prefix ? `${prefix}.${k}` : k
    if (typeof obj[k] === 'object' && obj[k] !== null && !Array.isArray(obj[k])) {
      keys = keys.concat(getNestedKeys(obj[k], full))
    } else {
      keys.push(full)
    }
  }
  return keys
}

function getByPath(obj: any, pathStr: string): any {
  const parts = pathStr.split('.')
  let cur = obj
  for (const p of parts) {
    if (cur === undefined || cur === null) return undefined
    cur = cur[p]
  }
  return cur
}

function walkDir(dir: string): string[] {
  let results: string[] = []
  const list = fs.readdirSync(dir)
  list.forEach(file => {
    const fullPath = path.join(dir, file)
    const stat = fs.statSync(fullPath)
    if (stat && stat.isDirectory()) {
      if (!['node_modules', '.nuxt', '.output', 'locales', 'i18n', 'test', 'coverage'].includes(file)) {
        results = results.concat(walkDir(fullPath))
      }
    } else if (file.endsWith('.vue') || (file.endsWith('.ts') && !file.endsWith('.d.ts'))) {
      results.push(fullPath)
    }
  })
  return results
}

describe('i18n Completeness & Quality Guard Tests', () => {
  const frontendDir = path.resolve(__dirname, '../..')
  const viPath = path.join(frontendDir, 'locales/vi.json')
  const enPath = path.join(frontendDir, 'locales/en.json')
  const viI18nPath = path.join(frontendDir, 'i18n/locales/vi.json')
  const enI18nPath = path.join(frontendDir, 'i18n/locales/en.json')

  const vi = JSON.parse(fs.readFileSync(viPath, 'utf8'))
  const en = JSON.parse(fs.readFileSync(enPath, 'utf8'))
  const viI18n = JSON.parse(fs.readFileSync(viI18nPath, 'utf8'))
  const enI18n = JSON.parse(fs.readFileSync(enI18nPath, 'utf8'))

  it('enforces 100% key symmetry between vi.json and en.json', () => {
    const viKeys = getNestedKeys(vi)
    const enKeys = getNestedKeys(en)

    const missingInEn = viKeys.filter(k => !enKeys.includes(k))
    const missingInVi = enKeys.filter(k => !viKeys.includes(k))

    expect(missingInEn, `Keys present in vi.json but missing in en.json: ${missingInEn.join(', ')}`).toEqual([])
    expect(missingInVi, `Keys present in en.json but missing in vi.json: ${missingInVi.join(', ')}`).toEqual([])
    expect(viKeys.length).toBeGreaterThan(500)
    expect(enKeys.length).toBe(viKeys.length)
  })

  it('synchronizes locales/ with i18n/locales/ identically', () => {
    expect(vi).toEqual(viI18n)
    expect(en).toEqual(enI18n)
  })

  it('contains full translation coverage for all core and supplementary enums', () => {
    const expectedEnums = [
      'RoomStatus',
      'DepositStatus',
      'LeaseStatus',
      'BillStatus',
      'PaymentStatus',
      'RequestStatus',
      'BoardingHouseType',
      'ListingStatus',
      'MaintenanceStatus',
      'MaintenanceCategory',
      'TaskPriority',
      'WorkTaskStatus',
      'UserRole',
      'Gender',
      'BusinessType',
      'PaymentPurpose',
      'PaymentProvider',
      'ImageOwnerType',
      'ReportTargetType',
      'ReportStatus',
      'NotificationType',
      'PaymentReturnOutcome',
      'OtpPurpose',
      'OtpVerifyResult',
      'PaymentConfirmation',
    ]

    for (const enumName of expectedEnums) {
      expect(vi.enums[enumName], `vi.json missing enum group "${enumName}"`).toBeDefined()
      expect(en.enums[enumName], `en.json missing enum group "${enumName}"`).toBeDefined()
      expect(Object.keys(vi.enums[enumName]).length).toBeGreaterThan(0)
      expect(Object.keys(vi.enums[enumName])).toEqual(Object.keys(en.enums[enumName]))
    }
  })

  it('ensures no Vue template contains unexpanded mustache {{ in HTML attributes', () => {
    const vueFiles = walkDir(frontendDir).filter(f => f.endsWith('.vue'))
    const invalidAttributes: { file: string; line: number; text: string }[] = []

    for (const file of vueFiles) {
      const content = fs.readFileSync(file, 'utf8')
      const lines = content.split('\n')
      lines.forEach((line, idx) => {
        const m = line.match(/\s[a-zA-Z0-9_\-]+="[^"]*\{\{[^"]*"/)
        if (m) {
          invalidAttributes.push({
            file: path.relative(frontendDir, file),
            line: idx + 1,
            text: line.trim(),
          })
        }
      })
    }

    expect(
      invalidAttributes,
      `Found literal "{{ ... }}" inside HTML attributes without v-bind (:). These render as literal text to users:\n` +
        invalidAttributes.map(a => `  ${a.file}:${a.line} -> ${a.text}`).join('\n')
    ).toEqual([])
  })

  it('validates that every static $t / t call in the codebase exists in both locales', () => {
    const codeFiles = walkDir(frontendDir)
    const staticTRegex = /(?:(?<=\W)|^)(?:\$t|t)\(\s*[\x27"]([a-zA-Z0-9_\-\.]+)[\x27"]/g

    const missingKeys: { file: string; key: string; locale: string }[] = []

    for (const file of codeFiles) {
      const content = fs.readFileSync(file, 'utf8')
      let m: RegExpExecArray | null
      while ((m = staticTRegex.exec(content)) !== null) {
        const key = m[1]
        if (key.includes('${') || key.length < 2) continue

        if (getByPath(vi, key) === undefined) {
          missingKeys.push({ file: path.relative(frontendDir, file), key, locale: 'vi' })
        }
        if (getByPath(en, key) === undefined) {
          missingKeys.push({ file: path.relative(frontendDir, file), key, locale: 'en' })
        }
      }
    }

    expect(
      missingKeys,
      `Found static $t('...') calls referencing non-existent keys in locales:\n` +
        missingKeys.map(k => `  ${k.file}: "${k.key}" (missing in ${k.locale}.json)`).join('\n')
    ).toEqual([])
  })

  it('validates that all filter tab arrays with dynamic $t(`enums.EnumType.${var}`) have 100% valid enum keys', () => {
    const vueFiles = walkDir(frontendDir).filter(f => f.endsWith('.vue'))
    const invalidEnumTabs: { file: string; line: number; enumType: string; item: string }[] = []

    for (const file of vueFiles) {
      const content = fs.readFileSync(file, 'utf8')
      const lines = content.split('\n')

      lines.forEach((line, idx) => {
        const tm = line.match(/[\$]?t\(\s*[`\x27"]enums\.([a-zA-Z0-9]+)\.\$\{([^}]+)\}[`\x27"]\s*\)/)
        if (tm) {
          const enumType = tm[1]
          const varName = tm[2].trim()

          for (let prevIdx = Math.max(0, idx - 15); prevIdx <= idx; prevIdx++) {
            const vforMatch = lines[prevIdx].match(new RegExp(`v-for="\\s*${varName}\\s+in\\s+\\[([^\\]]+)\\]"`))
            if (vforMatch) {
              const items = vforMatch[1].split(',').map(s => s.trim().replace(/^[\x27"]|[\x27"]$/g, ''))
              for (const item of items) {
                if (!item || item.startsWith('//')) continue
                if (!vi.enums[enumType]?.[item] || !en.enums[enumType]?.[item]) {
                  invalidEnumTabs.push({
                    file: path.relative(frontendDir, file),
                    line: idx + 1,
                    enumType,
                    item,
                  })
                }
              }
            }
          }
        }
      })
    }

    expect(
      invalidEnumTabs,
      `Found status filter loops referencing invalid enum values:\n` +
        invalidEnumTabs.map(e => `  ${e.file}:${e.line} -> enums.${e.enumType}.${e.item}`).join('\n')
    ).toEqual([])
  })

  it('ensures all navigateTo calls use localePath or switchLocalePath to prevent language loss', () => {
    const codeFiles = walkDir(frontendDir).filter(f => !f.includes('middleware/'))
    const unlocalizedNavigates: { file: string; line: number; text: string }[] = []

    for (const file of codeFiles) {
      const content = fs.readFileSync(file, 'utf8')
      const lines = content.split('\n')
      lines.forEach((line, idx) => {
        if (line.includes('navigateTo(')) {
          // If it contains raw string literal or template literal starting with / without localePath or switchLocalePath
          const hasRawPath = /navigateTo\(\s*[`\x27"]\/[^`\x27"]*[`\x27"]\s*\)/.test(line) ||
                            /navigateTo\(\s*`\/[^`]*`\s*\)/.test(line)
          if (hasRawPath && !line.includes('localePath') && !line.includes('targetPath')) {
            unlocalizedNavigates.push({
              file: path.relative(frontendDir, file),
              line: idx + 1,
              text: line.trim(),
            })
          }
        }
      })
    }

    expect(
      unlocalizedNavigates,
      `Found raw unlocalized navigateTo('/...') calls without localePath(...). These will reset the current language:\n` +
        unlocalizedNavigates.map(n => `  ${n.file}:${n.line} -> ${n.text}`).join('\n')
    ).toEqual([])
  })
})
