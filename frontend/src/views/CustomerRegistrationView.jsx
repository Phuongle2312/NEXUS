import React, { useState } from 'react';
import { PLANS_DATA, CITIES, SHOPS, getDiscountPercent } from '../data/mockData.js';
import { Sparkles, CheckCircle, Shield, AlertCircle } from 'lucide-react';

export default function CustomerRegistrationView({ onOrderSubmitted, preselectedPlan }) {
  const [customerName, setCustomerName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [city, setCity] = useState('Hà Nội (001)');
  const [address, setAddress] = useState('');
  const [service, setService] = useState(preselectedPlan ? preselectedPlan.service : 'BROADBAND');
  const [planId, setPlanId] = useState(preselectedPlan ? preselectedPlan.id : 12);
  const [quantity, setQuantity] = useState(1);
  const [submittedOrder, setSubmittedOrder] = useState(null);

  const availablePlans = PLANS_DATA.filter(p => p.service === service);
  const selectedPlan = PLANS_DATA.find(p => p.id === Number(planId)) || availablePlans[0] || PLANS_DATA[0];

  const discountPct = getDiscountPercent(quantity);
  const factor = 1 - discountPct / 100;
  const totalDeposit = Math.round(selectedPlan.deposit * quantity * factor);
  const totalAdvance = Math.round(selectedPlan.price * quantity * factor);
  const grandTotal = totalDeposit + totalAdvance;

  const handleServiceChange = (e) => {
    const s = e.target.value;
    setService(s);
    const plans = PLANS_DATA.filter(p => p.service === s);
    if (plans.length > 0) setPlanId(plans[0].id);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const typeChar = service === 'BROADBAND' ? 'B' : service === 'DIALUP' ? 'D' : 'T';
    const seq = Math.floor(1000000000 + Math.random() * 9000000000).toString();
    const orderId = `${typeChar}${seq}`;

    const newOrder = {
      orderId,
      customerName,
      phone,
      service,
      planName: selectedPlan.name,
      planPrice: selectedPlan.price,
      quantity,
      discountPercent: discountPct,
      depositPerConn: selectedPlan.deposit,
      totalDeposit,
      totalAdvance,
      status: 'SUBMITTED',
      city,
      shop: 'Cửa hàng trực tuyến Nexus',
      createdAt: new Date().toISOString().split('T')[0]
    };

    onOrderSubmitted(newOrder);
    setSubmittedOrder(newOrder);
  };

  return (
    <div style={{ maxWidth: '850px', margin: '0 auto' }}>
      <div className="nexus-card">
        <div className="nexus-card-header">
          <div>
            <h2 className="nexus-card-title" style={{ fontSize: '1.35rem' }}>
              <Sparkles size={22} color="var(--primary)" /> Đăng Ký Dịch Vụ Viễn Thông & Internet Nexus
            </h2>
            <p className="form-hint">Điền thông tin trực tuyến để nhân viên kỹ thuật tới khảo sát và cấp đường truyền</p>
          </div>
        </div>

        {submittedOrder ? (
          <div style={{ padding: '2rem 1.5rem', textAlign: 'center' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--success-bg)', color: 'var(--success)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 1.25rem auto' }}>
              <CheckCircle size={36} />
            </div>
            <h3 style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text-primary)', marginBottom: '8px' }}>
              Đăng Ký Thành Công!
            </h3>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '1.5rem' }}>
              Yêu cầu của quý khách đã được tiếp nhận vào hệ thống. Đội ngũ kỹ thuật Nexus sẽ liên hệ và khảo sát hạ tầng tại địa chỉ của bạn.
            </p>

            <div style={{ background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)', padding: '1.5rem', textAlign: 'left', maxWidth: '500px', margin: '0 auto 1.5rem auto' }}>
              <div className="flex-between" style={{ marginBottom: '8px' }}>
                <span className="text-muted">Mã Đơn Hàng (Order ID):</span>
                <strong className="font-mono" style={{ color: 'var(--primary)', fontSize: '1.15rem' }}>
                  {submittedOrder.orderId}
                </strong>
              </div>
              <div className="flex-between" style={{ marginBottom: '8px' }}>
                <span className="text-muted">Gói cước đã chọn:</span>
                <strong>{submittedOrder.planName}</strong>
              </div>
              <div className="flex-between" style={{ marginBottom: '8px' }}>
                <span className="text-muted">Số lượng đường truyền:</span>
                <strong>{submittedOrder.quantity} kết nối</strong>
              </div>
              <div className="flex-between" style={{ marginBottom: '8px' }}>
                <span className="text-muted">Mức chiết khấu áp dụng:</span>
                <span className="badge badge-success">-{submittedOrder.discountPercent}%</span>
              </div>
              <div className="flex-between" style={{ borderTop: '1px dashed var(--border-color)', paddingTop: '8px', marginTop: '8px' }}>
                <span className="text-muted">Tạm tính phí cọc & ứng trước:</span>
                <strong style={{ fontSize: '1.1rem', color: 'var(--primary)' }}>${(submittedOrder.totalDeposit + submittedOrder.totalAdvance).toLocaleString()}</strong>
              </div>
            </div>

            <button className="btn btn-primary" onClick={() => setSubmittedOrder(null)}>
              Tiếp Tục Đăng Ký Đơn Khác
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="grid-2">
              <div className="form-group">
                <label className="form-label">Họ tên khách hàng / Đơn vị đăng ký <span className="req">*</span></label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: Nguyễn Văn An / Công ty TNHH..."
                  value={customerName}
                  onChange={(e) => setCustomerName(e.target.value)}
                  required
                />
              </div>
              <div className="form-group">
                <label className="form-label">Số điện thoại liên hệ <span className="req">*</span></label>
                <input
                  type="tel"
                  className="form-control"
                  placeholder="0912345678"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className="grid-2">
              <div className="form-group">
                <label className="form-label">Email nhận thông báo cước</label>
                <input
                  type="email"
                  className="form-control"
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Tỉnh / Thành phố lắp đặt <span className="req">*</span></label>
                <select className="form-select" value={city} onChange={(e) => setCity(e.target.value)}>
                  {CITIES.map(c => (
                    <option key={c.code} value={`${c.name} (${c.code})`}>
                      {c.name} (Mã vùng: {c.code})
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Địa chỉ lắp đặt chi tiết (Số nhà, tòa nhà, đường phố) <span className="req">*</span></label>
              <input
                type="text"
                className="form-control"
                placeholder="Số 45, Đường Lê Duẩn, Phường Bến Nghé, Quận 1..."
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                required
              />
            </div>

            <div className="grid-2">
              <div className="form-group">
                <label className="form-label">Dịch vụ quan tâm <span className="req">*</span></label>
                <select className="form-select" value={service} onChange={handleServiceChange}>
                  <option value="BROADBAND">🌐 Internet Băng Rộng (Broadband - B)</option>
                  <option value="DIALUP">📞 Internet Dial-up (D - Cần có Landline Nexus)</option>
                  <option value="LANDLINE">☎️ Điện thoại cố định (Landline - T)</option>
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">Gói cước lựa chọn <span className="req">*</span></label>
                <select
                  className="form-select"
                  value={planId}
                  onChange={(e) => setPlanId(Number(e.target.value))}
                >
                  {availablePlans.map(p => (
                    <option key={p.id} value={p.id}>
                      {p.name} (${p.price} / {p.validity})
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Số lượng đường truyền đăng ký <span className="req">*</span></label>
              <div style={{ display: 'flex', gap: '14px', alignItems: 'center' }}>
                <input
                  type="number"
                  className="form-control"
                  min="1"
                  max="200"
                  value={quantity}
                  onChange={(e) => setQuantity(parseInt(e.target.value, 10) || 1)}
                  style={{ width: '130px' }}
                  required
                />
                <span className={`badge ${discountPct > 0 ? 'badge-success' : 'badge-neutral'}`} style={{ fontSize: '0.85rem', padding: '6px 12px' }}>
                  {discountPct > 0 ? `-${discountPct}% Chiết Khấu Số Lượng Lớn` : '0% Chiết Khấu (Dưới 10 đường)'}
                </span>
              </div>
              <p className="form-hint">Chính sách ưu đãi: 10–15 (giảm 25%), 16–25 (giảm 50%), 26–50 (giảm 75%), &gt;50 (giảm 100% cọc & cước trước)</p>
            </div>

            {/* Price Preview */}
            <div style={{ background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)', padding: '16px', marginTop: '1.25rem' }}>
              <div style={{ fontWeight: 700, fontSize: '0.95rem', marginBottom: '8px', color: 'var(--text-primary)' }}>
                💰 Chi Phí Dự Kiến (Đóng khi ký hợp đồng):
              </div>
              <div className="flex-between" style={{ fontSize: '0.9rem', marginBottom: '4px' }}>
                <span>Tiền cọc bảo đảm hoàn lại (${selectedPlan.deposit}/đường):</span>
                <span style={{ fontWeight: 700 }}>${totalDeposit.toLocaleString()}</span>
              </div>
              <div className="flex-between" style={{ fontSize: '0.9rem', marginBottom: '8px' }}>
                <span>Cước gói ứng trước (${selectedPlan.price}/đường):</span>
                <span style={{ fontWeight: 700 }}>${totalAdvance.toLocaleString()}</span>
              </div>
              <div className="flex-between" style={{ fontSize: '1.1rem', borderTop: '1px dashed var(--border-color)', paddingTop: '10px', fontWeight: 800, color: 'var(--primary)' }}>
                <span>Tổng chi phí ban đầu:</span>
                <span>${grandTotal.toLocaleString()}</span>
              </div>
            </div>

            <div style={{ marginTop: '1.5rem', textAlign: 'right' }}>
              <button type="submit" className="btn btn-primary btn-lg">
                Xác Nhận Đăng Ký Trực Tuyến
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
