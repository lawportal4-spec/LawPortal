export interface BlogPostMeta {
  slug: string;
  titleAr: string;
  titleEn: string;
  excerptAr: string;
  excerptEn: string;
  publishDate: string;
}

/** A few real, honestly-hedged educational articles — general information, not legal advice.
 * Each links back to the relevant service category so a reader can go straight from "I have a
 * question" to "I want to submit a real request." */
export const blogPosts: BlogPostMeta[] = [
  {
    slug: "power-of-attorney-guide",
    titleAr: "دليل الوكالة الشرعية: الفردية والشركات، وما الفرق بينهما؟",
    titleEn: "A guide to power of attorney: individual vs. corporate, and what separates them",
    excerptAr: "نظرة عامة على أنواع الوكالة الشرعية في السعودية، ومتى تحتاج كل نوع، وما الذي يميز وكالة الشركات عن الوكالة الفردية.",
    excerptEn: "An overview of power-of-attorney types in Saudi Arabia, when each is needed, and what separates a corporate PoA from an individual one.",
    publishDate: "2026-06-15",
  },
  {
    slug: "inheritance-basics",
    titleAr: "أساسيات المواريث في النظام السعودي: من أين تبدأ؟",
    titleEn: "Inheritance basics under Saudi law: where do you start?",
    excerptAr: "الخطوات الأولى العامة لأي وريث بعد الوفاة، وأين يقع كل إجراء ضمن العملية الأوسع.",
    excerptEn: "The general first steps for any heir after a death, and where each step sits within the wider process.",
    publishDate: "2026-07-02",
  },
  {
    slug: "execution-process-overview",
    titleAr: "ماذا تتوقع من مرحلة التنفيذ بعد صدور الحكم؟",
    titleEn: "What to expect from the execution phase after a judgment is issued",
    excerptAr: "لمحة عامة عن الفرق بين صدور الحكم وتنفيذه الفعلي، والخيارات المتاحة عند تعثّر التنفيذ.",
    excerptEn: "A general look at the difference between a judgment being issued and actually enforced, and the options available when enforcement stalls.",
    publishDate: "2026-07-20",
  },
];
