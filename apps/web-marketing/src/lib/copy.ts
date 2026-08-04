export type Locale = "ar" | "en";

export const LAWYER_APP_URL = import.meta.env.PUBLIC_LAWYER_APP_URL ?? "http://localhost:5174";
export const CLIENT_APP_URL = import.meta.env.PUBLIC_CLIENT_APP_URL ?? "http://localhost:5173";

export const app = {
  ar: { name: "بوابة القانون" },
  en: { name: "Law Portal" },
};

export const nav = {
  ar: {
    home: "الرئيسية",
    services: "الخدمات",
    faq: "الأسئلة الشائعة",
    blog: "المدونة",
    joinAsLawyer: "انضم كمحامٍ",
    openApp: "افتح البوابة",
  },
  en: {
    home: "Home",
    services: "Services",
    faq: "FAQ",
    blog: "Blog",
    joinAsLawyer: "Join as a lawyer",
    openApp: "Open Law Portal",
  },
};

export const footer = {
  ar: {
    tagline: "بوابة القانون — الوصول إلى محامين مرخّصين في السعودية بثقة وشفافية.",
    linksHeading: "روابط",
    legalHeading: "قانوني",
    privacy: "سياسة الخصوصية",
    terms: "الشروط والأحكام",
    rights: "جميع الحقوق محفوظة.",
  },
  en: {
    tagline: "Law Portal — reach licensed lawyers in Saudi Arabia, with confidence and transparency.",
    linksHeading: "Links",
    legalHeading: "Legal",
    privacy: "Privacy Policy",
    terms: "Terms of Service",
    rights: "All rights reserved.",
  },
};

export const home = {
  ar: {
    heroEyebrow: "منصة الخدمات القانونية السعودية",
    heroTitle: "الوثيقة، بلغة واضحة",
    heroSubtitle:
      "استشر محامين مرخّصين من وزارة العدل، قدّم طلبات التوثيق والتأسيس، وتفاوض على عروض الأسعار — كل ذلك من مكان واحد.",
    ctaPrimary: "ابحث عن محامٍ",
    ctaSecondary: "انضم كمحامٍ",
    trustHeading: "لماذا بوابة القانون",
    trust1Title: "ترخيص موثّق",
    trust1Body: "كل محامٍ على المنصة يخضع لمراجعة إدارية لترخيصه من وزارة العدل قبل ظهوره في الدليل.",
    trust2Title: "دفع وفوترة نظاميّة",
    trust2Body: "فوترة ضريبية متوافقة مع متطلبات هيئة الزكاة والضريبة والجمارك (فاتورة)، ومحفظة مالية بسجل محاسبي مزدوج القيد.",
    trust3Title: "تفاوض مباشر",
    trust3Body: "في خدمات القضاء والتنفيذ، يمكنك استقبال عروض من عدة محامين والتفاوض على السعر قبل الالتزام.",
    categoriesHeading: "خدماتنا",
    categoriesSubheading: "خمس فئات تغطي معظم حاجاتك القانونية",
    finalCtaTitle: "جاهز للبدء؟",
    finalCtaBody: "أنشئ حسابك في دقائق وابدأ أول طلب لك.",
  },
  en: {
    heroEyebrow: "Saudi Arabia's legal services platform",
    heroTitle: "The document, made legible",
    heroSubtitle:
      "Consult MoJ-licensed lawyers, submit notarization and incorporation requests, and negotiate price offers — all from one place.",
    ctaPrimary: "Find a lawyer",
    ctaSecondary: "Join as a lawyer",
    trustHeading: "Why Law Portal",
    trust1Title: "Verified licensing",
    trust1Body: "Every lawyer on the platform has their Ministry of Justice licence reviewed by our team before appearing in the directory.",
    trust2Title: "Compliant payments & invoicing",
    trust2Body: "ZATCA Fatoora-aware tax invoicing and a double-entry-ledger wallet, so every riyal is accounted for.",
    trust3Title: "Real negotiation",
    trust3Body: "For judiciary & execution services, receive offers from multiple lawyers and negotiate price before committing.",
    categoriesHeading: "Our services",
    categoriesSubheading: "Five categories covering most of your legal needs",
    finalCtaTitle: "Ready to start?",
    finalCtaBody: "Create your account in minutes and submit your first request.",
  },
};

export const services = {
  ar: {
    title: "الخدمات",
    subtitle: "استكشف فئات الخدمات المتاحة على بوابة القانون وطريقة التسعير لكل منها.",
    pricingLabels: {
      PerLawyerFixed: "سعر ثابت لكل محامٍ",
      CompetitiveBidding: "عروض أسعار تنافسية",
      PredefinedCatalog: "سعر محدد مسبقًا",
      DetailsOnly: "تفاصيل مباشرة",
    } as Record<string, string>,
  },
  en: {
    title: "Services",
    subtitle: "Explore the service categories available on Law Portal and how each is priced.",
    pricingLabels: {
      PerLawyerFixed: "Per-lawyer fixed price",
      CompetitiveBidding: "Competitive bidding",
      PredefinedCatalog: "Predefined fixed price",
      DetailsOnly: "Direct submission",
    } as Record<string, string>,
  },
};

export const faqItems = {
  ar: [
    {
      q: "هل جميع المحامين على المنصة مرخّصون؟",
      a: "نعم. كل محامٍ يسجّل في بوابة القانون يخضع لمراجعة إدارية لترخيصه الصادر من وزارة العدل قبل أن يظهر في دليل البحث أو يستقبل طلبات.",
    },
    {
      q: "كيف يتم تحديد السعر؟",
      a: "يختلف حسب نوع الخدمة: الاستشارات القانونية بسعر ثابت يحدده كل محامٍ، خدمات التوثيق بسعر مُعلن مسبقًا، وخدمات القضاء والتنفيذ عبر عروض أسعار تنافسية يمكنك التفاوض عليها.",
    },
    {
      q: "هل الدفع آمن؟",
      a: "تتم جميع المدفوعات عبر بوابة دفع سعودية معتمدة، وتُحفظ الأموال في حساب ضمان حتى اكتمال الخدمة. تصدر فاتورة ضريبية متوافقة مع متطلبات هيئة الزكاة والضريبة والجمارك عند اكتمال كل عملية دفع.",
    },
    {
      q: "ماذا لو لم أكن راضيًا عن الخدمة؟",
      a: "يمكنك التواصل مع المحامي مباشرة لحل أي إشكال، وفي حال عدم قبول المحامي للطلب يُرد المبلغ بالكامل تلقائيًا إلى محفظتك أو بطاقتك.",
    },
    {
      q: "كيف أنضم كمحامٍ إلى المنصة؟",
      a: "سجّل من صفحة «انضم كمحامٍ»، أدخل بيانات ترخيصك من وزارة العدل، وسيقوم فريقنا بمراجعته. بعد الاعتماد يمكنك تحديد أسعارك وتخصصاتك والبدء باستقبال الطلبات.",
    },
  ],
  en: [
    {
      q: "Are all lawyers on the platform licensed?",
      a: "Yes. Every lawyer who registers on Law Portal has their Ministry of Justice licence reviewed by our team before they appear in search or can receive requests.",
    },
    {
      q: "How is pricing determined?",
      a: "It depends on the service type: legal consultations are priced by each lawyer, notarization services carry a published fixed price, and judiciary & execution services use competitive, negotiable offers.",
    },
    {
      q: "Is payment secure?",
      a: "All payments go through a licensed Saudi payment gateway, and funds are held in escrow until the service is complete. A ZATCA-compliant tax invoice is issued for every completed payment.",
    },
    {
      q: "What if I'm not satisfied with the service?",
      a: "You can reach the lawyer directly to resolve any issue, and if a lawyer declines a request, the full amount is automatically refunded to your wallet or card.",
    },
    {
      q: "How do I join as a lawyer?",
      a: "Register from the \"Join as a lawyer\" page, enter your Ministry of Justice licence details, and our team will review it. Once approved, you can set your pricing and specialties and start receiving requests.",
    },
  ],
};

export const joinAsLawyer = {
  ar: {
    title: "انضم كمحامٍ إلى بوابة القانون",
    subtitle: "وسّع نطاق ممارستك المهنية، واستقبل عملاء جددًا من جميع مناطق المملكة.",
    benefit1: "قاعدة عملاء أوسع",
    benefit1Body: "دليل بحث تصل من خلاله إلى عملاء يبحثون عن تخصصك بالذات، دون تكلفة تسويق.",
    benefit2: "تحصيل مضمون",
    benefit2Body: "تُحصَّل أموالك من حساب ضمان بعد اكتمال الخدمة مباشرة، دون متابعة عملاء للسداد.",
    benefit3: "مرونة في التسعير",
    benefit3Body: "أنت من يحدد أسعار استشاراتك، ويمكنك تفعيل خطة اشتراك لخفض نسبة العمولة والوصول إلى طلبات عروض الأسعار المُذاعة.",
    cta: "سجّل الآن",
    reviewNote: "سيقوم فريقنا بمراجعة ترخيصك من وزارة العدل قبل ظهور ملفك في الدليل — عادة في غضون يوم عمل واحد.",
  },
  en: {
    title: "Join Law Portal as a lawyer",
    subtitle: "Grow your practice and reach new clients across every region of Saudi Arabia.",
    benefit1: "A wider client base",
    benefit1Body: "A searchable directory that reaches clients looking for exactly your specialty, with no marketing spend.",
    benefit2: "Guaranteed collection",
    benefit2Body: "Funds are released from escrow the moment a service is complete — no chasing clients for payment.",
    benefit3: "Pricing flexibility",
    benefit3Body: "You set your own consultation prices, and can subscribe to a plan for a lower commission rate and access to broadcast bidding leads.",
    cta: "Register now",
    reviewNote: "Our team reviews your Ministry of Justice licence before your profile goes live in the directory — usually within one business day.",
  },
};
