import React, { useEffect, useMemo, useState } from 'react';
import { api } from '../api/apiClient.js';
import HomeBanner from '../components/HomeBanner.jsx';
import {
  Zap, Check, AlertCircle, Building2, Gift, Clock, Search, Send,
  Printer, Star, Loader2, RefreshCw, LogIn
} from 'lucide-react';

/* ==========================================================================
   TRANG CÔNG KHAI - NEXUS
   Dữ liệu lấy từ ASP.NET Core Web API (.NET 10):
     GET /api/public/plans?type=D|B|T
     GET /api/public/shops
     GET /api/public/discount?qty=
     GET /api/public/track-order?orderId=
     GET /api/public/track-account?accountId=
     POST /api/my/feedback            (yêu cầu đăng nhập khách hàng)
   ========================================================================== */

const TABS = [
  { type: 'B', label: '🌐 Internet Băng Rộng (Broadband)' },
  { type: 'D', label: '📞 Internet Dial-up' },
  { type: 'T', label: '☎️ Điện Thoại Cố Định (Landline)' }
];

// Bậc ưu đãi dự phòng, dùng khi chưa gọi được API /public/discount.
const FALLBACK_TIERS = [
  { minQty: 1, maxQty: 9, discountPercent: 0 },
  { minQty: 10, maxQty: 15, discountPercent: 25 },
  { minQty: 16, maxQty: 25, discountPercent: 50 },
  { minQty: 26, maxQty: 50, discountPercent: 75 },
  { minQty: 51, maxQty: null, discountPercent: 100 }
];

const STATUS_META = {
  SUBMITTED: { label: 'Đã tiếp nhận', cls: 'badge-info' },
  FEASIBILITY_CHECK: { label: 'Đang khảo sát', cls: 'badge-warning' },
  FEASIBLE: { label: 'Khảo sát đạt', cls: 'badge-success' },
  NOT_FEASIBLE: { label: 'Không khả thi', cls: 'badge-danger' },
  CONNECTED: { label: 'Đã cấp kết nối', cls: 'badge-success' },
  CANCELLED: { label: 'Đã hủy', cls: 'badge-neutral' },
  ACTIVE: { label: 'Đang hoạt động', cls: 'badge-success' },
  TEMP_INACTIVE: { label: 'Tạm ngưng', cls: 'badge-warning' },
  PERM_INACTIVE: { label: 'Ngừng hẳn', cls: 'badge-danger' }
};

const ORDER_STEPS = [
  { status: 'SUBMITTED', label: 'Tiếp Nhận Đơn' },
  { status: 'FEASIBILITY_CHECK', label: 'Khảo Sát Khả Thi' },
  { status: 'FEASIBLE', label: 'Duyệt Khả Thi' },
  { status: 'CONNECTED', label: 'Cấp Đấu Nối' }
];

const SERVICE_PREFIX = { D: 'Dial-up', B: 'Broadband', T: 'Điện thoại' };

function statusMeta(status) {
  return STATUS_META[status] || { label: status || '—', cls: 'badge-neutral' };
}

function formatDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? String(value)
    : date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function planTitle(plan) {
  if (plan.connType === 'T') return `${plan.groupName} ${plan.planName}`.trim();
  if (plan.hoursIncluded != null) return `${SERVICE_PREFIX[plan.connType] || ''} ${plan.planName}`.trim();
  return `${plan.groupName} (${plan.planName})`;
}

function planQuota(plan) {
  if (plan.hoursIncluded != null) return `${plan.hoursIncluded} giờ`;
  if (plan.speedKbps != null) return `${plan.speedKbps} Kbps — không giới hạn`;
  const rates = plan.callRates || [];
  return rates.length
    ? rates.map(r => `${r.callType} ${r.ratePerMin}$/phút`).join(' • ')
    : 'Không giới hạn';
}

function planPeriod(plan) {
  const months = plan.validityMonths;
  if (months === 12) return '1 Năm';
  if (months === 6) return '6 Tháng';
  if (months === 1) return '1 Tháng';
  return `${months} Tháng`;
}

/** Gói "phổ biến": gói không giới hạn 1 tháng nhanh nhất (D/B), Local Monthly (T). */
function isFeatured(plan, all) {
  if (plan.connType === 'T') return plan.groupName === 'Local' && plan.planName === 'Monthly';

  const monthly = all.filter(p => p.connType === plan.connType
    && p.hoursIncluded == null && p.validityMonths === 1);
  if (!monthly.length) return false;
  const top = monthly.reduce((best, p) => ((p.speedKbps ?? 0) > (best.speedKbps ?? 0) ? p : best));
  return top.planId === plan.planId;
}

function localPercent(quantity, tiers) {
  const list = tiers && tiers.length ? tiers : FALLBACK_TIERS;
  const tier = list.find(t => quantity >= t.minQty && (t.maxQty == null || quantity <= t.maxQty));
  return tier ? tier.discountPercent : 0;
}

export default function PublicView() {
  const [activeType, setActiveType] = useState('B');

  // ----------------------------- Danh mục -----------------------------
  const [plans, setPlans] = useState([]);
  const [plansLoading, setPlansLoading] = useState(true);
  const [catalogError, setCatalogError] = useState('');
  const [shopCount, setShopCount] = useState(null);

  // ----------------------------- Ưu đãi ------------------------------
  const [sliderQuantity, setSliderQuantity] = useState(12);
  const [discount, setDiscount] = useState({ percent: localPercent(12), tiers: FALLBACK_TIERS });
  const [discountLoading, setDiscountLoading] = useState(false);

  // ---------------------------- Tra cứu ------------------------------
  const [searchOrderId, setSearchOrderId] = useState('');
  const [orderResult, setOrderResult] = useState(null);
  const [orderLoading, setOrderLoading] = useState(false);

  const [searchAccountId, setSearchAccountId] = useState('');
  const [accountResult, setAccountResult] = useState(null);
  const [accountLoading, setAccountLoading] = useState(false);
  const [selectedBillModal, setSelectedBillModal] = useState(null);

  // ---------------------------- Phản hồi -----------------------------
  const [feedback, setFeedback] = useState({ refId: '', rating: '5', comment: '' });
  const [feedbackState, setFeedbackState] = useState({ loading: false, error: '', success: '' });

  const filteredPlans = useMemo(
    () => plans.filter(p => p.connType === activeType),
    [plans, activeType]
  );

  const discountPct = discount?.percent ?? localPercent(sliderQuantity, discount?.tiers);

  // Tải danh mục một lần khi mở trang.
  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      const [plansRes, shopsRes] = await Promise.allSettled([api.getPlans(), api.getShops()]);
      if (cancelled) return;

      if (plansRes.status === 'fulfilled') {
        setPlans(plansRes.value || []);
        setCatalogError('');
      } else {
        setCatalogError(plansRes.reason?.message || 'Không tải được bảng giá từ máy chủ.');
      }

      if (shopsRes.status === 'fulfilled') {
        setShopCount((shopsRes.value || []).length);
      }

      setPlansLoading(false);
    };

    load();
    return () => { cancelled = true; };
  }, []);

  // Tính ưu đãi theo số lượng, chờ người dùng ngừng kéo thanh trượt mới gọi API.
  useEffect(() => {
    let cancelled = false;
    setDiscountLoading(true);

    const timer = setTimeout(async () => {
      try {
        const data = await api.getDiscount(sliderQuantity);
        if (!cancelled) setDiscount(data);
      } catch {
        if (!cancelled) setDiscount({ percent: localPercent(sliderQuantity), tiers: FALLBACK_TIERS });
      } finally {
        if (!cancelled) setDiscountLoading(false);
      }
    }, 250);

    return () => { cancelled = true; clearTimeout(timer); };
  }, [sliderQuantity]);

  const handleSearchOrder = async (event) => {
    event?.preventDefault();
    const query = searchOrderId.trim().toUpperCase();
    if (!query) {
      setOrderResult({ error: true, query: '', message: 'Vui lòng nhập Order ID cần tra cứu.' });
      return;
    }

    setOrderLoading(true);
    setOrderResult(null);
    try {
      setOrderResult({ data: await api.trackOrder(query) });
    } catch (err) {
      setOrderResult({ error: true, query, message: err.message });
    } finally {
      setOrderLoading(false);
    }
  };

  const handleSearchAccount = async (event) => {
    event?.preventDefault();
    const query = searchAccountId.trim().toUpperCase();
    if (!query) {
      setAccountResult({ error: true, query: '', message: 'Vui lòng nhập Account ID cần tra cứu.' });
      return;
    }

    setAccountLoading(true);
    setAccountResult(null);
    try {
      setAccountResult({ data: await api.trackAccount(query) });
    } catch (err) {
      setAccountResult({ error: true, query, message: err.message });
    } finally {
      setAccountLoading(false);
    }
  };

  const handleFeedbackSubmit = async (event) => {
    event.preventDefault();
    const refId = feedback.refId.trim().toUpperCase();

    if (!refId) {
      setFeedbackState({ loading: false, success: '', error: 'Vui lòng nhập Account ID hoặc Order ID để gắn phản hồi.' });
      return;
    }

    if (!feedback.comment.trim()) {
      setFeedbackState({ loading: false, success: '', error: 'Vui lòng nhập nội dung phản hồi.' });
      return;
    }

    setFeedbackState({ loading: true, success: '', error: '' });
    try {
      await api.sendFeedback({
        refType: refId.length === 11 ? 'ORDER' : 'CONNECTION',
        refId,
        rating: Number(feedback.rating),
        comment: feedback.comment.trim()
      });
      setFeedbackState({ loading: false, error: '', success: 'Cảm ơn quý khách! Phản hồi đã được chuyển tới Ban Chăm sóc Khách hàng Nexus.' });
      setFeedback({ refId: '', rating: '5', comment: '' });
    } catch (err) {
      const needsLogin = err.status === 401 || err.status === 403 || err.code === 'AUTH_INVALID';
      setFeedbackState({
        loading: false,
        success: '',
        error: needsLogin
          ? 'Gửi phản hồi yêu cầu đăng nhập cổng khách hàng. Vui lòng đăng nhập hoặc gọi hotline 1900 8888.'
          : err.message
      });
    }
  };

  const discountPolicy = discountPct === 0
    ? 'Không giảm (< 10 kết nối)'
    : `Giảm ${discountPct}% cho hợp đồng từ ${sliderQuantity} kết nối`;

  return (
    <div>
      {/* Home Banner Carousel */}
      <HomeBanner />

      {/* KPI Highlights */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon blue"><Zap size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">99.9%</div>
            <div className="stat-label">Độ ổn định đường truyền</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon green"><Building2 size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{shopCount == null ? '—' : `${shopCount}+`}</div>
            <div className="stat-label">Cửa hàng bán lẻ toàn quốc</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon orange"><Gift size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">Đến 100%</div>
            <div className="stat-label">Chiết khấu số lượng lớn</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon purple"><Clock size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">24/7</div>
            <div className="stat-label">Khảo sát & hỗ trợ kỹ thuật</div>
          </div>
        </div>
      </div>

      {/* Pricing & Plans Section */}
      <section id="plans-section" className="mt-4">
        <div className="nexus-card">
          <div className="nexus-card-header">
            <div>
              <h2 className="nexus-card-title">Bảng Giá Dịch Vụ & Gói Cước Niêm Yết</h2>
              <p className="form-hint">Mọi gói cước được áp dụng theo đúng quy chuẩn ban hành của Nexus Telecom</p>
            </div>
          </div>

          {/* Service Tabs */}
          <div className="service-nav-tabs">
            {TABS.map(tab => (
              <button
                key={tab.type}
                className={`service-tab-btn ${activeType === tab.type ? 'active' : ''}`}
                onClick={() => setActiveType(tab.type)}
              >
                {tab.label}
              </button>
            ))}
          </div>

          {catalogError && (
            <div style={{ padding: '12px', background: 'var(--danger-bg)', color: 'var(--danger)', borderRadius: 'var(--radius)', marginBottom: '1rem', fontWeight: 600 }}>
              <AlertCircle size={16} /> {catalogError}
            </div>
          )}

          {/* Plan Cards Grid */}
          <div className="pricing-grid">
            {plansLoading && (
              <div className="text-muted" style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '1rem' }}>
                <Loader2 size={16} className="spin" /> Đang tải bảng giá từ máy chủ...
              </div>
            )}

            {!plansLoading && !catalogError && filteredPlans.length === 0 && (
              <div className="text-muted" style={{ padding: '1rem' }}>
                Chưa có gói cước nào đang áp dụng cho loại dịch vụ này.
              </div>
            )}

            {filteredPlans.map(plan => {
              const featured = isFeatured(plan, filteredPlans);
              return (
                <div key={plan.planId} className={`pricing-card ${featured ? 'featured' : ''}`}>
                  {featured && <span className="pricing-badge">Phổ Biến Nhất</span>}
                  <div className="pricing-card-header">
                    <div className="pricing-service-type">
                      {SERVICE_PREFIX[plan.connType]} • {plan.speedKbps ? `${plan.speedKbps} Kbps` : 'Thoại cố định'}
                    </div>
                    <h3 className="pricing-plan-name">{planTitle(plan)}</h3>
                  </div>
                  <div className="pricing-price-wrap">
                    <span className="pricing-currency">$</span>
                    <span className="pricing-price">{plan.price}</span>
                    <span className="pricing-period">/ {planPeriod(plan)}</span>
                  </div>
                  <ul className="pricing-features">
                    <li>
                      <Check size={16} color="var(--success)" />
                      Hạn mức / Tốc độ: <strong>{planQuota(plan)}</strong>
                    </li>
                    <li>
                      <Check size={16} color="var(--success)" />
                      Tiền đặt cọc thiết bị: <strong>${plan.deposit}</strong> (Hoàn lại)
                    </li>
                    <li>
                      <Check size={16} color="var(--success)" />
                      Thời hạn hiệu lực: <strong>{planPeriod(plan)}</strong>
                    </li>
                    {plan.connType === 'D' && (
                      <li style={{ color: '#ea580c' }}>
                        <AlertCircle size={16} color="#ea580c" />
                        Yêu cầu có sẵn Landline của Nexus
                      </li>
                    )}
                  </ul>
                  <a href="#schemes-section" className={`btn ${featured ? 'btn-primary' : 'btn-outline'}`} style={{ textDecoration: 'none' }}>
                    Tính Chiết Khấu Khi Đăng Ký
                  </a>
                </div>
              );
            })}
          </div>

          {/* Bulk Discount Interactive Calculator */}
          <div id="schemes-section" className="bulk-calculator-card">
            <div className="flex-between">
              <div>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: '#166534', display: 'flex', alignItems: 'center', gap: '8px' }}>
                  🏷️ Công Cụ Tính Chiết Khấu Số Lượng Lớn (Corporate Schemes)
                </h3>
                <p style={{ fontSize: '0.85rem', color: '#15803d' }}>
                  Ưu đãi áp dụng khi khách hàng hoặc doanh nghiệp đăng ký từ 10 đường truyền trở lên
                </p>
              </div>
              <div className="discount-tier-badge" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                {discountLoading && <RefreshCw size={14} className="spin" />}
                -{discountPct}% GIẢM GIÁ
              </div>
            </div>

            <div className="slider-container" style={{ margin: '1.5rem 0' }}>
              <div className="flex-between" style={{ marginBottom: '8px', fontWeight: 700 }}>
                <span>
                  Số lượng kết nối dự kiến đăng ký:{' '}
                  <strong style={{ color: 'var(--primary)', fontSize: '1.1rem' }}>{sliderQuantity}</strong> kết nối
                </span>
                <span className="text-muted" style={{ fontSize: '0.85rem' }}>Kéo thanh trượt từ 1 đến 60 kết nối</span>
              </div>
              <input
                type="range"
                min="1"
                max="60"
                value={sliderQuantity}
                onChange={(e) => setSliderQuantity(parseInt(e.target.value, 10))}
                className="range-slider"
              />
            </div>

            <div className="grid-3" style={{ marginTop: '1rem', fontSize: '0.875rem' }}>
              <div style={{ background: 'white', padding: '12px', borderRadius: 'var(--radius)', border: '1px solid #bbf7d0' }}>
                <span className="text-muted">Chính sách áp dụng:</span>
                <div style={{ fontWeight: 700, color: '#166534', marginTop: '2px' }}>
                  {discountPolicy}
                </div>
              </div>
              <div style={{ background: 'white', padding: '12px', borderRadius: 'var(--radius)', border: '1px solid #bbf7d0' }}>
                <span className="text-muted">Mức giảm tiền cọc:</span>
                <div style={{ fontWeight: 700, color: '#166534', marginTop: '2px' }}>
                  -{discountPct}% cho mỗi đầu dây
                </div>
              </div>
              <div style={{ background: 'white', padding: '12px', borderRadius: 'var(--radius)', border: '1px solid #bbf7d0' }}>
                <span className="text-muted">Mức giảm cước ứng trước:</span>
                <div style={{ fontWeight: 700, color: '#166534', marginTop: '2px' }}>
                  -{discountPct}% chu kỳ đầu tiên
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Tracking Section (Order & Account ID) */}
      <section id="tracking-section" className="mt-4">
        <div className="grid-2">
          {/* Order Tracking */}
          <div className="nexus-card">
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">📦 Tra Cứu Tiến Độ Đơn (Order ID)</h3>
            </div>
            <p className="form-hint">Nhập mã đơn hàng 11 ký tự bạn nhận được tại cửa hàng (VD: <code>B0000000001</code>)</p>
            <form className="flex-gap-2" style={{ marginTop: '1rem' }} onSubmit={handleSearchOrder}>
              <input
                type="text"
                className="form-control"
                value={searchOrderId}
                onChange={(e) => setSearchOrderId(e.target.value)}
                placeholder="Nhập Order ID..."
              />
              <button type="submit" className="btn btn-primary" disabled={orderLoading}>
                {orderLoading ? <Loader2 size={16} className="spin" /> : <Search size={16} />} Tra Cứu
              </button>
            </form>

            <div className="tracking-result-box" style={{ marginTop: '1.25rem', padding: '1.25rem', background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)' }}>
              {!orderResult && (
                <div className="text-muted" style={{ fontSize: '0.875rem' }}>
                  Nhấn "Tra Cứu" để theo dõi quy trình khảo sát đường truyền và thời gian lắp đặt.
                </div>
              )}

              {orderResult?.error && (
                <div style={{ color: 'var(--danger)', fontWeight: 600 }}>
                  ❌ {orderResult.message}
                </div>
              )}

              {orderResult?.data && (() => {
                const order = orderResult.data;
                const meta = statusMeta(order.status);
                const currentIndex = ORDER_STEPS.findIndex(s => s.status === order.status);
                const failed = order.status === 'NOT_FEASIBLE' || order.status === 'CANCELLED';

                return (
                  <div>
                    <div style={{ fontWeight: 700, fontSize: '1.05rem', marginBottom: '8px', display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
                      <code className="font-mono" style={{ color: 'var(--primary)' }}>{order.orderId}</code>
                      <span className={`badge ${meta.cls}`}>{meta.label}</span>
                    </div>

                    <div className="nexus-stepper">
                      {ORDER_STEPS.map((step, index) => {
                        const done = !failed && currentIndex >= 0 && index <= currentIndex;
                        const active = !failed && index === currentIndex + 1;
                        return (
                          <div key={step.status} className={`step-item ${done ? 'completed' : ''} ${active ? 'active' : ''}`}>
                            <div className="step-circle">{done ? '✓' : index + 1}</div>
                            <div className="step-label">{step.label}</div>
                          </div>
                        );
                      })}
                    </div>

                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '8px', lineHeight: 1.6 }}>
                      • Gói đăng ký: <strong>{order.planName}</strong> ({order.quantity} đường)<br />
                      • Đã cấp kết nối: <strong>{order.connectedQty}/{order.quantity}</strong> đường<br />
                      • Ưu đãi áp dụng: <strong>{order.discountPercent}%</strong> | Ngày đặt: <strong>{formatDate(order.orderDate)}</strong>
                    </div>
                  </div>
                );
              })()}
            </div>
          </div>

          {/* Account Tracking */}
          <div className="nexus-card">
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">📡 Tra Cứu Cước & Thuê Bao (Account ID)</h3>
            </div>
            <p className="form-hint">Nhập mã tài khoản 16 ký tự do Kỹ thuật cấp (VD: <code>B001000000000001</code>)</p>
            <form className="flex-gap-2" style={{ marginTop: '1rem' }} onSubmit={handleSearchAccount}>
              <input
                type="text"
                className="form-control"
                value={searchAccountId}
                onChange={(e) => setSearchAccountId(e.target.value)}
                placeholder="Nhập Account ID..."
              />
              <button type="submit" className="btn btn-primary" disabled={accountLoading}>
                {accountLoading ? <Loader2 size={16} className="spin" /> : <Search size={16} />} Tra Cứu
              </button>
            </form>

            <div className="tracking-result-box" style={{ marginTop: '1.25rem', padding: '1.25rem', background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)' }}>
              {!accountResult && (
                <div className="text-muted" style={{ fontSize: '0.875rem' }}>
                  Nhập mã Account ID để xem trạng thái thuê bao và số nợ cước hiện tại.
                </div>
              )}

              {accountResult?.error && (
                <div style={{ color: 'var(--danger)', fontWeight: 600 }}>
                  ❌ {accountResult.message}
                </div>
              )}

              {accountResult?.data && (() => {
                const conn = accountResult.data;
                const meta = statusMeta(conn.status);

                return (
                  <div>
                    <div className="flex-between">
                      <div style={{ fontWeight: 700, fontSize: '1.05rem' }}>
                        Account ID: <code className="font-mono" style={{ color: 'var(--primary)' }}>{conn.accountId}</code>
                      </div>
                      <span className={`badge ${meta.cls}`}>{meta.label}</span>
                    </div>
                    <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: '10px', lineHeight: 1.6 }}>
                      • Gói cước: <strong>{conn.planName}</strong> ({SERVICE_PREFIX[conn.connType]})<br />
                      • Khu vực: <strong>{conn.cityName}</strong><br />
                      • Ngày kích hoạt: <strong>{formatDate(conn.activationDate)}</strong>
                    </div>

                    <div style={{ marginTop: '12px', paddingTop: '10px', borderTop: '1px dashed var(--border-color)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
                      <div>
                        <span className="text-muted" style={{ fontSize: '0.8rem' }}>Tổng nợ cước hiện tại:</span>
                        <div style={{ fontWeight: 800, fontSize: '1.1rem', color: conn.dueAmount > 0 ? 'var(--danger)' : 'var(--success)' }}>
                          ${Number(conn.dueAmount || 0).toFixed(2)}{' '}
                          <span className={`badge ${conn.dueAmount > 0 ? 'badge-danger' : 'badge-success'}`} style={{ fontSize: '0.7rem' }}>
                            {conn.dueAmount > 0 ? 'CÒN NỢ' : 'ĐÃ TẤT TOÁN'}
                          </span>
                        </div>
                      </div>
                      <button className="btn btn-outline btn-sm" onClick={() => setSelectedBillModal(conn)}>
                        <Printer size={14} /> Xem Chi Tiết Công Nợ
                      </button>
                    </div>

                    <p className="form-hint" style={{ marginTop: '10px' }}>
                      Chi tiết từng hóa đơn và thanh toán chỉ hiển thị sau khi đăng nhập cổng khách hàng.
                    </p>
                  </div>
                );
              })()}
            </div>
          </div>
        </div>
      </section>

      {/* Feedback Section */}
      <section id="feedback-section" className="mt-4">
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">💬 Gửi Ý Kiến Đóng Góp & Phản Hồi Dịch Vụ</h3>
          </div>

          {feedbackState.success && (
            <div style={{ padding: '12px', background: 'var(--success-bg)', color: 'var(--success)', borderRadius: 'var(--radius)', marginBottom: '1rem', fontWeight: 600, display: 'flex', gap: '8px', alignItems: 'center' }}>
              <Check size={16} /> {feedbackState.success}
            </div>
          )}

          {feedbackState.error && (
            <div style={{ padding: '12px', background: 'var(--danger-bg)', color: 'var(--danger)', borderRadius: 'var(--radius)', marginBottom: '1rem', fontWeight: 600, display: 'flex', gap: '8px', alignItems: 'flex-start' }}>
              <LogIn size={16} style={{ flexShrink: 0, marginTop: '2px' }} /> <span>{feedbackState.error}</span>
            </div>
          )}

          <p className="form-hint" style={{ marginBottom: '1rem' }}>
            Phản hồi được gắn với đúng thuê bao của bạn. Vui lòng đăng nhập cổng khách hàng trước khi gửi,
            hoặc nhập mã Order ID / Account ID đang sở hữu.
          </p>

          <form onSubmit={handleFeedbackSubmit}>
            <div className="grid-3">
              <div className="form-group">
                <label className="form-label">Mã Account ID / Order ID <span className="req">*</span></label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="B001000000000001"
                  value={feedback.refId}
                  onChange={(e) => setFeedback(f => ({ ...f, refId: e.target.value }))}
                  required
                />
              </div>
              <div className="form-group">
                <label className="form-label">Điểm đánh giá chất lượng</label>
                <select
                  className="form-select"
                  value={feedback.rating}
                  onChange={(e) => setFeedback(f => ({ ...f, rating: e.target.value }))}
                >
                  {[5, 4, 3, 2, 1].map(score => (
                    <option key={score} value={score}>{score} sao</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">Kênh hỗ trợ trực tiếp</label>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px', paddingTop: '0.55rem', fontWeight: 700, color: 'var(--primary)' }}>
                  <Star size={16} /> Hotline 1900 8888
                </div>
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">Nội dung phản hồi hoặc khiếu nại <span className="req">*</span></label>
              <textarea
                className="form-control"
                rows="3"
                placeholder="Nhập thông tin phản hồi của bạn về chất lượng dịch vụ, tốc độ đường truyền hoặc nhân viên hỗ trợ..."
                value={feedback.comment}
                onChange={(e) => setFeedback(f => ({ ...f, comment: e.target.value }))}
                required
              ></textarea>
            </div>
            <button type="submit" className="btn btn-primary" disabled={feedbackState.loading}>
              {feedbackState.loading ? <Loader2 size={16} className="spin" /> : <Send size={16} />} Gửi Phản Hồi Cho Nexus
            </button>
          </form>
        </div>
      </section>

      {/* Bill View Modal */}
      {selectedBillModal && (
        <div className="modal-overlay">
          <div className="modal-content" style={{ maxWidth: '520px' }}>
            <div className="modal-header">
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700 }}>📄 Bảng Tổng Hợp Công Nợ</h3>
              <button className="btn btn-outline btn-sm" onClick={() => setSelectedBillModal(null)}>✕</button>
            </div>
            <div className="modal-body">
              <div style={{ background: 'white', border: '1px dashed #cbd5e1', borderRadius: 'var(--radius)', padding: '2rem' }}>
                <div style={{ textAlign: 'center', marginBottom: '1.5rem', borderBottom: '1px dashed #cbd5e1', paddingBottom: '1rem' }}>
                  <h2 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--primary)' }}>NEXUS COMMUNICATION SYSTEM</h2>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '2px' }}>DỊCH VỤ VIỄN THÔNG & INTERNET</div>
                  <div style={{ marginTop: '8px', fontWeight: 700 }}>SAO KÊ THUÊ BAO</div>
                  <div style={{ fontSize: '0.775rem', color: 'var(--text-muted)' }}>Tra cứu công khai — chi tiết hóa đơn cần đăng nhập</div>
                </div>

                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Account ID:</span>
                  <code className="font-mono">{selectedBillModal.accountId}</code>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Gói cước:</span>
                  <strong>{selectedBillModal.planName}</strong>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Khu vực:</span>
                  <span>{selectedBillModal.cityName}</span>
                </div>
                <div className="flex-between" style={{ marginBottom: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Ngày kích hoạt:</span>
                  <span>{formatDate(selectedBillModal.activationDate)}</span>
                </div>
                <hr style={{ border: 'none', borderTop: '1px dashed #cbd5e1', margin: '10px 0' }} />

                <div className="flex-between" style={{ borderTop: '2px solid var(--text-primary)', marginTop: '1rem', paddingTop: '0.75rem', fontWeight: 800, fontSize: '1.1rem' }}>
                  <span>TỔNG CÔNG NỢ:</span>
                  <span style={{ color: 'var(--primary)' }}>${Number(selectedBillModal.dueAmount || 0).toFixed(2)}</span>
                </div>

                <div className="flex-between" style={{ marginTop: '8px', fontSize: '0.9rem' }}>
                  <span className="text-muted">Tình trạng thuê bao:</span>
                  <span className={`badge ${statusMeta(selectedBillModal.status).cls}`}>
                    {statusMeta(selectedBillModal.status).label}
                  </span>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setSelectedBillModal(null)}>Đóng</button>
              <button className="btn btn-primary" onClick={() => window.print()}>
                <Printer size={16} /> In Sao Kê
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}