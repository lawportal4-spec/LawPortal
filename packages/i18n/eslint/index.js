import { noPhysicalRtlClasses } from "./no-physical-rtl-classes.js";
import { noRawIntlFormatting } from "./no-raw-intl-formatting.js";

/** @type {import('eslint').ESLint.Plugin} */
const plugin = {
  meta: { name: "@law-portal/eslint-plugin-i18n" },
  rules: {
    "no-physical-rtl-classes": noPhysicalRtlClasses,
    "no-raw-intl-formatting": noRawIntlFormatting,
  },
};

export default plugin;

/** Flat-config recommended block: `...lawPortalI18n.configs.recommended` */
plugin.configs = {
  recommended: {
    plugins: { "law-portal-i18n": plugin },
    rules: {
      "law-portal-i18n/no-physical-rtl-classes": "error",
      "law-portal-i18n/no-raw-intl-formatting": "error",
    },
  },
};
