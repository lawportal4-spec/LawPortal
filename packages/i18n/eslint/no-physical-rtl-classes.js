// Fails a PR that uses a physical-direction Tailwind utility (pl-4, text-left, left-0, ...)
// instead of a logical one (ps-4, text-start, start-0, ...). This is the single highest-
// leverage RTL rule in the whole frontend: it's what lets `dir="rtl"`/`dir="ltr"` flip an
// entire layout for free, with zero duplicated CSS between the Arabic and English UI.

// Ordered [bannedPrefix, logicalReplacement] pairs, checked against the utility with any
// variant prefixes (md:, hover:, rtl:, dark:, ...) stripped off first.
const BANNED_PREFIXES = [
  ["pl-", "ps- (padding-inline-start)"],
  ["pr-", "pe- (padding-inline-end)"],
  ["ml-", "ms- (margin-inline-start)"],
  ["mr-", "me- (margin-inline-end)"],
  ["-ml-", "-ms- (negative margin-inline-start)"],
  ["-mr-", "-me- (negative margin-inline-end)"],
  ["left-", "start- (inset-inline-start)"],
  ["right-", "end- (inset-inline-end)"],
  ["text-left", "text-start"],
  ["text-right", "text-end"],
  ["border-l-", "border-s- (border-inline-start)"],
  ["border-r-", "border-e- (border-inline-end)"],
  ["rounded-l-", "rounded-s- (border-start-*-radius)"],
  ["rounded-r-", "rounded-e- (border-end-*-radius)"],
];

function bannedMatch(utility) {
  return BANNED_PREFIXES.find(([prefix]) => utility.startsWith(prefix));
}

function checkClassString(context, node, raw) {
  if (typeof raw !== "string") return;
  for (const token of raw.split(/\s+/).filter(Boolean)) {
    // Strip Tailwind variant prefixes (md:, hover:, dark:, rtl:, ...) before checking.
    const utility = token.includes(":") ? token.slice(token.lastIndexOf(":") + 1) : token;
    const match = bannedMatch(utility);
    if (match) {
      const [, suggestion] = match;
      context.report({
        node,
        message: `Physical-direction Tailwind class "{{token}}" breaks RTL layouts. Use ${suggestion} instead.`,
        data: { token },
      });
    }
  }
}

/** @type {import('eslint').Rule.RuleModule} */
export const noPhysicalRtlClasses = {
  meta: {
    type: "problem",
    docs: {
      description:
        "Disallow physical-direction Tailwind classes (pl-/pr-/left-/right-/text-left/text-right) in favor of logical properties so layouts flip automatically under dir=\"rtl\".",
    },
    schema: [],
    messages: {},
  },
  create(context) {
    return {
      JSXAttribute(node) {
        if (node.name.name !== "className" && node.name.name !== "class") return;
        const value = node.value;
        if (!value) return;

        if (value.type === "Literal") {
          checkClassString(context, node, value.value);
        } else if (value.type === "JSXExpressionContainer") {
          const expr = value.expression;
          if (expr.type === "Literal") {
            checkClassString(context, node, expr.value);
          } else if (expr.type === "TemplateLiteral") {
            for (const quasi of expr.quasis) {
              checkClassString(context, node, quasi.value.raw);
            }
          } else if (expr.type === "CallExpression" || expr.type === "ConditionalExpression") {
            // clsx(...)/cn(...) calls and ternaries: check every string-literal argument/branch.
            const literals = [];
            (function collect(n) {
              if (!n) return;
              if (n.type === "Literal" && typeof n.value === "string") literals.push(n);
              if (n.type === "TemplateLiteral") {
                for (const q of n.quasis) checkClassString(context, node, q.value.raw);
              }
              if (n.arguments) n.arguments.forEach(collect);
              if (n.consequent) collect(n.consequent);
              if (n.alternate) collect(n.alternate);
              if (n.properties) {
                for (const p of n.properties) {
                  if (p.key) collect(p.key);
                }
              }
            })(expr);
            for (const lit of literals) checkClassString(context, node, lit.value);
          }
        }
      },
    };
  },
};
