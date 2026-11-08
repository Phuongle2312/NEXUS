import React, { useState } from 'react';
import { PLANS_DATA, CITIES, SHOPS, getDiscountPercent } from '../data/mockData.js';
import { Plus, Printer, FileText, Clock, DollarSign } from 'lucide-react';

export default function RetailView({ orders, onAddOrder }) {
  const [showModal, setShowModal] = useState(false);

  // Form states
  const [customerName, setCustomerName] = useState('Tập đoàn Công nghệ Viễn thông Sao Mai');
  const [phone, setPhone] = useState('0934567890');
  const [city, setCity] = useState('Hà Nội (001)');
  const [shop, setShop] = useState('Nexus Store Ba Đình');
  const [service, setService] = useState('BROADBAND');
  const [planId, setPlanId] = useState(12); // Default Broadband 128K
  const [quantity, setQuantity] = useState(15);

  const availablePlans = PLANS_DATA.filter(p => p.service === service);
  const selectedPlan = PLANS_DATA.find(p => p.id === Number(planId)) || availablePlans[0] || PLANS_DATA[0];

  const discountPct = getDiscountPercent(quantity);
  const factor = 1 - discountPct / 100;

  const totalDeposit = Math.round(selectedPlan.deposit * quantity * factor);
  const totalAdvance = Math.round(selectedPlan.price * quantity * factor);
  const grandTotal = totalDeposit + totalAdvance;

  const handleServiceChange = (e) => {
    const newService = e.target.value;
    setService(newService);
    const plansForService = PLANS_DATA.filter(p => p.service === newService);
    if (plansForService.length > 0) {
      setPlanId(plansForService[0].id);
    }
  };

  const handleCreateOrder = (e) => {
    e.preventDefault();
    const typeChar = service === 'BROADBAND' ? 'B' : service === 'DIALUP' ? 'D' : 'T';
    const seq = (orders.length + 1).toString().padStart(10, '0');
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
      shop,
      createdAt: new Date().toISOString().split('T')[0]
    };

    onAddOrder(newOrder);
    setShowModal(false);
    alert(`Đã tạo thành công đơn hàng: ${orderId}!\nĐơn đã chuyển sang hàng đợi khảo sát của Bộ phận Kỹ thuật.`);
  };

  const handlePrintReceipt = (ord) => {
    alert(
      `Phiếu Thu Quầy Nexus:\nĐơn hàng: ${ord.orderId}\nKhách hàng: ${ord.customerName}\nTiền cọc: $${ord.totalDeposit.toLocaleString()}\nCước trả trước: $${ord.totalAdvance.toLocaleString()}\nTổng thực thu: $${(ord.totalAdvance + ord.totalDeposit).toLocaleString()}`
    );
  };

  return (
    <div>
      <div className="flex-between" style={{ marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text-primary)' }}>
            🏪 Cửa Hàng Bán Lẻ (Retail Outlet)
          </h2>
          <p className="form-hint">Điểm tiếp nhận yêu cầu, ký hợp đồng dịch vụ và thu phí trực tiếp tại quầy</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowModal(true)}>
          <Plus size={18} /> Tạo Đơn Hàng Mới
        </button>
      </div>

      {/* KPI Summary */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon blue"><FileText size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{orders.length}</div>
            <div className="stat-label">Tổng đơn đã tiếp nhận</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon orange"><Clock size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">{orders.filter(o => o.status === 'SUBMITTED').length}</div>
            <div className="stat-label">Chờ kỹ thuật khảo sát</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon green"><DollarSign size={24} /></div>
          <div className="stat-info">
            <div className="stat-value">
              ${orders.reduce((sum, o) => sum + (o.totalAdvance + o.totalDeposit), 0).toLocaleString()}
            </div>
            <div className="stat-label">Tổng tiền cọc & cước thu quầy</div>
          </div>
        </div>
      </div>

      {/* Orders Table */}
      <div className="nexus-card">
        <div className="nexus-card-header">
          <h3 className="nexus-card-title">Danh Sách Đơn Đặt Hàng Tiếp Nhận</h3>
        </div>
        <div className="table-responsive">
          <table className="nexus-table">
            <thead>
              <tr>
                <th>Order ID</th>
                <th>Khách Hàng / Đơn Vị</th>
                <th>Dịch Vụ & Gói Cước</th>
                <th>Số Lượng</th>
                <th>Ưu Đãi</th>
                <th>Tiền Cọc Thu</th>
                <th>Cước Ứng Trước</th>
                <th>Trạng Thái</th>
                <th>Thao Tác</th>
              </tr>
            </thead>
            <tbody>
              {orders.map(o => (
                <tr key={o.orderId}>
                  <td><strong className="font-mono" style={{ color: 'var(--primary)' }}>{o.orderId}</strong></td>
                  <td>
                    <strong>{o.customerName}</strong><br />
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{o.phone} • {o.city}</span>
                  </td>
                  <td>
                    <span className={`badge ${o.service === 'BROADBAND' ? 'badge-info' : o.service === 'DIALUP' ? 'badge-warning' : 'badge-neutral'}`}>
                      {o.service}
                    </span>
                    <div style={{ fontSize: '0.85rem', marginTop: '4px' }}>{o.planName}</div>
                  </td>
                  <td><strong>{o.quantity}</strong> đường</td>
                  <td>
                    {o.discountPercent > 0 ? (
                      <span className="badge badge-success">-{o.discountPercent}%</span>
                    ) : (
                      <span className="text-muted">0%</span>
                    )}
                  </td>
                  <td><strong>${o.totalDeposit.toLocaleString()}</strong></td>
                  <td><strong>${o.totalAdvance.toLocaleString()}</strong></td>
                  <td>
                    <span className={`badge ${
                      o.status === 'CONNECTED' ? 'badge-success' :
                      o.status === 'FEASIBLE' ? 'badge-info' :
                      o.status === 'SUBMITTED' ? 'badge-warning' : 'badge-danger'
                    }`}>
                      {o.status}
                    </span>
                  </td>
                  <td>
                    <button className="btn btn-outline btn-sm" onClick={() => handlePrintReceipt(o)}>
                      <Printer size={14} /> In Phiếu
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Create Order Modal */}
      {showModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700 }}>📝 Tiếp Nhận Đơn Đặt Hàng Mới</h3>
              <button className="btn btn-outline btn-sm" onClick={() => setShowModal(false)}>✕</button>
            </div>
            <form onSubmit={handleCreateOrder}>
              <div className="modal-body">
                <div className="grid-2">
                  <div className="form-group">
                    <label className="form-label">Tên khách hàng / Tổ chức <span className="req">*</span></label>
                    <input
                      type="text"
                      className="form-control"
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
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                      required
                    />
                  </div>
                </div>

                <div className="grid-2">
                  <div className="form-group">
                    <label className="form-label">Thành phố lắp đặt <span className="req">*</span></label>
                    <select className="form-select" value={city} onChange={(e) => setCity(e.target.value)}>
                      {CITIES.map(c => (
                        <option key={c.code} value={`${c.name} (${c.code})`}>
                          {c.name} (Mã: {c.code})
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Cửa hàng tiếp nhận <span className="req">*</span></label>
                    <select className="form-select" value={shop} onChange={(e) => setShop(e.target.value)}>
                      {SHOPS.map(s => (
                        <option key={s.id} value={s.name}>{s.name}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="grid-2">
                  <div className="form-group">
                    <label className="form-label">Loại dịch vụ <span className="req">*</span></label>
                    <select className="form-select" value={service} onChange={handleServiceChange}>
                      <option value="BROADBAND">🌐 Internet Băng Rộng (Broadband - B)</option>
                      <option value="DIALUP">📞 Internet Dial-up (D)</option>
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
                  <label className="form-label">Số lượng kết nối đăng ký <span className="req">*</span></label>
                  <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
                    <input
                      type="number"
                      className="form-control"
                      min="1"
                      max="500"
                      value={quantity}
                      onChange={(e) => setQuantity(parseInt(e.target.value, 10) || 1)}
                      style={{ width: '140px' }}
                      required
                    />
                    <span className={`badge ${discountPct > 0 ? 'badge-success' : 'badge-neutral'}`}>
                      {discountPct > 0 ? `-${discountPct}% Chiết Khấu` : '0% Chiết Khấu'}
                    </span>
                  </div>
                  <p className="form-hint">Chiết khấu số lượng: 10–15 (25%), 16–25 (50%), 26–50 (75%), &gt;50 (100%)</p>
                </div>

                {/* Calculation Summary */}
                <div style={{ background: '#f8fafc', border: '1px solid var(--border-color)', borderRadius: 'var(--radius)', padding: '14px', marginTop: '1rem' }}>
                  <div style={{ fontWeight: 700, fontSize: '0.9rem', marginBottom: '8px', color: 'var(--text-primary)' }}>
                    💰 Tạm Tính Chi Phí Thu Tại Quầy:
                  </div>
                  <div className="flex-between" style={{ fontSize: '0.85rem', marginBottom: '4px' }}>
                    <span>Tiền đặt cọc bảo đảm (Hoàn lại):</span>
                    <span style={{ fontWeight: 700 }}>${totalDeposit.toLocaleString()}</span>
                  </div>
                  <div className="flex-between" style={{ fontSize: '0.85rem', marginBottom: '6px' }}>
                    <span>Cước gói ứng trước kỳ đầu:</span>
                    <span style={{ fontWeight: 700 }}>${totalAdvance.toLocaleString()}</span>
                  </div>
                  <div className="flex-between" style={{ fontSize: '1rem', borderTop: '1px dashed var(--border-color)', paddingTop: '8px', fontWeight: 800, color: 'var(--primary)' }}>
                    <span>Tổng tiền thu tại quầy:</span>
                    <span>${grandTotal.toLocaleString()}</span>
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                  Hủy Bỏ
                </button>
                <button type="submit" className="btn btn-primary">
                  Xác Nhận & Tạo Đơn
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

