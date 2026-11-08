import React, { useState } from 'react';
import { PLANS_DATA, getDiscountPercent } from '../data/mockData.js';
import { Zap, Check, AlertCircle, Building2, Gift, Clock, Search, Send } from 'lucide-react';

export default function PublicView({ orders, connections, onRegisterClick }) {
  const [activeTab, setActiveTab] = useState('BROADBAND');
  const [sliderQuantity, setSliderQuantity] = useState(12);

  // Search states
  const [searchOrderId, setSearchOrderId] = useState('B0000000001');
  const [orderResult, setOrderResult] = useState(null);

  const [searchAccountId, setSearchAccountId] = useState('B001000000000001');
  const [accountResult, setAccountResult] = useState(null);

  const [feedbackSuccess, setFeedbackSuccess] = useState(false);

  const filteredPlans = PLANS_DATA.filter(p => p.service === activeTab);
  const discountPct = getDiscountPercent(sliderQuantity);

  const handleSearchOrder = () => {
    const q = searchOrderId.trim().toUpperCase();
    const found = orders.find(o => o.orderId.toUpperCase() === q);
    setOrderResult(found || { notFound: true, query: q });
  };

  const handleSearchAccount = () => {
    const q = searchAccountId.trim().toUpperCase();
    const found = connections.find(c => c.accountId.toUpperCase() === q);
    setAccountResult(found || { notFound: true, query: q });
  };

  const handleFeedbackSubmit = (e) => {
    e.preventDefault();
    setFeedbackSuccess(true);
    setTimeout(() => setFeedbackSuccess(false), 5000);
    e.target.reset();
  };

  return (
    <div>
      {/* Hero Section */}
      <section className="hero-banner">
        <div className="hero-content">
          <span className="hero-tag">
            <Zap size={14} />
            Hệ thống Viễn thông Thế hệ Mới (React 19 & Vite)
          </span>
          <h1 className="hero-title">Kết nối Siêu tốc, Ổn định & Dịch vụ Hoàn hảo</h1>
          <p className="hero-desc">
            Nexus Communication System hân hạnh mang tới giải pháp viễn thông toàn diện: Internet cáp quang băng rộng (Broadband), Dial-up bảo mật cao và Điện thoại cố định nội hạt - liên tỉnh cho mọi gia đình và tổ chức.
          </p>
          <div className="hero-actions">
            <a href="#plans-section" className="btn btn-primary btn-lg" style={{ textDecoration: 'none' }}>
              Khám Phá Gói Cước
            </a>
            <a href="#tracking-section" className="btn btn-secondary btn-lg" style={{ textDecoration: 'none' }}>
              Tra Cứu Đơn & Tài Khoản
            </a>
          </div>
        </div>
      </section>

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
            <div className="stat-value">64+</div>
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
              <p className="form-hint">Mọi gói cước được áp dụng theo đúng quy chuẩn ban hành của Nexus</p>
            </div>
          </div>

          {/* Service Tabs */}
          <div className="service-nav-tabs">
            <button
              className={`service-tab-btn ${activeTab === 'BROADBAND' ? 'active' : ''}`}
              onClick={() => setActiveTab('BROADBAND')}
            >
              🌐 Internet Băng Rộng (Broadband)
            </button>
            <button
              className={`service-tab-btn ${activeTab === 'DIALUP' ? 'active' : ''}`}
              onClick={() => setActiveTab('DIALUP')}
            >
              📞 Internet Dial-up
            </button>
            <button
              className={`service-tab-btn ${activeTab === 'LANDLINE' ? 'active' : ''}`}
              onClick={() => setActiveTab('LANDLINE')}
            >
              ☎️ Điện Thoại Cố Định (Landline)
            </button>
          </div>

          {/* Plan Cards Grid */}
          <div className="pricing-grid">
            {filteredPlans.map(plan => (
              <div key={plan.id} className={`pricing-card ${plan.featured ? 'featured' : ''}`}>
                {plan.featured && <span className="pricing-badge">Phổ Biến Nhất</span>}
                <div className="pricing-card-header">
                  <div className="pricing-service-type">{plan.service} • {plan.speed}</div>
                  <h3 className="pricing-plan-name">{plan.name}</h3>
                </div>
                <div className="pricing-price-wrap">
                  <span className="pricing-currency">$</span>
                  <span className="pricing-price">{plan.price}</span>
                  <span className="pricing-period">/ {plan.validity}</span>
                </div>
                <ul className="pricing-features">
                  <li>
                    <Check size={16} color="var(--success)" />
                    Hạn mức / Tốc độ: <strong>{plan.quota}</strong>
                  </li>
                  <li>
                    <Check size={16} color="var(--success)" />
                    Tiền đặt cọc thiết bị: <strong>${plan.deposit}</strong> (Hoàn lại)
                  </li>
                  <li>
                    <Check size={16} color="var(--success)" />
                    Thời hạn hiệu lực: <strong>{plan.validity}</strong>
                  </li>
                  {plan.service === 'DIALUP' && (
                    <li style={{ color: '#ea580c' }}>
                      <AlertCircle size={16} color="#ea580c" />
                      Yêu cầu có sẵn Landline của Nexus
                    </li>
                  )}
                </ul>
                <button
                  className={`btn ${plan.featured ? 'btn-primary' : 'btn-outline'}`}
                  onClick={() => onRegisterClick(plan)}
                >
                  Đăng Ký Tại Cửa Hàng
                </button>
              </div>
            ))}
          </div>

          {/* Bulk Discount Interactive Calculator */}
          <div className="bulk-calculator-card">
            <div className="flex-between">
              <div>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: '#166534', display: 'flex', alignItems: 'center', gap: '8px' }}>
                  🏷️ Công Cụ Tính Chiết Khấu Số Lượng Lớn (Corporate Schemes)
                </h3>
                <p style={{ fontSize: '0.85rem', color: '#15803d' }}>
                  Giảm trừ trực tiếp trên phí ứng trước (Advance Plan Fee) và tiền đặt cọc (Security Deposit)
                </p>
              </div>
              <div className="discount-tier-badge">
                -{discountPct}% GIẢM GIÁ
              </div>
            </div>

            <div className="slider-container" style={{ margin: '1.5rem 0' }}>
              <div className="flex-between" style={{ marginBottom: '8px', fontWeight: 700 }}>
                <span>
                  Số lượng kết nối đăng ký cùng lúc:{' '}
                  <strong style={{ color: 'var(--primary)', fontSize: '1.1rem' }}>{sliderQuantity}</strong> kết nối
                </span>
                <span className="text-muted" style={{ fontSize: '0.85rem' }}>Từ 1 đến 60+ kết nối</span>
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
                  {sliderQuantity < 10
                    ? 'Không giảm (< 10 kết nối)'
                    : sliderQuantity <= 15
                    ? 'Bậc 10–15: Giảm 25%'
                    : sliderQuantity <= 25
                    ? 'Bậc 16–25: Giảm 50%'
                    : sliderQuantity <= 50
                    ? 'Bậc 26–50: Giảm 75%'
                    : 'Bậc > 50: Giảm 100%'}
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

      {/* Tracking Section */}
      <section id="tracking-section" className="mt-4">
        <div className="grid-2">
          {/* Order Tracking */}
          <div className="nexus-card">
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">📦 Tra Cứu Đơn Hàng (Order ID)</h3>
            </div>
            <p className="form-hint">Nhập mã đơn hàng 11 ký tự (VD: <code>B0000000001</code>, <code>D0000000002</code>)</p>
            <div className="flex-gap-2" style={{ marginTop: '1rem' }}>
              <input
                type="text"
                className="form-control"
                value={searchOrderId}
                onChange={(e) => setSearchOrderId(e.target.value)}
                placeholder="Nhập Order ID..."
              />
              <button className="btn btn-primary" onClick={handleSearchOrder}>
                <Search size={16} /> Tra Cứu
              </button>
            </div>

            <div className="tracking-result-box" style={{ marginTop: '1.25rem', padding: '1.25rem', background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)' }}>
              {orderResult && orderResult.notFound ? (
                <div style={{ color: 'var(--danger)', fontWeight: 600 }}>
                  ❌ Không tìm thấy đơn hàng mã {orderResult.query}
                </div>
              ) : orderResult ? (
                <div>
                  <div style={{ fontWeight: 700, fontSize: '1.05rem', marginBottom: '8px' }}>
                    {orderResult.customerName}{' '}
                    <span className={`badge ${orderResult.status === 'CONNECTED' ? 'badge-success' : 'badge-warning'}`}>
                      {orderResult.status}
                    </span>
                  </div>
                  <div className="nexus-stepper">
                    <div className="step-item completed">
                      <div className="step-circle">✓</div>
                      <div className="step-label">Tiếp Nhận Đơn</div>
                    </div>
                    <div className={`step-item ${orderResult.status === 'FEASIBLE' || orderResult.status === 'CONNECTED' ? 'completed' : 'active'}`}>
                      <div className="step-circle">
                        {orderResult.status === 'FEASIBLE' || orderResult.status === 'CONNECTED' ? '✓' : '2'}
                      </div>
                      <div className="step-label">Khảo Sát Khả Thi</div>
                    </div>
                    <div className={`step-item ${orderResult.status === 'CONNECTED' ? 'completed' : ''}`}>
                      <div className="step-circle">
                        {orderResult.status === 'CONNECTED' ? '✓' : '3'}
                      </div>
                      <div className="step-label">Cấp Đấu Nối</div>
                    </div>
                  </div>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '8px' }}>
                    • Gói đăng ký: <strong>{orderResult.planName}</strong> ({orderResult.quantity} đường)<br />
                    • Đặt cọc: <strong>${orderResult.totalDeposit.toLocaleString()}</strong> | Cước ứng trước: <strong>${orderResult.totalAdvance.toLocaleString()}</strong> (Đã giảm {orderResult.discountPercent}%)<br />
                    • Cửa hàng tiếp nhận: <strong>{orderResult.shop}</strong> ({orderResult.createdAt})
                  </div>
                </div>
              ) : (
                <div className="text-muted" style={{ fontSize: '0.875rem' }}>
                  Nhấn "Tra Cứu" để xem tiến độ khảo sát và lắp đặt của đơn hàng.
                </div>
              )}
            </div>
          </div>

          {/* Account Tracking */}
          <div className="nexus-card">
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">📡 Tra Cứu Tài Khoản (Account ID)</h3>
            </div>
            <p className="form-hint">Nhập mã tài khoản 16 ký tự do Kỹ thuật cấp (VD: <code>B001000000000001</code>)</p>
            <div className="flex-gap-2" style={{ marginTop: '1rem' }}>
              <input
                type="text"
                className="form-control"
                value={searchAccountId}
                onChange={(e) => setSearchAccountId(e.target.value)}
                placeholder="Nhập Account ID..."
              />
              <button className="btn btn-primary" onClick={handleSearchAccount}>
                <Search size={16} /> Tra Cứu
              </button>
            </div>

            <div className="tracking-result-box" style={{ marginTop: '1.25rem', padding: '1.25rem', background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)' }}>
              {accountResult && accountResult.notFound ? (
                <div style={{ color: 'var(--danger)', fontWeight: 600 }}>
                  ❌ Không tìm thấy tài khoản kết nối mã {accountResult.query}
                </div>
              ) : accountResult ? (
                <div>
                  <div className="flex-between">
                    <div style={{ fontWeight: 700, fontSize: '1.05rem' }}>
                      Account ID: <code className="font-mono" style={{ color: 'var(--primary)' }}>{accountResult.accountId}</code>
                    </div>
                    <span className={`badge ${accountResult.status === 'ACTIVE' ? 'badge-success' : 'badge-danger'}`}>
                      {accountResult.status}
                    </span>
                  </div>
                  <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginTop: '10px', lineHeight: 1.6 }}>
                    • Chủ thuê bao: <strong>{accountResult.customerName}</strong><br />
                    • Dịch vụ: <strong>{accountResult.service}</strong> ({accountResult.planName})<br />
                    • Thiết bị modem/router: <code>{accountResult.equipmentSerial}</code><br />
                    • Địa chỉ đấu nối: <strong>{accountResult.installationAddress}</strong><br />
                    • Ngày kích hoạt: <strong>{accountResult.activatedAt}</strong>
                  </div>
                </div>
              ) : (
                <div className="text-muted" style={{ fontSize: '0.875rem' }}>
                  Nhấn "Tra Cứu" để xem tình trạng hoạt động và thiết bị của đường truyền.
                </div>
              )}
            </div>
          </div>
        </div>
      </section>

      {/* Feedback Section */}
      <section className="mt-4">
        <div className="nexus-card">
          <div className="nexus-card-header">
            <h3 className="nexus-card-title">💬 Gửi Ý Kiến Phản Hồi & Đóng Góp</h3>
          </div>
          {feedbackSuccess && (
            <div style={{ padding: '12px', background: 'var(--success-bg)', color: 'var(--success)', borderRadius: 'var(--radius)', marginBottom: '1rem', fontWeight: 600 }}>
              ✓ Cảm ơn quý khách! Ý kiến phản hồi đã được chuyển tới Ban Chăm sóc Khách hàng Nexus.
            </div>
          )}
          <form onSubmit={handleFeedbackSubmit}>
            <div className="grid-3">
              <div className="form-group">
                <label className="form-label">Họ và tên <span className="req">*</span></label>
                <input type="text" className="form-control" placeholder="Nguyễn Văn A" required />
              </div>
              <div className="form-group">
                <label className="form-label">Số điện thoại liên hệ <span className="req">*</span></label>
                <input type="tel" className="form-control" placeholder="0987654321" required />
              </div>
              <div className="form-group">
                <label className="form-label">Mã Account ID / Order ID (nếu có)</label>
                <input type="text" className="form-control" placeholder="B001000000000001" />
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">Nội dung phản hồi hoặc khiếu nại <span className="req">*</span></label>
              <textarea className="form-control" rows="3" placeholder="Nhập thông tin phản hồi của bạn về chất lượng dịch vụ, tốc độ đường truyền hoặc nhân viên hỗ trợ..." required></textarea>
            </div>
            <button type="submit" className="btn btn-primary">
              <Send size={16} /> Gửi Phản Hồi Cho Nexus
            </button>
          </form>
        </div>
      </section>
    </div>
  );
}

