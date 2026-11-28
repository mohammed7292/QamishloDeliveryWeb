(function () {
  const CFG = window.QAMISHLO_DELIVERY_CONFIG;
  const API = CFG.API_BASE_URL;
  const AUTH_KEY = 'qamishlo_delivery_auth_v2';

  let auth = null;
  let activeOrders = [];
  let historyOrders = [];
  let todayStats = { deliveredorders: 0, totalAmount: 0 };
  let lastOrderIds = new Set();
  let pollTimer = null;
  let gpsTimer = null;
  let isOnShift = true;
  let transferOrderId = null;

  // Map state
  let map = null;
  let tileLayers = {};
  let currentLayer = null;
  let driverMarker = null;
  let custMarker = null;
  let restMarker = null;
  let routeLine = null;
  let driverPos = null;
  let focusedOrder = null;
  let currentBearing = 0;
  let compassActive = false;

  const SVG_WA = '<svg width="22" height="22" viewBox="0 0 32 32" fill="none" style="vertical-align:middle;margin-left:6px;"><circle cx="16" cy="16" r="16" fill="#25D366"/><path d="M23.5 19.3c-.4-.2-2.3-1.1-2.6-1.3-.4-.1-.6-.2-.9.2-.3.4-1 1.3-1.2 1.5-.2.2-.4.3-.8.1-.4-.2-1.6-.6-3-1.9-1.1-1-1.9-2.2-2.1-2.6-.2-.4 0-.6.2-.8.2-.2.4-.4.5-.7.2-.2.2-.4.3-.6.1-.2.1-.4 0-.6-.1-.2-.9-2.1-1.2-2.9-.3-.8-.6-.7-.9-.7h-.7c-.3 0-.7.1-1 .5-.4.4-1.4 1.3-1.4 3.2s1.4 3.7 1.6 4c.2.2 2.8 4.2 6.7 5.9.9.4 1.7.6 2.2.8.9.3 1.8.2 2.5.1.8-.1 2.3-.9 2.6-1.8.3-.9.3-1.7.2-1.8-.1-.2-.3-.3-.7-.5z" fill="#fff"/></svg>';
  const SVG_PHONE = '<svg width="20" height="20" viewBox="0 0 24 24" fill="#fff" style="vertical-align:middle;margin-left:6px;"><path d="M6.62 10.79a15.05 15.05 0 006.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1C10.61 21 3 13.39 3 4c0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"/></svg>';
  const SVG_WAZE = '<svg width="22" height="22" viewBox="0 0 32 32" fill="none" style="vertical-align:middle;margin-left:6px;"><circle cx="16" cy="16" r="16" fill="#33CCFF"/><path d="M24.2 17.5c.5-4.3-2.9-8-7.4-8-4.1 0-7.5 3-7.8 7-.1 1-.6 1.5-1.4 1.6-.4 0-.6.4-.4.8.7 1.2 2.1 2.1 3.7 2.4a2.2 2.2 0 004.2.5h2.2a2.2 2.2 0 004.3-.3c1.6-.7 2.4-2.1 2.6-4z" fill="#fff"/><circle cx="14.5" cy="14.5" r="1.2" fill="#14110F"/><circle cx="19.5" cy="14.5" r="1.2" fill="#14110F"/><path d="M14.5 17.3c1 1.1 3.2 1.1 4.3 0" stroke="#14110F" stroke-width="1.3" stroke-linecap="round"/></svg>';
  const SVG_MAP = '<svg width="20" height="20" viewBox="0 0 24 24" fill="#fff" style="vertical-align:middle;margin-left:6px;"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5a2.5 2.5 0 110-5 2.5 2.5 0 010 5z"/></svg>';

  const $ = (id) => document.getElementById(id);
  const fmt = (n) => Math.round(Number(n || 0)).toLocaleString('en-US') + ' د.ع';
  const todayStr = () => new Date().toISOString().slice(0, 10);
  function toWaNumber(raw) {
    let d = String(raw || '').replace(/[^0-9]/g, '');
    if (d.startsWith('00')) d = d.slice(2);
    if (d.startsWith('0') && d.length >= 10) d = '964' + d.slice(1);
    else if (d.startsWith('7') && d.length === 10) d = '964' + d;
    return d;
  }

  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('./service-worker.js').catch(() => {});
  }

  function showToast(msg) {
    const t = $('toast');
    t.textContent = msg;
    t.classList.remove('hidden');
    clearTimeout(showToast._tm);
    showToast._tm = setTimeout(() => t.classList.add('hidden'), 3200);
  }

  function playAlertBeep() {
    try {
      const ctx = new (window.AudioContext || window.webkitAudioContext)();
      [880, 1175, 880].forEach((freq, i) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = 'sine';
        osc.frequency.value = freq;
        gain.gain.value = 0.2;
        osc.connect(gain); gain.connect(ctx.destination);
        osc.start(ctx.currentTime + i * 0.16);
        osc.stop(ctx.currentTime + i * 0.16 + 0.14);
      });
    } catch (_) {}
  }

  async function notifyDriverOs(title, body) {
    try {
      if (!('Notification' in window) || Notification.permission !== 'granted') return;
      const reg = await navigator.serviceWorker?.ready;
      if (reg && reg.showNotification) {
        await reg.showNotification(title, { body, icon: './assets/icon-192.png', vibrate: [200, 100, 200] });
      } else {
        new Notification(title, { body, icon: './assets/icon-192.png' });
      }
    } catch (_) {}
  }

  async function enablePushNotifications(silent = false) {
    if (!('Notification' in window)) {
      if (!silent) showToast('المتصفح لا يدعم الإشعارات');
      return;
    }
    const perm = Notification.permission === 'granted' ? 'granted' : (silent ? 'default' : await Notification.requestPermission());
    if (perm !== 'granted') {
      if (!silent) showToast('يرجى السماح بالإشعارات من إعدادات المتصفح');
      return;
    }
    $('pushBtn').classList.add('on');
    $('pushBtn').textContent = '🔔✓';
    try {
      const reg = await navigator.serviceWorker?.ready;
      const cfg = await fetch(API + 'api/push/public-config').then((r) => r.json()).catch(() => null);
      if (cfg?.configured && cfg.firebaseConfig?.apiKey && cfg.vapidKey && reg && auth) {
        const [{ initializeApp, getApps }, { getMessaging, getToken }] = await Promise.all([
          import('https://www.gstatic.com/firebasejs/10.12.2/firebase-app.js'),
          import('https://www.gstatic.com/firebasejs/10.12.2/firebase-messaging.js')
        ]);
        const fbApp = getApps().length ? getApps()[0] : initializeApp(cfg.firebaseConfig);
        const messaging = getMessaging(fbApp);
        const token = await getToken(messaging, { vapidKey: cfg.vapidKey, serviceWorkerRegistration: reg }) || '';
        if (token) {
          await apiFetch('api/push/register', {
            method: 'POST',
            body: JSON.stringify({ audienceType: 'driver', targetID: auth.id, tokenOrEndpoint: token, platform: 'web' })
          });
        }
      }
      if (!silent) showToast('✅ تم تفعيل إشعارات الخلفية');
    } catch (_) {
      if (!silent) showToast('✅ تم تفعيل الإشعارات');
    }
  }

  function authHeaders() {
    return {
      'Content-Type': 'application/json',
      'X-Delivery-Worker-Username': auth?.username || '',
      'X-Delivery-Worker-Password': auth?.password || ''
    };
  }

  async function apiFetch(path, opts = {}) {
    const resp = await fetch(API + path, { ...opts, headers: { ...authHeaders(), ...(opts.headers || {}) } });
    const text = await resp.text();
    let data = {};
    try { data = text ? JSON.parse(text) : {}; } catch (_) {}
    if (!resp.ok) throw new Error(data.message || ('HTTP ' + resp.status));
    return data;
  }


  function shiftStorageKey() { return auth ? ('qamishlo_driver_shift_' + auth.id) : 'qamishlo_driver_shift_0'; }
  function applyShiftUi(on) {
    isOnShift = !!on;
    const btn = $('shiftBtn');
    if (btn) {
      btn.textContent = isOnShift ? '🟢 على الدوام' : '🔴 خارج الدوام';
      btn.classList.toggle('on', isOnShift);
    }
  }
  function loadSavedShiftState() {
    try {
      const saved = localStorage.getItem(shiftStorageKey());
      if (saved === '0' || saved === 'false') { applyShiftUi(false); return; }
      if (saved === '1' || saved === 'true') { applyShiftUi(true); return; }
    } catch (_) {}
    applyShiftUi(true);
  }

  function loadExpenses() {
    if (!auth) return [];
    try { return JSON.parse(localStorage.getItem('qamishlo_driver_exp_' + auth.id) || '[]'); } catch (_) { return []; }
  }
  function saveExpenses(list) {
    if (!auth) return;
    localStorage.setItem('qamishlo_driver_exp_' + auth.id, JSON.stringify(list));
  }

  function statusAr(s) {
    const m = {
      New: 'جديد', Preparing: 'قيد التحضير', Ready: 'جاهز للتوصيل',
      OutForDelivery: 'في الطريق للزبون', Delivered: 'تم التوصيل',
      Cancelled: 'ملغي', Rejected: 'مرفوض'
    };
    return m[s] || s || '';
  }

  function renderOrders() {
    $('kpiActive').textContent = activeOrders.length;
    $('kpiTodayTop').textContent = fmt(todayStats.totalAmount);
    const wrap = $('ordersList');
    if (!activeOrders.length) {
      wrap.innerHTML = '<div class="empty">لا توجد طلبات توصيل نشطة مسندة إليك حالياً 🛵</div>';
      return;
    }
    wrap.innerHTML = activeOrders.map((o) => {
      const id = o.orderID ?? o.OrderID;
      const num = o.orderNumber ?? o.OrderNumber ?? ('Q-' + id);
      const st = o.status ?? o.Status ?? '';
      const cname = o.customerName ?? o.CustomerName ?? 'زبون';
      const phone = o.customerPhone ?? o.CustomerPhone ?? '';
      const addr = o.deliveryAddress ?? o.DeliveryAddress ?? 'غير محدد';
      const notes = o.notes ?? o.Notes ?? '';
      const total = o.totalAmount ?? o.TotalAmount ?? 0;
      const lat = Number(o.customerLatitude ?? o.CustomerLatitude ?? 0);
      const lng = Number(o.customerLongitude ?? o.CustomerLongitude ?? 0);
      const items = o.items ?? o.Items ?? [];
      const bClass = st === 'OutForDelivery' ? 'out' : (st === 'Ready' ? 'ready' : '');
      const itemsHtml = items.length
        ? '<ul class="items-box">' + items.map((it) => `<li>• ${it.productName ?? it.ProductName} × ${it.quantity ?? it.Quantity}</li>`).join('') + '</ul>'
        : '';
      const wazeBtn = (lat && lng)
        ? `<a class="act-btn act-waze" style="display:flex;align-items:center;justify-content:center;" href="https://waze.com/ul?ll=${lat},${lng}&navigate=yes" target="_blank" rel="noopener">${SVG_WAZE}<span>Waze</span></a>`
        : '';
      return `
        <div class="order-card">
          <div class="order-head">
            <span class="order-num">طلب ${num}</span>
            <span class="badge ${bClass}">${statusAr(st)}</span>
          </div>
          <div class="meta-row">👤 <strong>${cname}</strong> ${phone ? `— <span dir="ltr" style="color:#93C5FD;font-weight:800">${phone}</span>` : ''}</div>
          ${phone ? `<div style="display:flex;gap:8px;margin:8px 0 10px;">
            <a class="act-btn" style="flex:1;background:#15803D;padding:10px;font-size:14px;display:flex;align-items:center;justify-content:center;" href="https://wa.me/${toWaNumber(phone)}" target="_blank" rel="noopener">${SVG_WA}<span>واتساب</span></a>
            <a class="act-btn" style="flex:1;background:#1D4ED8;padding:10px;font-size:14px;display:flex;align-items:center;justify-content:center;" href="tel:${phone}">${SVG_PHONE}<span>اتصال</span></a>
          </div>` : ''}
          <div class="meta-row">📍 ${addr}</div>
          ${notes ? `<div class="meta-row">📝 ${notes}</div>` : ''}
          ${itemsHtml}
          <div class="total-line">💵 الإجمالي: ${fmt(total)}</div>
          <div class="btn-row">
            ${st !== 'OutForDelivery' ? `<button class="act-btn act-blue" onclick="window._qSetStatus(${id},'OutForDelivery')">🚀 انطلاق للتوصيل</button>` : ''}
            <button class="act-btn act-green" onclick="window._qSetStatus(${id},'Delivered')">✅ تم التوصيل</button>
            <button class="act-btn act-orange" style="display:flex;align-items:center;justify-content:center;" onclick="window._qOpenMap(${id})">${SVG_MAP}<span>الخريطة</span></button>
            ${wazeBtn}
            <button class="act-btn act-dark" onclick="window._qTransfer(${id})">🔄 نقل الطلب لسائق</button>
          </div>
        </div>`;
    }).join('');
  }

  function renderSalesAndExpenses() {
    const exps = loadExpenses();
    const tDate = todayStr();
    const todayExp = exps.filter((e) => e.date === tDate).reduce((s, e) => s + Number(e.amount || 0), 0);
    const allExp = exps.reduce((s, e) => s + Number(e.amount || 0), 0);
    const allAmt = historyOrders.reduce((s, o) => s + Number(o.totalAmount ?? o.TotalAmount ?? 0), 0);
    const netCash = Math.max(0, Number(todayStats.totalAmount || 0) - todayExp);

    $('sTodayAmt').textContent = fmt(todayStats.totalAmount);
    $('sTodayCnt').textContent = todayStats.deliveredorders || 0;
    $('sTodayExp').textContent = fmt(todayExp);
    $('sNetCash').textContent = fmt(netCash);
    $('sAllAmt').textContent = fmt(allAmt);
    $('sAllCnt').textContent = historyOrders.length;
    $('eTodayTotal').textContent = fmt(todayExp);
    $('eAllTotal').textContent = fmt(allExp);

    $('historyList').innerHTML = historyOrders.length
      ? historyOrders.slice(0, 30).map((o) => {
          const id = o.orderID ?? o.OrderID;
          const num = o.orderNumber ?? o.OrderNumber ?? ('Q-' + id);
          const cname = o.customerName ?? o.CustomerName ?? 'زبون';
          const amt = o.totalAmount ?? o.TotalAmount ?? 0;
          const dt = (o.orderDate ?? o.OrderDate ?? '').toString().replace('T', ' ').slice(0, 16);
          return `<div class="exp-item"><div><strong>${num}</strong> — ${cname}<br/><small style="color:#B8ACA4">${dt}</small></div><strong style="color:#4ADE80">${fmt(amt)}</strong></div>`;
        }).join('')
      : '<div class="empty">لا توجد طلبات مسلّمة بعد.</div>';

    $('expensesList').innerHTML = exps.length
      ? exps.map((e, idx) => `
          <div class="exp-item">
            <div><strong>${e.title}</strong><br/><small style="color:#B8ACA4">${e.date} • ${e.time || ''}</small></div>
            <div style="display:flex;align-items:center;gap:10px;">
              <strong style="color:#F87171">${fmt(e.amount)}</strong>
              <button class="pill-btn danger" onclick="window._qDelExp(${idx})">حذف</button>
            </div>
          </div>`).join('')
      : '<div class="empty">لم تسجل أي مصاريف بعد.</div>';
  }

  async function refreshAll(silent = false) {
    if (!auth) return;
    try {
      const [ordRes, statsRes, histRes] = await Promise.all([
        apiFetch(`api/DeliveryWorkers/${auth.id}/orders`),
        apiFetch(`api/DeliveryWorkers/${auth.id}/today-stats`).catch(() => ({ deliveredorders: 0, totalAmount: 0 })),
        apiFetch(`api/DeliveryWorkers/${auth.id}/history?pageSize=100`).catch(() => ({ orders: [] }))
      ]);
      const list = ordRes.orders || ordRes.Orders || [];
      const newIds = new Set(list.map((x) => x.orderID ?? x.OrderID));
      if (lastOrderIds.size > 0) {
        for (const id of newIds) {
          if (!lastOrderIds.has(id)) {
            playAlertBeep();
            showToast('🔔 وصلك طلب توصيل جديد!');
            notifyDriverOs('🛵 طلب توصيل جديد — قامشلو', 'تم إسناد طلب جديد لك، افتح التطبيق للتفاصيل.');
            break;
          }
        }
      }
      lastOrderIds = newIds;
      activeOrders = list;
      todayStats = {
        deliveredorders: statsRes.deliveredorders ?? statsRes.Deliveredorders ?? 0,
        totalAmount: statsRes.totalAmount ?? statsRes.TotalAmount ?? 0
      };
      historyOrders = histRes.orders || histRes.Orders || [];
      $('syncLbl').textContent = 'آخر تحديث: ' + new Date().toLocaleTimeString('ar-IQ', { hour: '2-digit', minute: '2-digit' });
      renderOrders();
      renderSalesAndExpenses();
      if (!silent) showToast('✅ تم تحديث البيانات');
    } catch (ex) {
      $('syncLbl').textContent = 'تعذر الاتصال مؤقتاً';
      if (!silent) showToast('⚠️ ' + ex.message);
    }
  }

  // Global actions
  window._qSetStatus = async function (orderId, status) {
    if (status === 'Delivered' && !confirm('تأكيد تسليم الطلب #' + orderId + ' للزبون؟')) return;
    try {
      await apiFetch(`api/DeliveryWorkers/${auth.id}/orders/${orderId}/status`, {
        method: 'POST',
        body: JSON.stringify({ Status: status })
      });
      showToast(status === 'Delivered' ? '✅ تم تسليم الطلب بنجاح' : '🚀 تم تحديث الحالة: في الطريق');
      await refreshAll(true);
    } catch (ex) { showToast('❌ ' + ex.message); }
  };

  window._qTransfer = async function (orderId) {
    try {
      const res = await apiFetch(`api/deliveryworkers/${auth.id}/transfer-targets`);
      const targets = res.workers || res.targets || [];
      if (!targets.length) { showToast('لا يوجد سائق آخر متاح للتحويل حالياً'); return; }
      transferOrderId = orderId;
      $('transferTitle').textContent = `🔄 اختر عامل التوصيل لنقل الطلب #${orderId}`;
      $('transferDriversList').innerHTML = targets.map((t) => {
        const tid = t.deliveryWorkerID ?? t.DeliveryWorkerID;
        const tname = t.fullName ?? t.FullName ?? 'سائق';
        const onShift = t.isOnShift ?? t.IsOnShift;
        return `<button class="driver-opt" onclick="window._qConfirmTransfer(${tid}, '${tname.replace(/'/g, '')}')">
          <span>🛵 ${tname}</span>
          <span style="font-size:12px;color:${onShift ? '#86EFAC' : '#D1D5DB'}">${onShift ? '🟢 على الدوام' : '⚪ متاح'}</span>
        </button>`;
      }).join('');
      $('transferModal').classList.remove('hidden');
    } catch (ex) { showToast('❌ ' + ex.message); }
  };

  window._qConfirmTransfer = async function (targetId, targetName) {
    $('transferModal').classList.add('hidden');
    if (!transferOrderId) return;
    try {
      await apiFetch(`api/deliveryworkers/${auth.id}/orders/${transferOrderId}/transfer`, {
        method: 'POST',
        body: JSON.stringify({ NewWorkerID: targetId, Username: auth.username, Password: auth.password })
      });
      showToast(`✅ تم نقل الطلب إلى ${targetName}`);
      await refreshAll(true);
    } catch (ex) { showToast('❌ ' + ex.message); }
  };

  window._qDelExp = function (idx) {
    const exps = loadExpenses();
    exps.splice(idx, 1);
    saveExpenses(exps);
    renderSalesAndExpenses();
    showToast('تم حذف المصروف');
  };

  // Map with Arabic Google Streets, Satellite, & Qamishlo Dark (CSS inverted Google Arabic — ZERO API KEY!)
  function ensureMap() {
    if (map) { setTimeout(() => map.invalidateSize(), 120); return; }
    map = L.map('qamishloMap', { zoomControl: true, rotate: true, touchRotate: true, rotateControl: { closeOnZeroBearing: false }, bearing: 0 }).setView([CFG.RESTAURANT_LAT, CFG.RESTAURANT_LNG], 15);
    tileLayers = {
      streets: L.tileLayer('https://mt{s}.google.com/vt/lyrs=m&hl=ar&x={x}&y={y}&z={z}', { subdomains: ['0', '1', '2', '3'], maxZoom: 20 }),
      sat: L.tileLayer('https://mt{s}.google.com/vt/lyrs=y&hl=ar&x={x}&y={y}&z={z}', { subdomains: ['0', '1', '2', '3'], maxZoom: 20 }),
      dark: L.tileLayer('https://mt{s}.google.com/vt/lyrs=m&hl=ar&x={x}&y={y}&z={z}', { subdomains: ['0', '1', '2', '3'], maxZoom: 20 })
    };
    currentLayer = tileLayers.streets.addTo(map);
    restMarker = L.marker([CFG.RESTAURANT_LAT, CFG.RESTAURANT_LNG]).addTo(map).bindPopup('🍔 مطعم قامشلو');
  }

  function maneuverAr(step) {
    const type = step?.maneuver?.type || '';
    const mod = step?.maneuver?.modifier || '';
    const name = step?.name ? ` (${step.name})` : '';
    if (type === 'depart') return `🚀 انطلق للأمام${name}`;
    if (type === 'arrive') return '🏁 وصلت إلى موقع الزبون';
    if (type === 'roundabout' || type === 'rotary') return `🔄 ادخل الدوار ثم اخرج${name}`;
    if (mod.includes('right')) return `➡️ انعطف يميناً${name}`;
    if (mod.includes('left')) return `⬅️ انعطف يساراً${name}`;
    if (mod.includes('uturn')) return `↩️ استدر للخلف${name}`;
    return `⬆️ استمر مباشرة${name}`;
  }

  async function drawRouteToOrder(o) {
    ensureMap();
    const lat = Number(o.customerLatitude ?? o.CustomerLatitude ?? 0);
    const lng = Number(o.customerLongitude ?? o.CustomerLongitude ?? 0);
    const cname = o.customerName ?? o.CustomerName ?? 'الزبون';
    const addr = o.deliveryAddress ?? o.DeliveryAddress ?? '';
    const num = o.orderNumber ?? o.OrderNumber ?? ('Q-' + (o.orderID ?? o.OrderID));

    if (!lat || !lng) {
      $('stepsBox').classList.add('hidden');
      $('mapInfo').innerHTML = `<strong>طلب ${num} (${cname})</strong><br/>📍 العنوان: ${addr}<br/><small style="color:#FBBF24">لا توجد إحداثيات GPS محفوظة لهذا الطلب.</small>`;
      map.setView([CFG.RESTAURANT_LAT, CFG.RESTAURANT_LNG], 14);
      return;
    }

    if (custMarker) map.removeLayer(custMarker);
    custMarker = L.marker([lat, lng]).addTo(map).bindPopup(`📍 الزبون: ${cname} (${num})`).openPopup();

    const startLat = driverPos ? driverPos.lat : CFG.RESTAURANT_LAT;
    const startLng = driverPos ? driverPos.lng : CFG.RESTAURANT_LNG;
    let routeSummary = '';

    try {
      const r = await fetch(`https://router.project-osrm.org/route/v1/driving/${startLng},${startLat};${lng},${lat}?overview=full&geometries=geojson&steps=true`);
      const rj = await r.json();
      if (rj.routes && rj.routes[0]) {
        const route = rj.routes[0];
        const coords = route.geometry.coordinates.map(([x, y]) => [y, x]);
        if (routeLine) map.removeLayer(routeLine);
        routeLine = L.polyline(coords, { color: '#F37A1F', weight: 6, opacity: 0.95 }).addTo(map);
        map.fitBounds(routeLine.getBounds(), { padding: [36, 36] });
        const km = (route.distance / 1000).toFixed(1);
        const mins = Math.max(1, Math.round(route.duration / 60));
        routeSummary = `🚗 المسافة: <strong>${km} كم</strong> • الوقت المتوقع: <strong>${mins} دقيقة</strong><br/>`;

        const steps = route.legs?.[0]?.steps || [];
        if (steps.length) {
          const firstStep = steps.find((s) => s.distance > 10) || steps[0];
          const firstTxt = maneuverAr(firstStep);
          const firstDist = firstStep.distance >= 1000 ? `${(firstStep.distance / 1000).toFixed(1)} كم` : `${Math.round(firstStep.distance)} م`;
          $('nextTurnIcon').textContent = firstTxt.split(' ')[0] || '⬆️';
          $('nextTurnText').textContent = firstTxt.replace(/^[^ ]+ /, '');
          $('nextTurnDist').textContent = firstDist;
          $('nextTurnBanner').classList.remove('hidden');
          $('stepsList').innerHTML = steps.map((st) => {
            const dist = st.distance >= 1000 ? `${(st.distance / 1000).toFixed(1)} كم` : `${Math.round(st.distance)} م`;
            return `<div class="step-item"><span>${maneuverAr(st)}</span><strong style="color:#FFB879;white-space:nowrap;">${dist}</strong></div>`;
          }).join('');
          $('stepsBox').classList.remove('hidden');
        }
      } else {
        map.setView([lat, lng], 16);
      }
    } catch (_) {
      map.setView([lat, lng], 16);
    }

    const gmaps = `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}`;
    const waze = `https://waze.com/ul?ll=${lat},${lng}&navigate=yes`;
    $('mapInfo').innerHTML = `
      <strong>طلب ${num} — ${cname}</strong><br/>
      📍 ${addr}<br/>
      ${routeSummary}
      <div class="nav-links">
        <a href="${waze}" target="_blank" rel="noopener" class="act-btn act-waze" style="display:flex;align-items:center;justify-content:center;">${SVG_WAZE}<span>Waze</span></a>
        <a href="${gmaps}" target="_blank" rel="noopener" class="act-btn act-orange" style="display:flex;align-items:center;justify-content:center;">${SVG_MAP}<span>Google Maps</span></a>
      </div>`;
  }

  window._qOpenMap = function (orderId) {
    focusedOrder = activeOrders.find((x) => (x.orderID ?? x.OrderID) === orderId) || null;
    switchTab('map');
    if (focusedOrder) drawRouteToOrder(focusedOrder);
  };

  function pushGps() {
    if (!auth || !navigator.geolocation) return;
    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        driverPos = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        if (map) {
          if (driverMarker) driverMarker.setLatLng([driverPos.lat, driverPos.lng]);
          else driverMarker = L.circleMarker([driverPos.lat, driverPos.lng], { radius: 9, color: '#fff', fillColor: '#2563EB', fillOpacity: 1, weight: 3 }).addTo(map).bindPopup('🛵 موقعك الحالي');
        }
        try {
          await apiFetch(`api/deliveryworkers/${auth.id}/location`, {
            method: 'POST',
            body: JSON.stringify({ Latitude: driverPos.lat, Longitude: driverPos.lng })
          });
        } catch (_) {}
      },
      () => {},
      { enableHighAccuracy: true, timeout: 10000 }
    );
  }

  function switchTab(tab) {
    ['orders', 'map', 'sales', 'expenses'].forEach((t) => {
      $('tab-' + t).classList.toggle('hidden', t !== tab);
    });
    document.querySelectorAll('.nav-btn').forEach((b) => {
      b.classList.toggle('active', b.dataset.tab === tab);
    });
    if (tab === 'map') {
      ensureMap();
      if (focusedOrder) drawRouteToOrder(focusedOrder);
      else if (activeOrders.length) drawRouteToOrder(activeOrders[0]);
    }
  }

  
  let wazeVoiceEnabled = true;
  let lastSpokenStep = '';
  let currentRouteTarget = null;
  function speakArabicTurn(text) {
    if (!wazeVoiceEnabled || !text || text === lastSpokenStep || !('speechSynthesis' in window)) return;
    lastSpokenStep = text;
    try {
      window.speechSynthesis.cancel();
      const u = new SpeechSynthesisUtterance(text);
      u.lang = 'ar-SA';
      u.rate = 1.05;
      window.speechSynthesis.speak(u);
    } catch (_) {}
  }
  const vBtn = $('wazeVoiceBtn');
  if (vBtn) vBtn.addEventListener('click', () => {
    wazeVoiceEnabled = !wazeVoiceEnabled;
    vBtn.textContent = wazeVoiceEnabled ? '🔊' : '🔇';
    showToast(wazeVoiceEnabled ? '🔊 تم تفعيل التوجيه الصوتي' : '🔇 تم كتم التوجيه الصوتي');
  });
  const wExtBtn = $('wazeExternalBtn');
  if (wExtBtn) wExtBtn.addEventListener('click', () => {
    if (currentRouteTarget) {
      window.open(`https://waze.com/ul?ll=${currentRouteTarget.lat},${currentRouteTarget.lng}&navigate=yes`, '_blank');
    } else {
      window.open('https://waze.com/ul?ll=36.120858,44.061717&navigate=yes', '_blank');
    }
  });

  function enterApp() {
    loadSavedShiftState();
    $('loginView').classList.add('hidden');
    $('appView').classList.remove('hidden');
    $('workerNameLbl').textContent = auth.name || auth.username;
    refreshAll(true);
    pushGps();
    enablePushNotifications(true);
    clearInterval(pollTimer);
    clearInterval(gpsTimer);
    pollTimer = setInterval(() => refreshAll(true), CFG.POLL_MS);
    gpsTimer = setInterval(pushGps, CFG.GPS_PUSH_MS);
  }

  // Events
  $('loginBtn').addEventListener('click', async () => {
    const username = $('uInput').value.trim();
    const password = $('pInput').value.trim();
    const err = $('loginErr');
    err.classList.add('hidden');
    if (!username || !password) { err.textContent = 'أدخل اسم المستخدم وكلمة المرور'; err.classList.remove('hidden'); return; }
    $('loginBtn').disabled = true;
    $('loginBtn').textContent = 'جارٍ الدخول...';
    try {
      const resp = await fetch(API + 'api/DeliveryWorkers/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Username: username, Password: password })
      });
      const data = await resp.json();
      if (!resp.ok || !data.success) throw new Error(data.message || 'بيانات الدخول غير صحيحة');
      auth = {
        id: data.deliveryWorkerID ?? data.DeliveryWorkerID,
        name: data.fullName ?? data.FullName ?? username,
        phone: data.phone ?? data.Phone ?? '',
        username,
        password
      };
      localStorage.setItem(AUTH_KEY, JSON.stringify(auth));
      enterApp();
    } catch (ex) {
      err.textContent = ex.message;
      err.classList.remove('hidden');
    } finally {
      $('loginBtn').disabled = false;
      $('loginBtn').textContent = 'دخول عامل التوصيل';
    }
  });

  $('logoutBtn').addEventListener('click', () => {
    if (!confirm('تسجيل الخروج من حساب عامل التوصيل؟')) return;
    localStorage.removeItem(AUTH_KEY);
    auth = null;
    clearInterval(pollTimer);
    clearInterval(gpsTimer);
    $('appView').classList.add('hidden');
    $('loginView').classList.remove('hidden');
  });

  $('pushBtn').addEventListener('click', () => enablePushNotifications(false));
  $('refreshBtn').addEventListener('click', () => refreshAll(false));
  $('closeTransferBtn').addEventListener('click', () => $('transferModal').classList.add('hidden'));

  $('shiftBtn').addEventListener('click', async () => {
    if (!auth) return;
    const ep = isOnShift ? `api/deliveryworkers/${auth.id}/end-shift` : `api/deliveryworkers/${auth.id}/start-shift`;
    try {
      await apiFetch(ep, { method: 'POST', body: '{}' });
      applyShiftUi(!isOnShift);
      try { localStorage.setItem(shiftStorageKey(), isOnShift ? '1' : '0'); } catch (_) {}
      showToast(isOnShift ? '🟢 تم بدء الدوام' : '🔴 تم إنهاء الدوام');
    } catch (ex) { showToast('⚠️ ' + ex.message); }
  });

  $('addExpBtn').addEventListener('click', () => {
    const title = $('expTitle').value.trim();
    const amount = Number($('expAmt').value || 0);
    if (!title || amount <= 0) { showToast('أدخل البيان والمبلغ بشكل صحيح'); return; }
    const exps = loadExpenses();
    exps.unshift({ title, amount, date: todayStr(), time: new Date().toLocaleTimeString('ar-IQ', { hour: '2-digit', minute: '2-digit' }) });
    saveExpenses(exps);
    $('expTitle').value = '';
    $('expAmt').value = '';
    renderSalesAndExpenses();
    showToast('✅ تم حفظ المصروف');
  });

  document.querySelectorAll('.nav-btn').forEach((b) => {
    b.addEventListener('click', () => switchTab(b.dataset.tab));
  });

  document.querySelectorAll('[data-layer]').forEach((b) => {
    b.addEventListener('click', () => {
      ensureMap();
      const lyr = b.dataset.layer;
      if (currentLayer) map.removeLayer(currentLayer);
      currentLayer = tileLayers[lyr].addTo(map);
      $('qamishloMap').classList.toggle('qamishlo-dark', lyr === 'dark');
      document.querySelectorAll('[data-layer]').forEach((x) => x.classList.toggle('active-layer', x === b));
    });
  });

  function setMapBearing(deg) {
    currentBearing = ((deg % 360) + 360) % 360;
    if (map && typeof map.setBearing === 'function') map.setBearing(currentBearing);
  }
  $('rotLeftBtn').addEventListener('click', () => { ensureMap(); setMapBearing(currentBearing - 45); });
  $('rotRightBtn').addEventListener('click', () => { ensureMap(); setMapBearing(currentBearing + 45); });
  $('compassBtn').addEventListener('click', async () => {
    ensureMap();
    compassActive = !compassActive;
    $('compassBtn').classList.toggle('on', compassActive);
    if (!compassActive) { setMapBearing(0); showToast('تم تثبيت اتجاه الشمال'); return; }
    if (typeof DeviceOrientationEvent !== 'undefined' && typeof DeviceOrientationEvent.requestPermission === 'function') {
      try { await DeviceOrientationEvent.requestPermission(); } catch (_) {}
    }
    showToast('🧭 تم تفعيل التدوير التلقائي 360° للخريطة');
  });
  window.addEventListener('deviceorientation', (ev) => {
    if (!compassActive || !map) return;
    const heading = typeof ev.webkitCompassHeading === 'number' ? ev.webkitCompassHeading : (typeof ev.alpha === 'number' ? 360 - ev.alpha : null);
    if (heading !== null && !Number.isNaN(heading)) setMapBearing(heading);
  });

  $('expandMapBtn').addEventListener('click', () => {
    const sh = $('mapShell');
    const isFull = sh.classList.toggle('fullscreen');
    $('expandMapBtn').textContent = isFull ? '✕ تصغير' : '⛶ تكبير';
    setTimeout(() => { if (map) map.invalidateSize(); }, 150);
  });

  $('locateMeBtn').addEventListener('click', () => {
    ensureMap();
    pushGps();
    if (driverPos) map.setView([driverPos.lat, driverPos.lng], 16);
    else showToast('جارٍ تحديد موقعك...');
  });

  try {
    const saved = JSON.parse(localStorage.getItem(AUTH_KEY) || 'null');
    if (saved && saved.id && saved.username && saved.password) {
      auth = saved;
      enterApp();
    }
  } catch (_) {}
})();
