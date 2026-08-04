// Bans direct Intl.NumberFormat / Intl.DateTimeFormat / .toLocaleString() / .toLocaleDateString()
// calls in app code. `Intl.NumberFormat('ar-SA')` silently returns Arabic-Indic digits and
// `toLocaleDateString('ar-SA')` silently returns the Hijri calendar — both wrong for this
// product's Western-numeral / Gregorian-by-default convention. Use @law-portal/i18n's
// formatNumber/formatCurrency/formatDate/formatDateTime instead, which pin both explicitly.

const BANNED_MEMBER_CALLS = new Set([
  "toLocaleString",
  "toLocaleDateString",
  "toLocaleTimeString",
]);

/** @type {import('eslint').Rule.RuleModule} */
export const noRawIntlFormatting = {
  meta: {
    type: "problem",
    docs: {
      description:
        "Disallow raw Intl.* / toLocaleString() calls; use the centralized formatters in @law-portal/i18n/format instead.",
    },
    schema: [],
    messages: {},
  },
  create(context) {
    return {
      NewExpression(node) {
        if (
          node.callee.type === "MemberExpression" &&
          node.callee.object.type === "Identifier" &&
          node.callee.object.name === "Intl" &&
          node.callee.property.type === "Identifier" &&
          (node.callee.property.name === "NumberFormat" || node.callee.property.name === "DateTimeFormat")
        ) {
          context.report({
            node,
            message:
              "Do not construct Intl.{{ctor}} directly — it defaults to Arabic-Indic digits / the Hijri calendar for ar-SA. Use formatNumber/formatCurrency/formatDate from @law-portal/i18n/format.",
            data: { ctor: node.callee.property.name },
          });
        }
      },
      CallExpression(node) {
        if (
          node.callee.type === "MemberExpression" &&
          node.callee.property.type === "Identifier" &&
          BANNED_MEMBER_CALLS.has(node.callee.property.name)
        ) {
          context.report({
            node,
            message:
              "Do not call .{{method}}() directly — it defaults to Arabic-Indic digits / the Hijri calendar for ar-SA. Use the centralized formatters in @law-portal/i18n/format.",
            data: { method: node.callee.property.name },
          });
        }
      },
    };
  },
};
