(() => {
    "use strict";

    const translations = {
        "QAMISHLO DELIVERY": "قامشلو للتوصيل",
        "Fast • Reliable • Professional": "سريع • موثوق • احترافي",
        "Welcome back 👋": "أهلاً بعودتك 👋",
        "Sign in to manage your assigned deliveries.": "سجّل الدخول لإدارة طلبات التوصيل المسندة إليك.",
        "Username": "اسم المستخدم",
        "Password": "كلمة المرور",
        "Enter your username": "أدخل اسم المستخدم",
        "Enter your password": "أدخل كلمة المرور",
        "Sign In": "تسجيل الدخول",
        "Install Qamishlo Delivery": "تثبيت تطبيق قامشلو للتوصيل",
        "Powered by": "بدعم من",
        "Today's Overview": "ملخص اليوم",
        "A quick look at your delivery activity.": "نظرة سريعة على نشاط التوصيل الخاص بك.",
        "Assigned Orders": "الطلبات المسندة",
        "Active Deliveries": "عمليات التوصيل النشطة",
        "Delivered Today": "تم توصيلها اليوم",
        "Today's Total": "إجمالي اليوم",
        "Refresh dashboard and orders": "تحديث لوحة التحكم والطلبات",
        "Daily Reports & Delivered Orders": "التقارير اليومية والطلبات المسلّمة",
        "My Orders": "طلباتي",
        "View order details, update status, or prepare to transfer an order.": "اعرض تفاصيل الطلب أو حدّث حالته أو جهّز لتحويله إلى عامل آخر.",
        "Filter orders": "تصفية الطلبات",
        "All orders": "كل الطلبات",
        "New": "جديد",
        "Preparing": "قيد التحضير",
        "Ready": "جاهز",
        "On the way": "في الطريق",
        "Order number": "رقم الطلب",
        "Customer:": "الزبون:",
        "Phone:": "الهاتف:",
        "Type:": "النوع:",
        "Zone:": "المنطقة:",
        "Address:": "العنوان:",
        "Order time:": "وقت الطلب:",
        "Notes:": "ملاحظات:",
        "Customer Location": "موقع الزبون",
        "GPS coordinates": "إحداثيات GPS",
        "Navigate with Waze": "التنقل باستخدام Waze",
        "Copy location": "نسخ الموقع",
        "Customer location not provided": "لم يتم تحديد موقع الزبون",
        "Order total": "إجمالي الطلب",
        "WhatsApp customer": "مراسلة الزبون عبر واتساب",
        "Accept order": "قبول الطلب",
        "The restaurant is preparing this order. The pickup button becomes available when the restaurant marks it Ready.": "المطعم يحضّر هذا الطلب. سيظهر زر الاستلام عندما يغيّر المطعم حالته إلى جاهز.",
        "Pick up & start delivery": "استلام الطلب وبدء التوصيل",
        "Confirm delivery": "تأكيد التسليم",
        "View details": "عرض التفاصيل",
        "Transfer Order to Another Worker": "تحويل الطلب إلى عامل آخر",
        "No orders in this category": "لا توجد طلبات في هذه الفئة",
        "Choose another status to see your orders.": "اختر حالة أخرى لعرض طلباتك.",
        "All Orders Taken Today": "جميع الطلبات المسندة إليك اليوم",
        "Complete list of every order assigned to your account during today's business day, including completed orders assigned to you today.": "القائمة الكاملة لكل الطلبات المسندة إلى حسابك خلال يوم العمل الحالي، بما فيها الطلبات المكتملة.",
        "No orders recorded today": "لا توجد طلبات مسجلة اليوم",
        "Orders assigned to your account today will appear here.": "ستظهر هنا الطلبات التي أُسندت إلى حسابك اليوم.",
        "Logout": "تسجيل الخروج",
        "Order details": "تفاصيل الطلب",
        "Close details": "إغلاق التفاصيل",
        "Transfer delivery order": "تحويل طلب التوصيل",
        "No other active delivery workers are available to receive this order.": "لا يوجد عمال توصيل نشطون آخرون لاستلام هذا الطلب.",
        "Close": "إغلاق",
        "Choose the receiving worker": "اختر عامل التوصيل المستلم",
        "Select a worker": "اختر عاملاً",
        "Note (optional)": "ملاحظة (اختياري)",
        "Confirm transfer": "تأكيد التحويل",
        "Cancel": "إلغاء",
        "Daily report and delivered order history": "التقرير اليومي وسجل الطلبات المسلّمة",
        "Back to dashboard": "العودة إلى لوحة التحكم",
        "Today's Report": "تقرير اليوم",
        "Delivered today": "تم توصيلها اليوم",
        "Today's total": "إجمالي اليوم",
        "Average per delivered order": "متوسط قيمة الطلب المسلّم",
        "IQD": "دينار عراقي",
        "IQD — based on today's API totals": "دينار عراقي — وفق إجماليات API لهذا اليوم",
        "Delivered Orders": "الطلبات المسلّمة",
        "No delivered orders were returned": "لم يتم إرجاع طلبات مسلّمة",
        "This can mean there are no delivered orders, or the current API endpoint returns active orders only.": "قد يعني ذلك عدم وجود طلبات مسلّمة، أو أن نقطة API الحالية تعرض الطلبات النشطة فقط.",
        "Delivered": "تم التسليم",
        "Customer:": "الزبون:",
        "Phone:": "الهاتف:",
        "Order date:": "تاريخ الطلب:",
        "Address:": "العنوان:",
        "End Shift / نهاية وقت العمل": "إنهاء الدوام",
        "On shift": "داخل الدوام",
        "Off shift": "خارج الدوام",
        "Delivery dashboard · On shift": "لوحة عامل التوصيل · داخل الدوام",
        "Worker:": "العامل:",
        "OutForDelivery": "في الطريق",
        "Cancelled": "ملغي",
        "Canceled": "ملغي",
        "All": "الكل",
        "No active orders assigned": "لا توجد طلبات نشطة مسندة إليك",
        "Orders could not be loaded": "تعذر تحميل الطلبات",
        "Your delivery-worker session has expired. Please sign in again.": "انتهت جلسة عامل التوصيل. يرجى تسجيل الدخول مجدداً.",
        "Use your browser menu and choose Add to Home screen or Install app.": "افتح قائمة المتصفح واختر إضافة إلى الشاشة الرئيسية أو تثبيت التطبيق."
    };

    const reverseTranslations = Object.fromEntries(
        Object.entries(translations).map(([english, arabic]) => [arabic, english])
    );
    const originalText = new WeakMap();
    const originalAttributes = new WeakMap();
    let activeLanguage = "en";

    function translateValue(value, language) {
        const lookup = language === "ar" ? translations : reverseTranslations;
        const trimmed = value.trim();
        if (!trimmed) return value;
        const translated = lookup[trimmed];
        if (!translated) return value;
        const leading = value.match(/^\s*/)?.[0] ?? "";
        const trailing = value.match(/\s*$/)?.[0] ?? "";
        return leading + translated + trailing;
    }

    function applyLanguage(language) {
        const lang = language === "ar" ? "ar" : "en";
        activeLanguage = lang;
        document.documentElement.lang = lang;
        document.documentElement.dir = lang === "ar" ? "rtl" : "ltr";
        document.body.dir = lang === "ar" ? "rtl" : "ltr";

        const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        let node;
        while ((node = walker.nextNode())) {
            const parentTag = node.parentElement?.tagName;
            if (parentTag === "SCRIPT" || parentTag === "STYLE" || parentTag === "NOSCRIPT") continue;
            if (!originalText.has(node)) originalText.set(node, node.nodeValue ?? "");
            node.nodeValue = translateValue(originalText.get(node), lang);
        }

        const attributeNames = ["placeholder", "aria-label", "title"];
        document.querySelectorAll("input,button,a,[aria-label],[title],form[onsubmit]").forEach(element => {
            if (!originalAttributes.has(element)) originalAttributes.set(element, {});
            const saved = originalAttributes.get(element);
            for (const attribute of attributeNames) {
                if (!element.hasAttribute(attribute)) continue;
                if (!(attribute in saved)) saved[attribute] = element.getAttribute(attribute);
                element.setAttribute(attribute, translateValue(saved[attribute] ?? "", lang));
            }
            if (element.hasAttribute("onsubmit")) {
                if (!("onsubmit" in saved)) saved.onsubmit = element.getAttribute("onsubmit");
                const originalSubmit = saved.onsubmit ?? "";
                const localizedSubmit = lang === "ar"
                    ? originalSubmit.replace("End your delivery shift now?", "هل تريد إنهاء دوامك الآن؟")
                    : originalSubmit.replace("هل تريد إنهاء دوامك الآن؟", "End your delivery shift now?");
                if (localizedSubmit !== element.getAttribute("onsubmit"))
                    element.setAttribute("onsubmit", localizedSubmit);
            }
        });

        const toggle = document.getElementById("qamishloLanguageToggle");
        if (toggle) {
            const toggleLabel = lang === "ar" ? "English" : "العربية";
            if (toggle.textContent !== toggleLabel) toggle.textContent = toggleLabel;
            toggle.setAttribute("aria-label", lang === "ar" ? "Switch to English" : "التبديل إلى العربية");
        }
        try { localStorage.setItem("qamishlo-delivery-language", lang); } catch { /* storage may be disabled */ }
    }

    function mountToggle() {
        const button = document.createElement("button");
        button.id = "qamishloLanguageToggle";
        button.type = "button";
        button.className = "language-toggle";
        button.addEventListener("click", () => {
            applyLanguage(activeLanguage === "ar" ? "en" : "ar");
        });
        document.body.appendChild(button);

        const observer = new MutationObserver(() => {
            applyLanguage(activeLanguage);
        });
        observer.observe(document.body, { childList: true, subtree: true });

        let saved = "en";
        try { saved = localStorage.getItem("qamishlo-delivery-language") || "en"; } catch { }
        applyLanguage(saved);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", mountToggle, { once: true });
    } else {
        mountToggle();
    }
})();
