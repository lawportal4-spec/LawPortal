// Run: node --experimental-strip-types packages/i18n/scripts/hijri.check.ts
import assert from "node:assert/strict";
import { hijriToIsoDate } from "../src/hijri.ts";

assert.equal(hijriToIsoDate("22/10/1443"), "2022-05-23"); // the example date in the reference video
assert.equal(hijriToIsoDate("1/10/1443"), "2022-05-02"); // Eid al-Fitr 1443
assert.equal(hijriToIsoDate("1/1/1446"), "2024-07-07"); // Islamic new year 1446
assert.equal(hijriToIsoDate(" 05/03/1450 "), hijriToIsoDate("5/3/1450")); // padding/whitespace tolerated
assert.equal(hijriToIsoDate("30/7/1445"), null); // Rajab 1445 has 29 days in Umm al-Qura
assert.equal(hijriToIsoDate("30/12/1445"), "2024-07-06"); // ...but Dhu al-Hijjah 1445 has 30
assert.equal(hijriToIsoDate("31/1/1445"), null);
assert.equal(hijriToIsoDate("2022-05-23"), null);
assert.equal(hijriToIsoDate(""), null);
console.log("hijri ok");

import { hijriMonthLayout } from "../src/hijri.ts";
// Shawwal 1443 began on Monday 2 May 2022 and had 29 days; Dhu al-Hijjah 1445 had 30.
assert.deepEqual(hijriMonthLayout(1443, 10), { days: 29, firstWeekday: 1 });
assert.equal(hijriMonthLayout(1445, 12)?.days, 30);
assert.equal(hijriMonthLayout(1200, 1), null);
console.log("hijri calendar ok");
